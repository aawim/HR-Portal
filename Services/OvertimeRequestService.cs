using System.Data;
using System.Text.Json;
using HRM.Models;
using HRM.DTOs;
using HRM.Helpers;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Components.Shared;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed record OvertimeRow(int Id,int StateId,string Reference,string Staff,DateTime Date,int Minutes,string Status,string Reason,bool CanApprove,bool CanCancel,bool CanPay,string? PaymentReference)
{ public string Source { get; init; } = "Legacy"; public DateTime? Start { get; init; } public DateTime? End { get; init; } public string? Task { get; init; } }
public sealed partial class OvertimeRequestService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IRequestService requests,IOperationLogService logs,IWorkflowEngine workflow,IWorkflowApproverResolver resolver,IWorkflowRequestContextResolver contexts)
{
    public const string TypeName="Overtime Claim Request";public const string PageUrl="/online/overtime-requests";
    private static DateTime Today=>DateTime.UtcNow.AddHours(5).Date;
    private WorkflowRequestProcessing Processing=>new(resolver,contexts);
    private static WorkflowRequestSubject Subject(OvertimeRequest r)=>new(r.Request,r.JobId,r.IndividualId,r.OrganisationId);
    private async Task<(int Org,int Person,int User,bool Manage,bool Pay)> ScopeAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();if(org<=0||!await access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Select your current organisation.");
        var admin=actor.IsSuperAdministrator||await access.HasPermissionAsync("Admin");return(org,actor.IndividualId,actor.UserId,admin||await access.HasPermissionAsync("Overtime.Review"),actor.IsSuperAdministrator||await access.HasPermissionAsync("Overtime.Pay"));
    }
    private static async Task<JobTypeOvertimeRule> ValidateAsync(HrmTeContext db,OvertimeRequest r)
    {
        var job=await db.Jobs.SingleOrDefaultAsync(j=>j.JobId==r.JobId&&j.OrganisationID==r.OrganisationId&&j.IndividualID==r.IndividualId&&j.JobStateId==4&&j.JoinedDate<=r.WorkDate&&(!j.TerminatedDate.HasValue||j.TerminatedDate>r.WorkDate))??throw new InvalidOperationException("Approved employment is required for the claimed date.");
        var policy=await db.JobTypeOvertimeRules.SingleOrDefaultAsync(p=>p.OrganisationId==r.OrganisationId&&p.JobTypeId==job.JobTypeId&&p.IsAllowed)??throw new InvalidOperationException("Overtime is not enabled for your Job Type. Contact HR.");
        if (r.ClaimSource != "Legacy")
        {
            await ValidateEvidenceAsync(db, r);
            await ValidateCapsAsync(db, r, policy);
            return policy;
        }
        var used=await db.OvertimeRequests.Where(x=>x.OrganisationId==r.OrganisationId&&x.IndividualId==r.IndividualId&&x.RequestId!=r.RequestId&&x.WorkDate>=r.WeekStart&&x.WorkDate<r.WeekStart.AddDays(7)&&(!x.Request.RequestState.IsFinalState||x.Request.RequestStateId==4)).Select(x=>new{x.WorkDate,x.Minutes}).ToListAsync();
        if(used.Sum(x=>x.Minutes)+r.Minutes>policy.MaximumWeeklyHours*60||policy.MaximumDailyHours.HasValue&&used.Where(x=>x.WorkDate==r.WorkDate).Sum(x=>x.Minutes)+r.Minutes>policy.MaximumDailyHours.Value*60)throw new InvalidOperationException("This claim exceeds the remaining daily or weekly OT allowance, including pending claims.");
        var facts=await AttendanceScheduleFacts.LoadAsync(db,r.IndividualId,r.WorkDate,r.WorkDate,r.OrganisationId);
        var planned=facts.Where(f=>f.Purpose=="Official").Select(f=>(f.ScheduledStart,f.ScheduledEnd)).ToList();
        var worked=facts.Where(f=>f.CheckIn.HasValue&&f.CheckOut.HasValue&&f.CheckOut>f.CheckIn&&f.CheckOut<=DateTime.UtcNow.AddHours(5)).Select(f=>(f.CheckIn!.Value,f.CheckOut!.Value)).ToList();
        var available=planned.Count==0?0:WorkHourCoverage.Calculate(planned,worked).PotentialOvertimeMinutes;
        if(used.Where(x=>x.WorkDate==r.WorkDate).Sum(x=>x.Minutes)+r.Minutes>available)throw new InvalidOperationException("The claim exceeds completed overtime outside official hours supported by paired clock events. Complete attendance corrections first.");
        return policy;
    }
    // Retained for older callers; arbitrary minute-based claims are no longer accepted.
    public Task<ServiceResult> SubmitAsync(DateTime date, int minutes, string reason) =>
        Task.FromResult(ServiceResult.Fail("Select saved start and end clock events to submit overtime."));
    public async Task<List<OvertimeRow>> LoadAsync(bool pendingOnly=false)
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();var source=await db.OvertimeRequests.Include(x=>x.Request).ThenInclude(x=>x.RequestState).Where(x=>x.OrganisationId==actor.Org&&(!pendingOnly||!x.Request.RequestState.IsFinalState)&&(actor.Manage||actor.Pay||x.IndividualId==actor.Person||db.WfRequestApprovals.Any(a=>a.ApproverUserId==actor.User&&a.RequestStageInstance.WorkflowRequestInstance.RequestId==x.RequestId))).OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync();
        var result=new List<OvertimeRow>();foreach(var row in source){var instance=await WorkflowRequestProcessing.Instance(db,row.RequestId);var name=await db.Individuals.Where(i=>i.BusinessEntityId==row.IndividualId).Select(i=>i.FirstNameEnglish+" "+i.LastNameEnglish).SingleAsync();result.Add(new(row.RequestId,row.Request.RequestStateId,row.Request.ReferenceNumber??$"#{row.RequestId}",name,row.WorkDate,row.Minutes,row.PaidAtUtc.HasValue?"Paid":(row.Request.RequestState.IsFinalState?"":"Processing · ")+row.Request.RequestState.StateName,row.Reason,await Processing.CanApprove(db,Subject(row),instance,actor.User,actor.Person),row.IndividualId==actor.Person&&!row.Request.RequestState.IsFinalState&&instance?.WorkflowStatus=="InProgress",actor.Pay&&row.Request.RequestStateId==4&&!row.PaidAtUtc.HasValue&&row.IndividualId!=actor.Person,row.PaymentReference) { Source = row.ClaimSource, Start = row.StartedAt, End = row.EndedAt,
            Task = row.PreapprovalTaskId.HasValue ? await db.OvertimeWorkTasks.Where(t => t.Id == row.PreapprovalTaskId).Select(t => t.Description).SingleOrDefaultAsync() : null });}return result;
    }
    public async Task<ServiceResult> ActAsync(int id,int expectedState,string action,string? comment)
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try{var row=await db.OvertimeRequests.Include(x=>x.Request).ThenInclude(x=>x.RequestState).SingleOrDefaultAsync(x=>x.RequestId==id&&x.OrganisationId==actor.Org)??throw new UnauthorizedAccessException("Claim unavailable in this organisation.");
            await Processing.ExecuteAsync(db,Subject(row),expectedState,action,comment,actor.User,actor.Person,async()=>{await ValidateAsync(db,row);},async()=>{var policy=await ValidateAsync(db,row);row.ApprovedPolicyJson=JsonSerializer.Serialize(new{policy.Id,policy.Version,policy.MaximumWeeklyHours,policy.MaximumDailyHours});});await tx.CommitAsync();return ServiceResult.Ok("Overtime claim action saved.");}
        catch(InvalidOperationException ex){await tx.RollbackAsync();if(ex is WorkflowConfigurationException config)await config.NotifyAsync(factory);if(ex is RequestTransitionConfigurationException transition){var row=await db.OvertimeRequests.AsNoTracking().SingleAsync(x=>x.RequestId==id&&x.OrganisationId==actor.Org);await new WorkflowConfigurationException(await contexts.ResolveAsync(db,id,row.JobId),transition.Detail){ConfigurationUrl="/admin/settings/config/state-transitions"}.NotifyAsync(factory);}return ServiceResult.Fail(ex.Message);}
    }
    public async Task<ServiceResult> RecordPaymentAsync(int id,string reference)
    {
        var actor=await ScopeAsync();if(!actor.Pay)throw new UnauthorizedAccessException("You do not have permission to record OT payments.");if(string.IsNullOrWhiteSpace(reference)||reference.Length>200)return ServiceResult.Fail("Enter a payroll/payment reference of up to 200 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var row=await db.OvertimeRequests.Include(x=>x.Request).SingleOrDefaultAsync(x=>x.RequestId==id&&x.OrganisationId==actor.Org)??throw new UnauthorizedAccessException("Claim unavailable.");
        if(row.IndividualId==actor.Person||row.Request.RequestStateId!=4||row.PaidAtUtc.HasValue||row.ApprovedPolicyJson==null)return ServiceResult.Fail("Only an independently approved, unpaid OT claim can be marked paid.");
        row.PaidAtUtc=DateTime.UtcNow;row.PaidByUserId=actor.User;row.PaymentReference=reference.Trim();await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"OT claim {id}, organisation {actor.Org}: payment recorded with reference {reference.Trim()}.");var recipients=await db.Users.Where(u=>u.BusinessEntityID==row.IndividualId&&u.UserOrganisation!.BusinessEntityID==actor.Org&&u.IsActive).Select(u=>u.UserId).ToListAsync();foreach(var recipient in recipients)db.StaffNotifications.Add(new(){UserId=recipient,IndividualId=row.IndividualId,OrganisationId=actor.Org,Kind="Overtime",Message="Payment has been recorded for your approved overtime claim.",Url=PageUrl,CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Overtime payment reference recorded.");
    }
}
