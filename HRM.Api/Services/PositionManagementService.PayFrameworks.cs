using System.Data;
using HRM.Application.Contracts.Pages;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class PositionManagementService
{
    public async Task<PayFrameworkData> LoadPayFrameworksAsync()
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();
        var frameworks=await db.PositionFrameworks.OrderBy(x=>x.FrameworkId).Select(x=>new PositionFrameworkOption(x.FrameworkId,x.Code,x.Name,x.IsActive)).ToListAsync();
        var steps=await db.FrameworkJobMatrixSteps.OrderBy(x=>x.StepNumber).ToListAsync();
        var titles=await(from link in db.FrameworkJobMatrixTitles join title in db.FrameworkJobTitles on link.JobTitleId equals title.JobTitleId where title.IsActive select new{link.JobMatrixId,title.JobTitleId,title.Name}).ToListAsync();
        var benefits=await db.FrameworkJobMatrixBenefits.ToListAsync();
        var used=await db.PositionMatrices.Select(x=>x.JobMatrixId).Distinct().ToListAsync();
        var matrices=(await db.FrameworkJobMatrices.OrderBy(x=>x.MatrixCode).ToListAsync()).Select(x=>new JobMatrixRow(x.JobMatrixId,x.FrameworkId??0,x.MatrixCode,x.Name,x.CurrencyTypeId??0,x.BasicSalary,x.EffectiveFrom,x.EffectiveTo,x.IsActive,used.Contains(x.JobMatrixId),
            titles.Where(t=>t.JobMatrixId==x.JobMatrixId).Select(t=>new PositionLookup(t.JobTitleId,t.Name)).ToList(),
            steps.Where(s=>s.JobMatrixId==x.JobMatrixId).Select(s=>new MatrixStepOption(s.JobMatrixStepId,s.StepNumber,s.BasicSalaryOverride??x.BasicSalary,s.JobAllowance,s.IsActive)).ToList(),
            benefits.Where(b=>b.JobMatrixId==x.JobMatrixId).Select(b=>new MatrixBenefitInput(b.BenefitName,b.Amount,b.FrequencyCode??"Monthly")).ToList()){Profile=Profile(x)}).ToList();
        var duties=await db.FrameworkMatrixDuties.OrderBy(x=>x.SequenceNumber).ToListAsync(); var qualifications=await db.FrameworkMatrixQualifications.OrderBy(x=>x.PathwayNumber).ToListAsync(); foreach(var m in matrices){m.Profile.Duties=string.Join("\n",duties.Where(x=>x.JobMatrixId==m.Id).Select(x=>x.DutyDescription));m.Profile.Qualifications=string.Join("\nOR\n",qualifications.Where(x=>x.JobMatrixId==m.Id).Select(x=>x.RequirementText));} var currencies=await db.FinanceCurrencies.OrderBy(x=>x.Name).Select(x=>new PositionLookup(x.CurrencyTypeId,x.Name)).ToListAsync();
        var families=await db.PayJobFamilies.Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new PositionLookup(x.JobFamilyId,x.Name)).ToListAsync();
        var professions=(await db.PayProfessions.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToListAsync()).Select(x=>new ProfessionOption(x.Id,x.JobFamilyId,x.Name,x.Tasks,System.Text.Json.JsonSerializer.Deserialize<List<string>>(x.ExampleTitlesJson)??[],System.Text.Json.JsonSerializer.Deserialize<List<string>>(x.MisscoCodesJson)??[])).ToList();
        var studyFields=await db.PayStudyFields.Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new StudyFieldOption(x.JobFamilyId,x.Name)).ToListAsync();
        var experienceAreas=await db.PayExperienceAreas.Where(x=>x.IsActive).OrderBy(x=>x.Sector).ThenBy(x=>x.Name).Select(x=>new ExperienceAreaOption(x.Id,x.JobFamilyId,x.Sector,x.Name,x.Description)).ToListAsync();
        return new(frameworks,matrices,currencies,actor.Super,actor.Manage){JobFamilies=families,Professions=professions,StudyFields=studyFields,ExperienceAreas=experienceAreas};
    }

    public async Task<ServiceResult> SaveFrameworkAsync(PayFrameworkInput input)
    {
        var actor=await ScopeAsync();if(!actor.Super)throw new UnauthorizedAccessException("Only a superadministrator can configure shared pay frameworks.");
        var code=input.Code?.Trim().ToUpperInvariant()??"";
        if(!System.Text.RegularExpressions.Regex.IsMatch(code,"^[A-Z][A-Z0-9_]{0,29}$")||string.IsNullOrWhiteSpace(input.Name)||input.Name.Trim().Length>150)return ServiceResult.Fail("Enter a code up to 30 characters and a framework name up to 150 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var row=input.Id==0?new PositionFramework{FrameworkId=(await db.PositionFrameworks.MaxAsync(x=>(int?)x.FrameworkId)??0)+1}:await db.PositionFrameworks.SingleOrDefaultAsync(x=>x.FrameworkId==input.Id)??throw new InvalidOperationException("Framework unavailable.");
        if(input.Id!=0&&row.Code!=code)return ServiceResult.Fail("Framework codes are permanent. Create a new framework to use another code.");
        if(await db.PositionFrameworks.AnyAsync(x=>x.FrameworkId!=row.FrameworkId&&x.Code==code))return ServiceResult.Fail("This framework code already exists.");
        if(!input.Active&&(await db.FrameworkJobMatrices.AnyAsync(x=>x.FrameworkId==row.FrameworkId&&x.IsActive)||await db.Positions.AnyAsync(x=>x.PositionFrameworkId==row.FrameworkId&&x.IsActive!=false)))return ServiceResult.Fail("Deactivate dependent matrices and positions before disabling this framework.");
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Pay framework {row.FrameworkId}: {code}, {input.Name.Trim()}, active={input.Active}.");
        row.Code=code;row.Name=input.Name.Trim();row.IsActive=input.Active;if(input.Id==0)db.PositionFrameworks.Add(row);
        await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Pay framework saved.");
    }

    private static bool Money(decimal value)=>value>=0&&value<=999999999999.99m&&decimal.Round(value,2)==value;
    public async Task<ServiceResult> SaveJobMatrixAsync(JobMatrixInput input)
    {
        var actor=await ScopeAsync();if(!actor.Super)throw new UnauthorizedAccessException("Only a superadministrator can configure shared job matrices.");
        var resolved=await ResolveReferenceAsync(input);if(resolved==null)return ServiceResult.Fail("Select an active Job Family and Profession / Occupation.");input=resolved;
        if(!ValidProfile(input.Profile))return ServiceResult.Fail("Check the Job Matrix classification, daily attendance benefit and text limits."); if(string.IsNullOrWhiteSpace(input.Code)||input.Code.Trim().Length>60||string.IsNullOrWhiteSpace(input.Name)||input.Name.Trim().Length>250||!Money(input.BasicSalary)||input.From.Year<1900||input.To<input.From.Date)
            return ServiceResult.Fail("Enter a matrix code, name, valid salary and effective period.");
        if(input.Titles==null||input.Steps==null||input.Benefits==null||input.Titles.Count is <1 or >100||input.Steps.Count is <1 or >100||input.Benefits.Count>100||input.Titles.Any(x=>string.IsNullOrWhiteSpace(x)||x.Trim().Length>250)||input.Titles.Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=input.Titles.Count)
            return ServiceResult.Fail("Add at least one unique title and one step, with up to 100 rows per section.");
        if(input.Steps.Any(x=>x==null||x.Number<1||!Money(x.JobAllowance)||x.BasicSalaryOverride.HasValue&&!Money(x.BasicSalaryOverride.Value))||input.Steps.Select(x=>x.Number).Distinct().Count()!=input.Steps.Count||!input.Steps.Any(x=>x.Active))return ServiceResult.Fail("Use unique positive step numbers, valid amounts and at least one active step.");
        if(input.Benefits.Any(x=>x==null||string.IsNullOrWhiteSpace(x.Name)||x.Name.Trim().Length>200||x.Amount.HasValue&&!Money(x.Amount.Value)||x.Frequency is not("Monthly" or "SixMonthly" or "Annually"))||input.Benefits.Select(x=>x.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=input.Benefits.Count)return ServiceResult.Fail("Use unique allowance names, valid amounts and a supported payment frequency.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.PositionFrameworks.AnyAsync(x=>x.FrameworkId==input.FrameworkId&&x.IsActive)||!await db.FinanceCurrencies.AnyAsync(x=>x.CurrencyTypeId==input.CurrencyId))return ServiceResult.Fail("Select an active framework and salary currency.");
        if(input.Id!=0&&await db.PositionMatrices.AnyAsync(x=>x.JobMatrixId==input.Id))return ServiceResult.Fail("This job matrix is already used by a position matrix. Create a new matrix version to preserve employment and pay history.");
        if(await db.FrameworkJobMatrices.AnyAsync(x=>x.JobMatrixId!=input.Id&&x.MatrixCode==input.Code.Trim()))return ServiceResult.Fail("This matrix code already exists. Use a distinct code for a new version.");
        if(input.Id!=0&&!await ProfileTextsMatch(db,input.Id,input.Profile))return ServiceResult.Fail("Existing structured duties and qualifications are preserved. Create a new matrix version to revise them."); var row=input.Id==0?new FrameworkJobMatrix():await db.FrameworkJobMatrices.SingleOrDefaultAsync(x=>x.JobMatrixId==input.Id)??throw new InvalidOperationException("Job matrix unavailable.");
        var log=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Job matrix {input.Id}: framework {input.FrameworkId}, {input.Code.Trim()}, salary {input.BasicSalary}, currency {input.CurrencyId}.");
        row.FrameworkId=input.FrameworkId;row.CurrencyTypeId=input.CurrencyId;row.MatrixCode=input.Code.Trim();row.Name=input.Name.Trim();row.BasicSalary=input.BasicSalary;row.EffectiveFrom=input.From.Date;row.EffectiveTo=input.To?.Date;row.IsActive=input.Active;row.OperationLogId=log.OperationLogId;
        ApplyProfile(row,input.Profile); if(input.Id==0)db.FrameworkJobMatrices.Add(row);await db.SaveChangesAsync();
        if(input.Id!=0)
        {
            db.FrameworkJobMatrixTitles.RemoveRange(await db.FrameworkJobMatrixTitles.Where(x=>x.JobMatrixId==row.JobMatrixId).ToListAsync());
            db.FrameworkJobMatrixSteps.RemoveRange(await db.FrameworkJobMatrixSteps.Where(x=>x.JobMatrixId==row.JobMatrixId).ToListAsync());
            db.FrameworkJobMatrixBenefits.RemoveRange(await db.FrameworkJobMatrixBenefits.Where(x=>x.JobMatrixId==row.JobMatrixId).ToListAsync());
            await db.SaveChangesAsync();
        }
        foreach(var name in input.Titles.Select(x=>x.Trim()))
        {
            var title=await db.FrameworkJobTitles.FirstOrDefaultAsync(x=>x.Name.ToUpper()==name.ToUpper());
            if(title==null){title=new(){Name=name,IsActive=true};db.FrameworkJobTitles.Add(title);await db.SaveChangesAsync();}
            if(!title.IsActive)return ServiceResult.Fail("An inactive title with this name already exists. Use another title.");
            db.FrameworkJobMatrixTitles.Add(new(){JobMatrixId=row.JobMatrixId,JobTitleId=title.JobTitleId});
        }
        db.FrameworkJobMatrixSteps.AddRange(input.Steps.Select(x=>new FrameworkJobMatrixStep{JobMatrixId=row.JobMatrixId,StepNumber=x.Number,JobAllowance=x.JobAllowance,BasicSalaryOverride=x.BasicSalaryOverride,IsActive=x.Active}));
        db.FrameworkJobMatrixBenefits.AddRange(input.Benefits.Select(x=>new FrameworkJobMatrixBenefit{JobMatrixId=row.JobMatrixId,BenefitName=x.Name.Trim(),Amount=x.Amount,FrequencyCode=x.Frequency}));
        await SaveProfileText(db,row.JobMatrixId,input.Profile); await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Job matrix saved with its titles, steps and allowances.");
    }

    public async Task<ServiceResult> CreatePositionMatrixAsync(PositionMatrixInput input)
    {
        var actor=await ScopeAsync();if(!actor.Manage)throw new UnauthorizedAccessException("Position configuration permission is required.");
        if(input.Slots<1||input.Slots>100000||input.From.Year<1900||input.To<input.From.Date)return ServiceResult.Fail("Enter positive approved slots and a valid effective period.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var matrix=await db.FrameworkJobMatrices.SingleOrDefaultAsync(x=>x.JobMatrixId==input.JobMatrixId&&x.IsActive);
        if(matrix?.FrameworkId==null||matrix.CurrencyTypeId==null||!await db.PositionFrameworks.AnyAsync(x=>x.FrameworkId==matrix.FrameworkId&&x.IsActive)||matrix.EffectiveFrom>input.From.Date||matrix.EffectiveTo.HasValue&&(!input.To.HasValue||input.To.Value.Date>matrix.EffectiveTo))return ServiceResult.Fail("Select an active job matrix and dates within its effective period.");
        if(!await db.FrameworkJobMatrixTitles.AnyAsync(x=>x.JobMatrixId==matrix.JobMatrixId&&x.JobTitleId==input.TitleId)||!await db.FrameworkJobTitles.AnyAsync(x=>x.JobTitleId==input.TitleId&&x.IsActive))return ServiceResult.Fail("Select an allowed title belonging to this job matrix.");
        if(await db.PositionMatrices.AnyAsync(x=>x.OrganisationId==actor.Org&&x.JobMatrixId==matrix.JobMatrixId&&x.JobTitleId==input.TitleId&&x.IsActive&&(!input.To.HasValue||x.EffectiveFrom<=input.To.Value)&&(!x.EffectiveTo.HasValue||x.EffectiveTo>=input.From.Date)))return ServiceResult.Fail("An overlapping position matrix already exists for this title.");
        var title=await db.FrameworkJobTitles.SingleAsync(x=>x.JobTitleId==input.TitleId);
        var log=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Create position matrix in organisation {actor.Org}: job matrix {matrix.JobMatrixId}, title {input.TitleId}, slots {input.Slots}.");
        var positionMatrix=new PositionMatrix{OrganisationId=actor.Org,JobMatrixId=matrix.JobMatrixId,JobTitleId=input.TitleId,ApprovedSlots=input.Slots,EffectiveFrom=input.From.Date,EffectiveTo=input.To?.Date,IsActive=true,OperationLogId=log.OperationLogId};
        db.PositionMatrices.Add(positionMatrix);await db.SaveChangesAsync();
        db.FrameworkPositionMatrixHistories.Add(new(){PositionMatrixId=positionMatrix.PositionMatrixId,NewApprovedSlots=input.Slots,ChangeReason="Initial position matrix",ChangedAt=DateTime.UtcNow,OperationLogId=log.OperationLogId});
        var name=$"{title.Name} · {matrix.MatrixCode}";
        if(name.Length>250)return ServiceResult.Fail("The title and matrix code produce a position name longer than 250 characters. Shorten the title or code.");
        db.Positions.Add(new(){Name=name,PositionFrameworkId=matrix.FrameworkId,PositionMatrixId=positionMatrix.PositionMatrixId,OrganisationId=actor.Org,IsActive=true,IsGlobal=false,IsSalaryGivenBasedOnPostion=true,OperationLogId=log.OperationLogId});
        await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Position matrix created. The position is available for staff assignment through the existing approval flow.");
    }
}
