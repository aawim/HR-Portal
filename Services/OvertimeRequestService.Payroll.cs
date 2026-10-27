using System.Data;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.DTOs;
using HRM.Components.Shared;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class OvertimeRequestService
{
    private static Task<bool> IsPayrollLockedAsync(HrmTeContext db,OvertimeRequest row) =>
        row.PaidAtUtc.HasValue ? Task.FromResult(true) : db.OvertimePayrollItems.AnyAsync(x=>x.RequestId==row.RequestId);
    private static async Task<bool> IsSupervisorAsync(HrmTeContext db,OvertimeRequest row,int person) =>
        row.IndividualId!=person && (await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db,row.JobId)).Contains(person);
    private static async Task<List<int>> SupervisorJobsAsync(HrmTeContext db,int org,int person)
    {
        var departments=db.OrganisationStructureSupervisors.Where(x=>x.SupervisorStaffId==person&&x.IsActive&&x.IsValid).Select(x=>x.OrganisationStructureId);
        var teams=db.TeamStaffs.Where(x=>x.StaffId==person&&x.IsSuperVisor&&x.IsValid&&x.StartDate<=Today&&(!x.EndDate.HasValue||x.EndDate>=Today)&&x.Team.OrganisationId==org&&x.Team.IsValid==true).Select(x=>x.TeamId);
        var members=db.TeamStaffs.Where(x=>teams.Contains(x.TeamId)&&x.IsValid&&x.StartDate<=Today&&(!x.EndDate.HasValue||x.EndDate>=Today)).Select(x=>x.StaffId);
        var jobs=await db.Jobs.Where(x=>x.OrganisationID==org&&x.IndividualID!=person&&(departments.Contains(x.OrganisationStructureId)||members.Contains(x.IndividualID))&&db.OvertimeRequests.Any(r=>r.JobId==x.JobId)).Select(x=>x.JobId).ToListAsync();
        var result=new List<int>();
        foreach(var job in jobs)if((await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db,job)).Contains(person))result.Add(job);
        return result;
    }
    public async Task<ServiceResult> ProcessPayrollAsync(IEnumerable<int> requestIds,DateTime month,string reference,Guid clientBatchId)
    {
        var actor=await ScopeAsync();
        if(!actor.Pay)throw new UnauthorizedAccessException("You do not have permission to process overtime payroll.");
        var ids=requestIds.Distinct().Order().ToArray();
        if(ids.Length is <1 or >500 || month.Year<2000 || month.Year>2100 || string.IsNullOrWhiteSpace(reference)||reference.Trim().Length>200||clientBatchId==Guid.Empty)
            return ServiceResult.Fail("Select approved claims, a payroll month and a reference of up to 200 characters.");
        month=new DateTime(month.Year,month.Month,1);reference=reference.Trim();
        await using var db=await factory.CreateDbContextAsync();
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var existing=await db.OvertimePayrollBatches.Include(x=>x.Items).SingleOrDefaultAsync(x=>x.OrganisationId==actor.Org&&x.ClientBatchId==clientBatchId);
            if(existing!=null)
                return existing.ProcessedByUserId==actor.User&&existing.PayrollMonth==month&&existing.Reference==reference&&existing.Items.Select(x=>x.RequestId).Order().SequenceEqual(ids)
                    ? ServiceResult.Ok("Overtime payroll was already processed.") : ServiceResult.Fail("This payroll submission has changed. Refresh and try again.");
            var claims=await db.OvertimeRequests.Include(x=>x.Request).Where(x=>ids.Contains(x.RequestId)&&x.OrganisationId==actor.Org).ToListAsync();
            if(claims.Count!=ids.Length)throw new InvalidOperationException("Some selected claims are unavailable in this organisation. Refresh and try again.");
            foreach(var claim in claims)
                if(claim.Request.RequestStateId!=4||claim.IndividualId==actor.Person||string.IsNullOrWhiteSpace(claim.ApprovedPolicyJson)||await IsPayrollLockedAsync(db,claim))
                    throw new InvalidOperationException("Only independently approved claims that have not been processed can be included. Refresh your selection.");
            var log=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"OT payroll {month:yyyy-MM}: {reference}; claims {string.Join(",",ids)}; processed by user {actor.User}, organisation {actor.Org}.");
            var batch=new OvertimePayrollBatch{OrganisationId=actor.Org,PayrollMonth=month,Reference=reference,ClientBatchId=clientBatchId,ProcessedByUserId=actor.User,ProcessedAtUtc=DateTime.UtcNow,OperationLogId=log.OperationLogId};
            foreach(var claim in claims)
            {
                batch.Items.Add(new(){RequestId=claim.RequestId,IndividualId=claim.IndividualId,JobId=claim.JobId,WorkDate=claim.WorkDate,Minutes=claim.Minutes,RequestReference=claim.Request.ReferenceNumber??$"#{claim.RequestId}"});
                await NotifyPayrollAsync(db,claim,$"Overtime {claim.Request.ReferenceNumber??$"#{claim.RequestId}"}: Payroll processed for {month:MMMM yyyy}.");
            }
            db.OvertimePayrollBatches.Add(batch);await db.SaveChangesAsync();await tx.CommitAsync();
            return ServiceResult.Ok($"Payroll processed for {claims.Count} overtime claim(s), {month:MMMM yyyy}.");
        }
        catch(InvalidOperationException ex){await tx.RollbackAsync();return ServiceResult.Fail(ex.Message);}
        catch(DbUpdateException){await tx.RollbackAsync();return ServiceResult.Fail("The selection changed or has already been processed. Refresh and try again.");}
    }
    private static async Task NotifyPayrollAsync(HrmTeContext db,OvertimeRequest row,string message)
    {
        var users=await db.Users.Where(u=>u.BusinessEntityID==row.IndividualId&&u.UserOrganisation!.BusinessEntityID==row.OrganisationId&&u.IsActive).Select(u=>u.UserId).ToListAsync();
        foreach(var user in users)db.StaffNotifications.Add(new(){UserId=user,IndividualId=row.IndividualId,OrganisationId=row.OrganisationId,Kind="Overtime",Message=message,Url=PageUrl,CreatedAtUtc=DateTime.UtcNow});
    }
    private async Task ChangeApprovedOrReturnedAsync(HrmTeContext db,OvertimeRequest row,int expected,string action,string? comment,int user,int person)
    {
        var from=row.Request.RequestStateId;
        var to=action switch {"Cancel"=>5,"Reject"=>6,"Incomplete"=>1,_=>throw new InvalidOperationException("Unsupported action for an approved claim.")};
        if(from!=expected||from is not (4 or 1)||await IsPayrollLockedAsync(db,row))throw new InvalidOperationException("The claim changed. Refresh before acting.");
        var instance=await WorkflowRequestProcessing.Instance(db,row.RequestId)??throw new InvalidOperationException("The approval history is unavailable. Contact HR.");
        if(from==1 && (instance.WorkflowStatus!="Returned"||action!="Cancel"))throw new InvalidOperationException("The claim is not available for this action.");
        if(!(action=="Cancel"&&row.IndividualId==person)&&!(from==4&&await IsSupervisorAsync(db,row,person)))
            throw new UnauthorizedAccessException("Only the requesting staff member can cancel, or their current supervisor can revise an approved claim before payroll processing.");
        if(string.IsNullOrWhiteSpace(comment)||comment.Length>2000)throw new InvalidOperationException("Enter an action comment of up to 2,000 characters.");
        // Only approved OT claims may reopen a final approval, and only across an explicitly configured edge.
        if(!await db.RequestTypesAllowedRequestStateTransitions.AnyAsync(x=>x.RequestTypeId==row.Request.RequestTypeId&&x.FromRequestStateId==from&&x.ToRequestStateId==to))
            throw new RequestTransitionConfigurationException($"Transition not allowed for {TypeName}: {from} → {to}. Configure this From State / To State in Settings > State Transitions.");
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"OT claim {row.RequestId}: {from} → {to}; actor {user}; {comment}; previous approved policy: {row.ApprovedPolicyJson}");
        row.Request.RequestStateId=to;row.Request.LastStateChangedByUserId=user;row.Request.LastStateChangedDate=DateTime.UtcNow;row.Request.StateChangeRemarks=comment.Trim()[..Math.Min(comment.Trim().Length,500)];
        instance.WorkflowStatus=to==1?"Returned":to==5?"Cancelled":"Rejected";instance.CompletedAt=DateTime.UtcNow;
        if(to==1)row.ApprovedPolicyJson=null;
        db.WfRequestActions.Add(new(){WorkflowRequestInstance=instance,RequestStageInstance=instance.CurrentStage,PerformedByUserId=user,ActionCode=action,FromRequestStateId=from,ToRequestStateId=to,Comments=comment.Trim(),PerformedAt=DateTime.UtcNow});
        await NotifyPayrollAsync(db,row,$"Overtime {row.Request.ReferenceNumber}: {instance.WorkflowStatus}. {comment.Trim()[..Math.Min(comment.Trim().Length,300)]}");
        await db.SaveChangesAsync();
    }
    public async Task<ServiceResult> ResubmitClaimAsync(int id,string reason)
    {
        var actor=await ScopeAsync();
        if(string.IsNullOrWhiteSpace(reason)||reason.Trim().Length>1000)return ServiceResult.Fail("Enter work performed / reason of up to 1,000 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var row=await db.OvertimeRequests.Include(x=>x.Request).ThenInclude(x=>x.RequestState).SingleOrDefaultAsync(x=>x.RequestId==id&&x.OrganisationId==actor.Org&&x.IndividualId==actor.Person)??throw new UnauthorizedAccessException("Claim unavailable.");
            var instance=await WorkflowRequestProcessing.Instance(db,id);
            if(row.Request.RequestStateId!=1||instance?.WorkflowStatus!="Returned"||await IsPayrollLockedAsync(db,row))throw new InvalidOperationException("Only your returned, unprocessed claim can be resubmitted.");
            await ValidateAsync(db,row);
            var stages=instance.WorkflowDefinition.Stages.Where(s=>s.IsActive).OrderBy(s=>s.StageOrder).ToList();
            if(stages.Count==0)throw new InvalidOperationException("The approval flow is unavailable. Contact HR.");
            foreach(var stage in stages)if((await Processing.Candidates(db,Subject(row),stage.WorkflowStageId)).Count==0)throw new InvalidOperationException("An approval stage has no eligible approver. Contact HR.");
            var first=stages[0];await RequestTransitionPolicy.RequireAllowedAsync(db,row.Request.RequestTypeId,1,first.RequestStateId);
            var candidates=await Processing.Candidates(db,Subject(row),first.WorkflowStageId);
            var attempt=(await db.WfRequestStageInstances.Where(s=>s.WorkflowRequestInstanceId==instance.WorkflowRequestInstanceId&&s.WorkflowStageId==first.WorkflowStageId).Select(s=>(int?)s.AttemptNumber).MaxAsync()??0)+1;
            var opened=new WfRequestStageInstance{WorkflowRequestInstance=instance,WorkflowStage=first,AttemptNumber=attempt,StartedAt=DateTime.UtcNow};
            foreach(var c in candidates)opened.Approvals.Add(new(){ApproverUserId=c.UserId,StageApproverId=c.StageApproverId,ApprovalSequence=c.SequenceNumber,AssignedAt=DateTime.UtcNow});
            db.WfRequestStageInstances.Add(opened);await db.SaveChangesAsync();
            instance.CurrentStage=opened;instance.WorkflowStatus="InProgress";instance.ExecutionStatus="NotStarted";instance.CompletedAt=null;
            row.Reason=reason.Trim();row.Request.RequestStateId=first.RequestStateId;row.Request.LastStateChangedByUserId=actor.User;row.Request.LastStateChangedDate=DateTime.UtcNow;row.Request.StateChangeRemarks=("Resubmitted: "+reason.Trim())[..Math.Min(13+reason.Trim().Length,500)];
            db.WfRequestActions.Add(new(){WorkflowRequestInstance=instance,RequestStageInstance=opened,PerformedByUserId=actor.User,ActionCode="Resubmit",FromRequestStateId=1,ToRequestStateId=first.RequestStateId,Comments=reason.Trim(),PerformedAt=DateTime.UtcNow});
            foreach(var c in candidates.Where(c=>first.ApprovalMode!="Sequential"||c.SequenceNumber==candidates.Min(x=>x.SequenceNumber)))db.RequestNotifications.Add(new(){RequestId=id,UserId=c.UserId,NotificationKind="ActionRequired",RequestStageInstanceId=opened.RequestStageInstanceId,FromStateId=1,ToStateId=first.RequestStateId,ReferenceNumber=row.Request.ReferenceNumber??$"#{id}",CreatedAtUtc=DateTime.UtcNow});
            await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Overtime claim resubmitted for approval.");
        }
        catch(InvalidOperationException ex){await tx.RollbackAsync();return ServiceResult.Fail(ex is RequestTransitionConfigurationException?"The approval flow needs HR assistance. No changes have been made.":ex.Message);}
    }
}
