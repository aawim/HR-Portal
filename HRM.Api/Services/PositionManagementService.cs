using System.Data;
using HRM.Application.Contracts.Pages;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed partial class PositionManagementService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService logs):IPositionManagement
{
    private async Task<(int Org,bool Manage,bool Super)> ScopeAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        var manage=actor.IsSuperAdministrator||await access.HasPermissionAsync("Positions.Configure");
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!manage&&!await access.HasPermissionAsync("Position.Details")&&!await access.HasPermissionAsync("Staff.Positions.Manage"))throw new UnauthorizedAccessException("Position management permission is required in your current organisation.");
        return(org,manage,actor.IsSuperAdministrator);
    }
    public async Task<PositionManagementData> LoadAsync()
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();var today=DateTime.UtcNow.AddHours(5).Date;
        var frameworks=await db.PositionFrameworks.OrderBy(x=>x.FrameworkId).Select(x=>new PositionFrameworkOption(x.FrameworkId,x.Code,x.Name,x.IsActive)).ToListAsync();
        var matrices=await(from m in db.PositionMatrices join j in db.FrameworkJobMatrices on m.JobMatrixId equals j.JobMatrixId join t in db.FrameworkJobTitles on m.JobTitleId equals t.JobTitleId
            where m.OrganisationId==actor.Org&&m.IsActive&&m.EffectiveFrom<=today&&(!m.EffectiveTo.HasValue||m.EffectiveTo>=today)&&j.IsActive&&t.IsActive
            orderby t.Name select new PositionMatrixOption(m.PositionMatrixId,t.Name+" · "+j.MatrixCode,j.BasicSalary,m.ApprovedSlots){FrameworkId=j.FrameworkId??0}).ToListAsync();
        var rows=await db.Positions.Include(x=>x.PositionBasicSalaries).Where(x=>x.OrganisationId==actor.Org||x.IsGlobal||x.OrganisationId==null&&db.JobPositions.Any(a=>a.PositionId==x.PositionId&&a.Job.OrganisationID==actor.Org)).OrderBy(x=>x.Name).ToListAsync();var positions=new List<ManagedPosition>();
        foreach(var p in rows)
        {
            var count=await db.JobPositions.Where(x=>x.PositionId==p.PositionId&&x.Job.OrganisationID==actor.Org&&x.JobPositionStateId==4&&x.FromDate<=today&&(!x.ToDate.HasValue||x.ToDate>today)&&x.Job.JobStateId==4&&x.Job.JoinedDate<=today&&(!x.Job.TerminatedDate.HasValue||x.Job.TerminatedDate>today)).Select(x=>x.Job.IndividualID).Distinct().CountAsync();
            positions.Add(new(p.PositionId,p.Name,p.DhivehiName,p.Descriptions,p.IsActive!=false,p.IsGlobal,p.PositionFrameworkId,p.PositionMatrixId,frameworks.FirstOrDefault(x=>x.Id==p.PositionFrameworkId)?.Name??"Unclassified existing position",count,actor.Manage&&(p.OrganisationId==actor.Org&&!p.IsGlobal||actor.Super&&p.IsGlobal)){TypeId=p.PositionTypeId,RankId=p.PositionRankId,ClassificationId=p.PositionClassificationId,Salaries=p.PositionBasicSalaries.Where(x=>x.IsValid).OrderByDescending(x=>x.EffectiveDate).Select(x=>new PositionSalaryRow((decimal)x.Amount,x.EffectiveDate)).ToList()});
        }
        return new(frameworks,matrices,positions,actor.Manage,actor.Super){
            Types=await db.PositionTypes.Where(x=>x.IsActive!=false).OrderBy(x=>x.Name).Select(x=>new PositionLookup(x.PositionTypeId,x.Name)).ToListAsync(),
            Ranks=await db.PositionRanks.Where(x=>x.IsActive!=false&&x.OrganisationBusinessEntityID==actor.Org).OrderBy(x=>x.Name).Select(x=>new PositionLookup(x.PositionRankId,x.Name)).ToListAsync(),
            Classifications=await db.PositionClassifications.Where(x=>x.IsActive!=false&&(x.OrganisationBusinessEntityID==actor.Org||x.OrganisationBusinessEntityID==null)).OrderBy(x=>x.Name).Select(x=>new PositionLookup(x.PositionClassificationId,x.Name)).ToListAsync()};
    }
    public async Task<ServiceResult> SaveAsync(PositionInput input)
    {
        var actor=await ScopeAsync();if(!actor.Manage)throw new UnauthorizedAccessException("Position configuration permission is required.");
        if(string.IsNullOrWhiteSpace(input.Name)||input.Name.Trim().Length>250||input.DhivehiName?.Length>250||input.Description?.Length>4000)return ServiceResult.Fail("Enter a position name up to 250 characters and description up to 4,000 characters.");
        if(input.Global&&!actor.Super)throw new UnauthorizedAccessException("Only a superadministrator can manage global positions.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var framework=await db.PositionFrameworks.SingleOrDefaultAsync(x=>x.FrameworkId==input.FrameworkId&&x.IsActive)??throw new InvalidOperationException("Select an active position framework.");
        if(input.BasicSalary.HasValue!=input.SalaryEffectiveDate.HasValue||input.BasicSalary is <0||input.BasicSalary.HasValue&&(input.BasicSalary>999999999999.99m||decimal.Round(input.BasicSalary.Value,2)!=input.BasicSalary))return ServiceResult.Fail("Enter a nonnegative salary with up to two decimal places and its effective date.");
        if(input.TypeId.HasValue&&!await db.PositionTypes.AnyAsync(x=>x.PositionTypeId==input.TypeId&&x.IsActive!=false))return ServiceResult.Fail("Select an active position type.");
        if(input.RankId.HasValue&&!await db.PositionRanks.AnyAsync(x=>x.PositionRankId==input.RankId&&x.IsActive!=false&&x.OrganisationBusinessEntityID==actor.Org))return ServiceResult.Fail("Select an active rank in your organisation.");
        if(input.ClassificationId.HasValue&&!await db.PositionClassifications.AnyAsync(x=>x.PositionClassificationId==input.ClassificationId&&x.IsActive!=false&&(x.OrganisationBusinessEntityID==actor.Org||x.OrganisationBusinessEntityID==null)))return ServiceResult.Fail("Select an active classification in your organisation.");
        if(framework.Code=="NEW_PAY"||input.MatrixId.HasValue)
        {
            if(input.BasicSalary.HasValue)return ServiceResult.Fail("Government matrix salaries are managed through the Job Matrix and pay steps.");
            var today=DateTime.UtcNow.AddHours(5).Date;
            if(input.Global||!await (from m in db.PositionMatrices join j in db.FrameworkJobMatrices on m.JobMatrixId equals j.JobMatrixId where m.PositionMatrixId==input.MatrixId&&m.OrganisationId==actor.Org&&m.IsActive&&j.IsActive&&j.FrameworkId==input.FrameworkId&&m.EffectiveFrom<=today&&(!m.EffectiveTo.HasValue||m.EffectiveTo>=today) select m).AnyAsync())return ServiceResult.Fail("Select an active position matrix in this framework belonging to your organisation.");
        }
        else if(input.MatrixId.HasValue)return ServiceResult.Fail("CSC legacy and custom positions do not use a government matrix link.");
        var p=input.Id==0?new Position{OrganisationId=actor.Org}:await db.Positions.SingleOrDefaultAsync(x=>x.PositionId==input.Id)??throw new UnauthorizedAccessException();
        if(input.Id!=0&&!(p.OrganisationId==actor.Org&&!p.IsGlobal||actor.Super&&p.IsGlobal))throw new UnauthorizedAccessException("Position unavailable for editing in this organisation.");
        if(input.Id!=0&&(p.IsGlobal!=input.Global||p.PositionFrameworkId!=input.FrameworkId||p.PositionMatrixId!=input.MatrixId||p.PositionTypeId!=input.TypeId||p.PositionRankId!=input.RankId||p.PositionClassificationId!=input.ClassificationId)&&await db.JobPositions.AnyAsync(x=>x.PositionId==p.PositionId))return ServiceResult.Fail("This position has employment history. Create a new position to change its framework, classification, rank, type, matrix or global scope.");
        if(input.MatrixId.HasValue&&await db.Positions.AnyAsync(x=>x.PositionId!=input.Id&&x.PositionMatrixId==input.MatrixId))return ServiceResult.Fail("This matrix already has a position. Select the existing position for staff assignment.");
        if(input.BasicSalary.HasValue&&input.Id!=0&&await db.PositionBasicSalaries.AnyAsync(x=>x.PositionId==input.Id&&x.IsValid&&x.EffectiveDate>=input.SalaryEffectiveDate!.Value))return ServiceResult.Fail("Salary history cannot be overwritten. Select an effective date after the last salary record.");
        if(await db.Positions.AnyAsync(x=>x.PositionId!=input.Id&&x.OrganisationId==(input.Global?null:actor.Org)&&x.Name==input.Name.Trim()&&x.PositionFrameworkId==input.FrameworkId))return ServiceResult.Fail("This position name already exists in the selected framework.");
        var log=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Position {input.Id} configured in organisation {actor.Org}; framework {input.FrameworkId}, matrix {input.MatrixId}; active {input.Active}.");
        p.Name=input.Name.Trim();p.DhivehiName=input.DhivehiName?.Trim();p.Descriptions=input.Description?.Trim();p.IsActive=input.Active;p.IsGlobal=input.Global;p.OrganisationId=input.Global?null:actor.Org;p.PositionFrameworkId=input.FrameworkId;p.PositionMatrixId=input.MatrixId;p.OperationLogId=log.OperationLogId;
        p.PositionTypeId=input.TypeId;p.PositionRankId=input.RankId;p.PositionClassificationId=input.ClassificationId;
        if(input.BasicSalary.HasValue){p.IsSalaryGivenBasedOnPostion=true;p.PositionBasicSalaries.Add(new(){Amount=(double)input.BasicSalary.Value,EffectiveDate=input.SalaryEffectiveDate!.Value,IsValid=true,OperationLogId=log.OperationLogId});}
        if(input.Id==0)db.Positions.Add(p);await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Position saved. Existing staff assignments are unchanged.");
    }
}
