using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed partial class StaffPositionManagementService
{
    internal static readonly IReadOnlyDictionary<string,int> TerminationStates=new Dictionary<string,int>(StringComparer.Ordinal)
    {
        ["Job Termination Request"]=15,["Job Resignation Request"]=7,["Job Retirement Request"]=8,
        ["Job Temporary Termination Request"]=15,["Job Termination Due To Promotion Request"]=16,
        ["Job Termination Due To Contract Expiration"]=17,["Job Termination During Probation Period Request"]=18,
        ["Job Terminated"]=15,["Job Terminated other"]=19
    };

    private static async Task ValidateTerminationAsync(HrmTeContext db,int jobId,DateTime end)
    {
        var job=await db.Jobs.SingleAsync(x=>x.JobId==jobId);
        if(job.JobStateId!=4||job.TerminatedDate.HasValue)throw new InvalidOperationException("This employment has already ended or has a termination scheduled.");
        if(end<=job.JoinedDate.Date)throw new InvalidOperationException("The termination effective date must be after the joining date.");
        if(await db.JobPositions.AnyAsync(x=>x.JobId==jobId&&x.JobPositionStateId==4&&x.FromDate>=end))throw new InvalidOperationException("Resolve approved position assignments starting on or after the termination date before submitting.");
        if(await db.FrameworkJobStepHistories.AnyAsync(x=>x.JobId==jobId&&x.EffectiveFrom>=end))throw new InvalidOperationException("Resolve future pay-step assignments before terminating employment.");
    }

    internal async Task ProcessRequestedChangeAsync(HrmTeContext db,StaffPositionChangeRequest r,bool apply)
    {
        var typeName=await db.RequestTypes.Where(x=>x.RequestTypeId==r.Request.RequestTypeId).Select(x=>x.TypeName).SingleAsync();
        var termination=r.ChangeKind=="Remove"&&TerminationStates.ContainsKey(typeName);
        await ValidateAndApplyAsync(db,r.OrganisationId,r.IndividualId,r.JobId,r.PositionId,r.FromDate,r.ToDate,r.Reason,Original(r),r.ChangeKind=="Change",apply&&!termination,r.RequestId,r.JobMatrixStepId);
        if(!termination)return;
        var end=r.ToDate??throw new InvalidOperationException("Select a termination effective date.");
        await ValidateTerminationAsync(db,r.JobId,end);
        if(!apply)return;
        var audit=await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.JOB_POSITION_UPDATE,$"Approve {typeName}: request {r.RequestId}, job {r.JobId}, organisation {r.OrganisationId}, termination effective {end:yyyy-MM-dd}. {r.Reason}");
        var job=await db.Jobs.SingleAsync(x=>x.JobId==r.JobId);
        job.TerminatedDate=end;job.OperationLogId=audit.OperationLogId;
        // Keep future employment approved; its active period is bounded by TerminatedDate.
        if(end<=DateTime.UtcNow.AddHours(5).Date)job.JobStateId=TerminationStates[typeName];
        foreach(var assignment in await db.JobPositions.Where(x=>x.JobId==r.JobId&&x.JobPositionStateId==4&&(!x.ToDate.HasValue||x.ToDate>end)).ToListAsync())
        {assignment.ToDate=end;assignment.OperationLogId=audit.OperationLogId;}
        foreach(var pay in await db.FrameworkJobStepHistories.Where(x=>x.JobId==r.JobId&&(!x.EffectiveTo.HasValue||x.EffectiveTo>end)).ToListAsync())
        {pay.EffectiveTo=end;pay.OperationLogId=audit.OperationLogId;}
        await db.SaveChangesAsync();
    }
}
