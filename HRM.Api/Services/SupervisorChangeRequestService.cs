using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;


public sealed class SupervisorChangeRequestService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IRequestService requests,IOperationLogService logs,IWorkflowEngine workflow,IWorkflowApproverResolver resolver,IWorkflowRequestContextResolver contexts)
:global::HRM.Services.Contracts.SupervisorChangeRequestServiceContracts,global::HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations{
    
    public static string TypeName(bool assign)=>assign?"Supervisor Assignment Request":"Supervisor Removal Request";
    private WorkflowRequestProcessing Processing=>new(resolver,contexts);
    private static WorkflowRequestSubject Subject(SupervisorChangeRequest r)=>new(r.Request,r.JobId,r.IndividualId,r.OrganisationId);
    private async Task<(int Org,int Person,int User,bool Manage)> ScopeAsync(int? selected=null)
    {
        var actor=await access.RequireContextAsync();var current=await access.GetCurrentOrganisationIdAsync();var org=selected??current;
        if(org<=0||!actor.IsSuperAdministrator&&(org!=current||!await access.HasOrganisationAccessAsync(org)))throw new UnauthorizedAccessException("Use your current organisation.");
        return(org,actor.IndividualId,actor.UserId,actor.IsSuperAdministrator||await access.HasPermissionAsync("Admin")||await access.HasPermissionAsync("Organisation.Edit")||await access.HasPermissionAsync("UserOrganisations.Edit"));
    }
    private static async Task<string> Version(HrmTeContext db,SupervisorChangeRequest r)
    {
        object rows=r.TargetKind=="Department"?
            await db.OrganisationStructureSupervisors.AsNoTracking().Where(s=>s.OrganisationStructureId==r.TargetId&&s.SupervisorStaffId==r.IndividualId).OrderBy(s=>s.OrganisationStructureSupervisorId).Select(s=>new{s.OrganisationStructureSupervisorId,s.IsActive,s.IsValid,s.OperationLogId}).ToListAsync():
            await db.TeamStaffs.AsNoTracking().Where(s=>s.TeamId==r.TargetId&&s.StaffId==r.IndividualId).OrderBy(s=>s.TeamStaffsId).Select(s=>new{s.TeamStaffsId,s.IsSuperVisor,s.IsValid,s.StartDate,s.EndDate,s.OperationLogId}).ToListAsync();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows))));
    }
    private async Task Validate(HrmTeContext db,SupervisorChangeRequest r,bool apply)
    {
        if(await Version(db,r)!=r.OriginalVersion)throw new InvalidOperationException("The supervisor assignment changed. Cancel this request and submit the current change again.");
        if(!await db.Jobs.AnyAsync(j=>j.JobId==r.JobId&&j.IndividualID==r.IndividualId&&j.OrganisationID==r.OrganisationId))throw new InvalidOperationException("The subject employment is no longer available.");
        var result=await OrganisationSettingsService.ValidateSupervisorChangeAsync(db,logs,r.OrganisationId,r.TargetKind,r.TargetId,r.IndividualId,r.IsAssignment,apply);
        if(!result.Success)throw new InvalidOperationException(result.Message);
        if(apply){r.AppliedAtUtc=DateTime.UtcNow;r.IsOpen=false;}
    }
    public async Task<ServiceResult> SubmitAsync(int organisationId,string kind,int target,int person,bool assign,string reason)
    {
        var actor=await ScopeAsync(organisationId);
        var permission=kind=="Team"?"Organisation.Teams.Edit":assign?"Organisation.Structure.Supervisor.Add":"Organisation.Structure.Supervisor.Edit";
        if(!actor.Manage&&!await access.HasPermissionAsync(permission))throw new UnauthorizedAccessException("Supervisor management permission is required.");
        if(kind is not ("Department" or "Team")||target<=0||person<=0||string.IsNullOrWhiteSpace(reason)||reason.Trim().Length>500)return ServiceResult.Fail("Select a department/team and staff member, and enter a reason of up to 500 characters.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var today=DateTime.UtcNow.AddHours(5).Date;
            var job=await db.Jobs.Where(j=>j.OrganisationID==actor.Org&&j.IndividualID==person&&(!assign||j.JobStateId==4&&j.JoinedDate<=today&&(!j.TerminatedDate.HasValue||j.TerminatedDate>today))).OrderByDescending(j=>j.JoinedDate).FirstOrDefaultAsync()??throw new InvalidOperationException("A matching employment record is required for the approval flow.");
            if(await db.SupervisorChangeRequests.AnyAsync(r=>r.OrganisationId==actor.Org&&r.TargetKind==kind&&r.TargetId==target&&r.IndividualId==person&&r.IsOpen))throw new InvalidOperationException("This supervisor already has a pending change for this department or team.");
            var row=new SupervisorChangeRequest{OrganisationId=actor.Org,IndividualId=person,JobId=job.JobId,TargetKind=kind,TargetId=target,IsAssignment=assign,Reason=reason.Trim(),CreatedByUserId=actor.User,CreatedAtUtc=DateTime.UtcNow};
            row.OriginalVersion=await Version(db,row);await Validate(db,row,false);
            row.TargetName=kind=="Team"?await db.Teams.Where(t=>t.TeamId==target).Select(t=>t.Name).SingleAsync():await db.OrganisationStructures.Where(t=>t.OrganisationStructureId==target).Select(t=>t.Name).SingleAsync();
            var type=await db.RequestTypes.SingleOrDefaultAsync(t=>t.TypeName==TypeName(assign))??throw new InvalidOperationException("Supervisor requests are not available yet. Please contact HR.");
            var audit=await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,$"Supervisor {(assign?"assignment":"removal")} request: organisation {actor.Org}, {kind} {target}, individual {person}.");
            row.Request=await requests.CreateAsync(db,new(){RequestTypeId=type.RequestTypeId,ApplicantBusinessEntityId=person,OrganisationBusinessEntityId=actor.Org,EffectiveDate=today,OperationLogId=audit.OperationLogId});row.RequestId=row.Request.RequestId;
            db.SupervisorChangeRequests.Add(row);await db.SaveChangesAsync();await workflow.StartAsync(db,row.RequestId,row.JobId);await tx.CommitAsync();return ServiceResult.Ok("Supervisor change submitted for approval. The current assignment remains in place until approval.");
        }
        catch(InvalidOperationException ex){await tx.RollbackAsync();if(ex is WorkflowConfigurationException configuration)await configuration.NotifyAsync(factory);return ServiceResult.Fail(ex.Message);}
        catch(DbUpdateException){await tx.RollbackAsync();return ServiceResult.Fail("The request changed or a pending request already exists. Refresh and try again.");}
    }
    public async Task<List<SupervisorRequestRow>> LoadAsync(int? organisationId=null,bool pendingOnly=false)
    {
        var a=await ScopeAsync(organisationId);await using var db=await factory.CreateDbContextAsync();
        var rows=await db.SupervisorChangeRequests.AsNoTracking().Include(r=>r.Request).ThenInclude(r=>r.RequestState)
            .Where(r=>r.OrganisationId==a.Org&&(!pendingOnly||r.IsOpen)&&(a.Manage||r.CreatedByUserId==a.User||r.IndividualId==a.Person||db.WfRequestApprovals.Any(p=>p.ApproverUserId==a.User&&p.RequestStageInstance.WorkflowRequestInstance.RequestId==r.RequestId)))
            .OrderByDescending(r=>r.CreatedAtUtc).Take(100).ToListAsync();
        var result=new List<SupervisorRequestRow>();foreach(var r in rows){var i=await WorkflowRequestProcessing.Instance(db,r.RequestId);var name=await db.Individuals.Where(p=>p.BusinessEntityId==r.IndividualId).Select(p=>p.FirstNameEnglish+" "+p.LastNameEnglish).SingleAsync();
            result.Add(new(r.RequestId,r.Request.RequestStateId,r.Request.ReferenceNumber??$"#{r.RequestId}",name,r.TargetKind+" · "+r.TargetName,r.IsAssignment?"Add supervisor":"Remove supervisor",r.Reason,(r.Request.RequestState.IsFinalState?"":"Processing · ")+r.Request.RequestState.StateName,i?.CurrentStage?.WorkflowStage.StageName??"",r.IsOpen&&await Processing.CanApprove(db,Subject(r),i,a.User,a.Person),r.IsOpen&&(r.CreatedByUserId==a.User||r.IndividualId==a.Person)&&i?.WorkflowStatus=="InProgress"));}
        return result;
    }
    public async Task<ServiceResult> ActAsync(int id,int expectedState,string action,string? comment,int? organisationId=null)
    {
        var actor=await ScopeAsync(organisationId);await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var row=await db.SupervisorChangeRequests.Include(r=>r.Request).ThenInclude(r=>r.RequestState).SingleOrDefaultAsync(r=>r.RequestId==id&&r.OrganisationId==actor.Org)??throw new UnauthorizedAccessException("Request unavailable in this organisation.");
            if(!row.IsOpen)throw new InvalidOperationException("This request is already closed.");
            var person=action=="Cancel"&&row.CreatedByUserId==actor.User?row.IndividualId:actor.Person;
            await Processing.ExecuteAsync(db,Subject(row),expectedState,action,comment,actor.User,person,()=>Validate(db,row,false),()=>Validate(db,row,true));
            if(await db.RequestStates.AnyAsync(s=>s.RequestStateId==row.Request.RequestStateId&&s.IsFinalState))row.IsOpen=false;
            if(row.CreatedByUserId!=actor.User && await db.Users.AnyAsync(u=>u.UserId==row.CreatedByUserId&&u.BusinessEntityID!=row.IndividualId&&u.IsActive&&u.LocalUserStateId==1&&u.UserOrganisation!.BusinessEntityID==row.OrganisationId))
                db.RequestNotifications.Add(new(){RequestId=row.RequestId,UserId=row.CreatedByUserId,NotificationKind=expectedState==row.Request.RequestStateId?"RequestAction":"StateChanged",FromStateId=expectedState,ToStateId=row.Request.RequestStateId,ReferenceNumber=row.Request.ReferenceNumber??$"#{row.RequestId}",CreatedAtUtc=DateTime.UtcNow});
            await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Supervisor request action saved.");
        }
        catch(InvalidOperationException ex){await tx.RollbackAsync();if(ex is WorkflowConfigurationException configuration)await configuration.NotifyAsync(factory);
            if(ex is RequestTransitionConfigurationException transition){var row=await db.SupervisorChangeRequests.AsNoTracking().SingleAsync(r=>r.RequestId==id&&r.OrganisationId==actor.Org);await new WorkflowConfigurationException(await contexts.ResolveAsync(db,id,row.JobId),transition.Detail){ConfigurationUrl="/admin/settings/config/state-transitions"}.NotifyAsync(factory);}return ServiceResult.Fail(ex.Message);}
    }
}
