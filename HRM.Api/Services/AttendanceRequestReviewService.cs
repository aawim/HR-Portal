using System.Data;
using HRM.Application.Contracts.Pages;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Attendance.Abstraction;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed class AttendanceRequestReviewService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,
    IWorkflowApproverResolver resolver,IWorkflowRequestContextResolver contexts,IOperationLogService logs,
    IAttendanceLogProcessor processor,ILogger<AttendanceRequestReviewService> logger) : IAttendanceRequestReview
{
    private WorkflowRequestProcessing Processing=>new(resolver,contexts);
    private sealed record Subject(Request Request,AttendanceLog Log,Job Job,bool Correction)
    {public WorkflowRequestSubject Workflow=>new(Request,Job.JobId,Log.IndividualId,Log.OrganisationId);}
    private async Task<Subject> LoadAsync(HrmTeContext db,int id,int org)
    {
        var links=await db.AttendanceLogRequests.Where(x=>x.RequestId==id).Select(x=>x.AttendanceLogId).ToListAsync();
        var corrections=await db.AttendanceLogChangeRequests.Where(x=>x.RequestId==id).Select(x=>x.AttendanceLogId).ToListAsync();
        if(links.Count+corrections.Count!=1)throw new InvalidOperationException("The request needs one attendance event before it can be reviewed. Please contact HR.");
        var log=await db.AttendanceLogs.SingleAsync(x=>x.AttendanceLogId==(links.Count==1?links[0]:corrections[0]));
        var request=await db.Requests.Include(x=>x.RequestState).SingleAsync(x=>x.RequestId==id);
        if(log.OrganisationId!=org||request.OrganisationBusinessEntityID!=org||request.ApplicantBusinessEntityId!=log.IndividualId)throw new UnauthorizedAccessException("Request unavailable in this organisation.");
        var jobs=await db.RequestJobs.Where(x=>x.RequestId==id&&x.IsValid&&x.Job.OrganisationID==org&&x.Job.IndividualID==log.IndividualId).Select(x=>x.Job).ToListAsync();
        if(jobs.Count==0)
        {
            var today=DateTime.UtcNow.AddHours(5).Date;
            jobs=await db.Jobs.Where(x=>x.IndividualID==log.IndividualId&&x.OrganisationID==org&&x.JobStateId==4&&x.JoinedDate<=today&&(!x.TerminatedDate.HasValue||x.TerminatedDate>today)).ToListAsync();
        }
        if(jobs.Count!=1)throw new InvalidOperationException("The request's employment link is ambiguous or unavailable. Please contact HR.");
        return new(request,log,jobs[0],corrections.Count==1);
    }
    private async Task<(HRM.DTOs.UserContext.UserContextDto Actor,int Org)> ScopeAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Use your current organisation.");return(actor,org);
    }
    public async Task<AttendanceReview> GetAsync(int requestId)
    {
        var(actor,org)=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();var row=await LoadAsync(db,requestId,org);
        var assigned=await db.WfRequestApprovals.AnyAsync(a=>a.ApproverUserId==actor.UserId&&a.RequestStageInstance.WorkflowRequestInstance.RequestId==requestId);
        if(actor.IndividualId!=row.Log.IndividualId&&!assigned&&!actor.IsSuperAdministrator&&!await access.HasPermissionAsync("Attendance.View")&&!await access.HasPermissionAsync("Admin"))throw new UnauthorizedAccessException();
        var instance=await WorkflowRequestProcessing.Instance(db,requestId);var stage=instance?.CurrentStage?.WorkflowStage.StageName??"";
        var canApprove=await Processing.CanApprove(db,row.Workflow,instance,actor.UserId,actor.IndividualId);
        var canCancel=row.Log.IndividualId==actor.IndividualId&&!row.Request.RequestState.IsFinalState&&instance?.WorkflowStatus=="InProgress";
        return new(requestId,row.Request.RequestStateId,stage,canApprove,canCancel,
            stage.Contains("verif",StringComparison.OrdinalIgnoreCase)||row.Request.RequestState.StateName.Contains("verif",StringComparison.OrdinalIgnoreCase)?"Verify stage":"Approve stage",
            canApprove||canCancel?"":row.Request.RequestState.IsFinalState?"This request is final.":instance==null?"The approval workflow is unavailable. Please contact HR.":"This request is awaiting its assigned reviewer.");
    }
    public async Task<ServiceResult> ActAsync(int requestId,int expectedState,string action,string? comment)
    {
        var(actor,org)=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var approvedLogId=0;
        try
        {
            var row=await LoadAsync(db,requestId,org);
            await Processing.ExecuteAsync(db,row.Workflow,expectedState,action,comment,actor.UserId,actor.IndividualId,
                async()=>
                {
                    var today=DateTime.UtcNow.AddHours(5).Date;
                    if(row.Job.JobStateId!=4||row.Job.TerminatedDate.HasValue&&row.Job.TerminatedDate<=today)throw new InvalidOperationException("Employment ended. This request cannot be approved.");
                    if(row.Request.RequestTypeId!=(row.Correction?SharedConfig.RequestTypes.ATTENDANCE_LOG_CHANGE_REQUEST:SharedConfig.RequestTypes.ATTENDANCE_LOG_REQUEST))throw new InvalidOperationException("The attendance request type does not match its linked event.");
                    var eligibility=await AttendanceRequestService.DateInfoAsync(db,row.Log.IndividualId,DateOnly.FromDateTime(row.Log.Date),row.Correction,org);
                    if(!eligibility.CanSubmit||eligibility.IsEarlyReturn)throw new InvalidOperationException(eligibility.Message);
                    if(row.Correction)
                    {
                        if(!row.Log.RelatedAttendanceLogId.HasValue||!await db.AttendanceLogs.AnyAsync(x=>x.AttendanceLogId==row.Log.RelatedAttendanceLogId&&x.IndividualId==row.Log.IndividualId&&x.OrganisationId==org&&x.Date.Date==row.Log.Date.Date&&x.AttendanceLogStateId==SharedConfig.AttendanceLogStates.Valid))throw new InvalidOperationException("The original clock event changed. Reject this request and submit an updated correction.");
                    }
                },async()=>
                {
                    var audit=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.ATTENDANCE_LOG_CREATE,$"Attendance request {requestId} approved by user {actor.UserId}.");
                    if(row.Correction)
                    {
                        var original=await db.AttendanceLogs.SingleAsync(x=>x.AttendanceLogId==row.Log.RelatedAttendanceLogId);
                        original.AttendanceLogStateId=SharedConfig.AttendanceLogStates.Invalid;original.OperationLogId=audit.OperationLogId;
                        foreach(var resolution in await db.AttendanceLogResolutions.Where(x=>x.AttendanceLogId==original.AttendanceLogId&&x.IsValid).ToListAsync())resolution.IsValid=false;
                    }
                    row.Log.AttendanceLogStateId=SharedConfig.AttendanceLogStates.Valid;row.Log.OperationLogId=audit.OperationLogId;approvedLogId=row.Log.AttendanceLogId;
                    foreach(var resolution in await db.AttendanceLogResolutions.Where(x=>x.AttendanceLogId==row.Log.AttendanceLogId&&x.IsValid).ToListAsync())resolution.IsValid=false;
                });
            await tx.CommitAsync();
        }
        catch(InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            if(ex is WorkflowConfigurationException configuration)await configuration.NotifyAsync(factory);
            if(ex is RequestTransitionConfigurationException transition){var row=await LoadAsync(db,requestId,org);var context=await contexts.ResolveAsync(db,requestId,row.Job.JobId);await new WorkflowConfigurationException(context,transition.Detail){ConfigurationUrl="/admin/settings/config/state-transitions"}.NotifyAsync(factory);}
            return ServiceResult.Fail(ex.Message);
        }
        catch(DbUpdateException){await tx.RollbackAsync();return ServiceResult.Fail("The request changed. Refresh before trying again.");}
        if(approvedLogId>0)
        {
            try{var result=await processor.ProcessAsync(approvedLogId);if(!result.Success){logger.LogWarning("Approved attendance log {LogId} needs resolution processing: {Message}",approvedLogId,result.Message);return ServiceResult.Ok("Request approved. Attendance processing is pending; please contact HR if the clock event does not appear.");}}catch(Exception ex){logger.LogError(ex,"Approved attendance log {LogId} needs resolution processing",approvedLogId);return ServiceResult.Ok("Request approved. Attendance processing is pending; please contact HR if the clock event does not appear.");}
        }
        return ServiceResult.Ok("Attendance request action saved.");
    }
}
