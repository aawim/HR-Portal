using HRM.Models;
using Microsoft.EntityFrameworkCore;
using HRM.Application.Contracts.Pages;
namespace HRM.Services;

// Called inside the existing serializable approval transaction, including onboarding.
internal static class PositionMatrixAssignments
{
    internal static async Task<PositionPayInfo?> SummaryAsync(HrmTeContext db,int positionId,int? selectedStepId=null)
    {
        var matrixId=await db.Positions.Where(x=>x.PositionId==positionId).Select(x=>x.PositionMatrixId).SingleAsync();if(!matrixId.HasValue)return null;
        var pay=await(from p in db.PositionMatrices join m in db.FrameworkJobMatrices on p.JobMatrixId equals m.JobMatrixId  where p.PositionMatrixId==matrixId select new{m.JobMatrixId,m.CurrencyTypeId,Salary=m.BasicSalary}).SingleAsync();
        var currency=await db.FinanceCurrencies.Where(x=>x.CurrencyTypeId==pay.CurrencyTypeId).Select(x=>x.Name).SingleOrDefaultAsync()??"Currency not configured";
        var benefits=await db.FrameworkJobMatrixBenefits.Where(x=>x.JobMatrixId==pay.JobMatrixId).Select(x=>new MatrixBenefitInput(x.BenefitName,x.Amount,x.FrequencyCode??"Monthly")).ToListAsync();
        var steps=await db.FrameworkJobMatrixSteps.Where(x=>x.JobMatrixId==pay.JobMatrixId&&x.IsActive).OrderBy(x=>x.StepNumber).Select(x=>new MatrixStepOption(x.JobMatrixStepId,x.StepNumber,x.BasicSalaryOverride??pay.Salary,x.JobAllowance,x.IsActive)).ToListAsync(); var selected=steps.FirstOrDefault(x=>x.Id==selectedStepId); return new(currency,selected?.BasicSalary??pay.Salary,selected?.JobAllowance??0,benefits){Steps=steps,SelectedStepId=selectedStepId};
    }
    internal sealed record Pay(PositionMatrix Matrix,FrameworkJobMatrix JobMatrix,FrameworkJobMatrixStep? Step);
    internal static async Task<Pay?> ValidateAsync(HrmTeContext db,int org,int positionId,DateTime from,DateTime? to,int? excluding=null,int? selectedStepId=null)
    {
        if(from.Year<1900||to.HasValue&&to<=from)throw new InvalidOperationException("Choose valid position dates.");
        var position=await db.Positions.SingleAsync(x=>x.PositionId==positionId);
        if(!position.PositionMatrixId.HasValue)return null;
        var matrix=await db.PositionMatrices.SingleOrDefaultAsync(x=>x.PositionMatrixId==position.PositionMatrixId&&x.OrganisationId==org&&x.IsActive)
            ??throw new InvalidOperationException("The position matrix is unavailable in this organisation.");
        if(db.Database.IsSqlServer())await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource={"HRM.PositionMatrix:"+matrix.PositionMatrixId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; IF @result<0 THROW 51062,'Position assignments are busy. Please try again.',1;");
        var jobMatrix=await db.FrameworkJobMatrices.SingleAsync(x=>x.JobMatrixId==matrix.JobMatrixId);
        var stepId=selectedStepId??matrix.JobMatrixStepId; var step=stepId.HasValue?await db.FrameworkJobMatrixSteps.SingleOrDefaultAsync(x=>x.JobMatrixStepId==stepId&&x.JobMatrixId==matrix.JobMatrixId):null; if(stepId.HasValue&&step==null)throw new InvalidOperationException("Select a pay step belonging to this Job Matrix.");
        if(matrix.ApprovedSlots<1)throw new InvalidOperationException("This position matrix has no approved staffing slots available.");
        if(!jobMatrix.IsActive||step is {IsActive:false}||!jobMatrix.CurrencyTypeId.HasValue||jobMatrix.FrameworkId!=position.PositionFrameworkId||!await db.PositionFrameworks.AnyAsync(x=>x.FrameworkId==jobMatrix.FrameworkId&&x.IsActive)||!await db.FrameworkJobTitles.AnyAsync(x=>x.JobTitleId==matrix.JobTitleId&&x.IsActive)||matrix.EffectiveFrom>from||matrix.EffectiveTo.HasValue&&(!to.HasValue||to.Value.AddDays(-1)>matrix.EffectiveTo)||jobMatrix.EffectiveFrom>from||jobMatrix.EffectiveTo.HasValue&&(!to.HasValue||to.Value.AddDays(-1)>jobMatrix.EffectiveTo))
            throw new InvalidOperationException("Choose position dates within the active matrix period.");
        var periods=await(from a in db.JobPositions join p in db.Positions on a.PositionId equals p.PositionId where p.PositionMatrixId==matrix.PositionMatrixId&&a.JobPositionStateId==4&&a.JobPositionId!=excluding&&(!to.HasValue||a.FromDate<to.Value)&&(!a.ToDate.HasValue||a.ToDate>@from) select new{a.FromDate,a.ToDate}).ToListAsync();
        var events=new List<(DateTime Day,int Change)>();
        foreach(var p in periods){events.Add((p.FromDate<from?from:p.FromDate,1));if(p.ToDate.HasValue)events.Add((p.ToDate.Value,-1));}
        var used=0;foreach(var day in events.GroupBy(x=>x.Day).OrderBy(x=>x.Key)){used+=day.Sum(x=>x.Change);if(used>=matrix.ApprovedSlots)throw new InvalidOperationException("This position matrix has no approved staffing slots available for the selected dates.");}
        return new(matrix,jobMatrix,step);
    }
    internal static async Task SnapshotAsync(HrmTeContext db,int org,HRM.Models.JobPosition assignment,int audit,bool changing=false,int? selectedStepId=null)
    {
        var pay=await ValidateAsync(db,org,assignment.PositionId,assignment.FromDate,assignment.ToDate,assignment.JobPositionId,selectedStepId);if(pay==null)return; if(pay.Step==null)throw new InvalidOperationException("Select the employee job pay step before approving this assignment.");
        var overlapping=await db.FrameworkJobStepHistories.Where(x=>x.JobId==assignment.JobId&&(!assignment.ToDate.HasValue||x.EffectiveFrom<assignment.ToDate)&&(!x.EffectiveTo.HasValue||x.EffectiveTo>assignment.FromDate)).ToListAsync();
        foreach(var old in overlapping)
        {
            if(!changing||old.EffectiveFrom>=assignment.FromDate)throw new InvalidOperationException("This employment already has a pay-step assignment for the selected dates. Use the position-change process.");
            old.EffectiveTo=assignment.FromDate;
        }
        if(overlapping.Count>0)await db.SaveChangesAsync();
        db.FrameworkJobStepHistories.Add(new(){JobId=assignment.JobId,PositionMatrixId=pay.Matrix.PositionMatrixId,JobMatrixStepId=pay.Step.JobMatrixStepId,EffectiveFrom=assignment.FromDate,EffectiveTo=assignment.ToDate,BasicSalarySnapshot=pay.Step.BasicSalaryOverride??pay.JobMatrix.BasicSalary,JobAllowanceSnapshot=pay.Step.JobAllowance,OperationLogId=audit});
        db.JobPositionBasicSalaries.Add(new(){JobPoistionId=assignment.JobPositionId,Amount=(double)(pay.Step.BasicSalaryOverride??pay.JobMatrix.BasicSalary),EffectiveDate=DateOnly.FromDateTime(assignment.FromDate),IsValid=true,OperationLogId=audit});
    }
}
