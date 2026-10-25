using System.Data;
using System.Text.Json;
using HRM.DTOs;
using HRM.Helpers;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record StaffPositionRequestRow(int Id, int StateId, string Reference, string Staff, string Kind,
    string Position, DateTime From, DateTime? To, string Reason, string Status, string Stage,
    List<string> History, bool CanApprove, bool CanCancel);

public sealed partial class StaffPositionManagementService
{
    public const string RequestsPageUrl = "/online/staff-position-requests";
    public static string RequestTypeName(string kind) => kind switch {
        "Assign" => "Staff Position Assignment Request", "Remove" => "Staff Position Removal Request",
        "Dates" => "Staff Position Dates Request", _ => "Staff Position Change Request" };
    private WorkflowRequestProcessing Processing => new(resolver, contextResolver);
    private static WorkflowRequestSubject Subject(StaffPositionChangeRequest r) => new(r.Request, r.JobId, r.IndividualId, r.OrganisationId);
    private static Assignment? Original(StaffPositionChangeRequest r) => r.OriginalJson == null ? null : JsonSerializer.Deserialize<Assignment>(r.OriginalJson);
    private async Task<(int User, int Person, int Org, bool Manage)> RequestScopeAsync()
    {
        var actor = await access.RequireContextAsync(); var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select your current organisation.");
        var manage = actor.IsSuperAdministrator || await access.HasPermissionAsync("Staff.Positions.Manage") || await access.HasPermissionAsync("Admin");
        return (actor.UserId, actor.IndividualId, org, manage);
    }
    public async Task<List<StaffPositionRequestRow>> LoadRequestsAsync(int? staffId = null, bool pendingOnly = false)
    {
        var actor = await RequestScopeAsync(); await using var db = await factory.CreateDbContextAsync();
        var rows = await db.StaffPositionChangeRequests.AsNoTracking().Include(r=>r.Request).ThenInclude(r=>r.RequestState)
            .Where(r=>r.OrganisationId == actor.Org && (!staffId.HasValue || r.IndividualId == staffId) &&
                (!pendingOnly || !r.Request.RequestState.IsFinalState) &&
                (actor.Manage || r.IndividualId == actor.Person || r.CreatedByUserId == actor.User ||
                 db.WfRequestApprovals.Any(a=>a.ApproverUserId == actor.User && a.RequestStageInstance.WorkflowRequestInstance.RequestId == r.RequestId)))
            .OrderByDescending(r=>r.CreatedAtUtc).Take(100).ToListAsync();
        var result = new List<StaffPositionRequestRow>();
        foreach(var r in rows)
        {
            var instance = await WorkflowRequestProcessing.Instance(db,r.RequestId);
            var name = await db.Individuals.Where(i=>i.BusinessEntityId == r.IndividualId).Select(i=>i.FirstNameEnglish + " " + i.LastNameEnglish).SingleAsync();
            var position = await db.Positions.Where(p=>p.PositionId == r.PositionId).Select(p=>p.Name).SingleAsync();
            var original = Original(r);
            if (r.ChangeKind == "Change" && original != null) position = original.Name + " → " + position;
            var history = instance == null ? new List<string>() : await db.WfRequestActions.Where(a=>a.WorkflowRequestInstanceId == instance.WorkflowRequestInstanceId)
                .OrderBy(a=>a.PerformedAt).Select(a=>a.ActionCode + ": " + (a.Comments ?? "")).ToListAsync();
            result.Add(new(r.RequestId,r.Request.RequestStateId,r.Request.ReferenceNumber ?? $"#{r.RequestId}",name,r.ChangeKind,
                position,r.FromDate,r.ToDate,r.Reason,
                (r.Request.RequestState.IsProcessingState && !r.Request.RequestState.IsFinalState ? "Processing · " : "") + r.Request.RequestState.StateName,
                instance?.CurrentStage?.WorkflowStage.StageName ?? "",history,
                await Processing.CanApprove(db,Subject(r),instance,actor.User,actor.Person),
                (r.IndividualId == actor.Person || r.CreatedByUserId == actor.User) && !r.Request.RequestState.IsFinalState && instance?.WorkflowStatus == "InProgress"));
        }
        return result;
    }
    public async Task<ServiceResult> ActOnRequestAsync(int id, int expectedState, string action, string? comment)
    {
        var actor = await RequestScopeAsync(); await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var r = await db.StaffPositionChangeRequests.Include(r=>r.Request).ThenInclude(r=>r.RequestState)
                .SingleOrDefaultAsync(r=>r.RequestId == id && r.OrganisationId == actor.Org)
                ?? throw new UnauthorizedAccessException("Request unavailable in this organisation.");
            // The submitting HR officer can cancel the request they raised on behalf of staff.
            // Approve/reject always use the actual person's identity and configured stage assignment.
            var person = action == "Cancel" && r.CreatedByUserId == actor.User ? r.IndividualId : actor.Person;
            await Processing.ExecuteAsync(db,Subject(r),expectedState,action,comment,actor.User,person,
                ()=>ValidateAndApplyAsync(db,actor.Org,r.IndividualId,r.JobId,r.PositionId,r.FromDate,r.ToDate,r.Reason,Original(r),r.ChangeKind == "Change",false),
                ()=>ValidateAndApplyAsync(db,actor.Org,r.IndividualId,r.JobId,r.PositionId,r.FromDate,r.ToDate,r.Reason,Original(r),r.ChangeKind == "Change",true));
            await tx.CommitAsync(); return ServiceResult.Ok("Position request action saved.");
        }
        catch(InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            if(ex is WorkflowConfigurationException configuration) await configuration.NotifyAsync(factory);
            if(ex is RequestTransitionConfigurationException transition)
            {
                var r = await db.StaffPositionChangeRequests.AsNoTracking().SingleAsync(r=>r.RequestId == id && r.OrganisationId == actor.Org);
                await new WorkflowConfigurationException(await contextResolver.ResolveAsync(db,id,r.JobId),transition.Detail)
                    { ConfigurationUrl = "/admin/settings/config/state-transitions" }.NotifyAsync(factory);
            }
            return ServiceResult.Fail(ex.Message);
        }
        catch(DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("This request changed. Refresh and try again."); }
    }
}
