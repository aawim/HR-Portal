using System.Data;
using System.Text.Json;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed partial class StaffPositionManagementService(IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access, IOperationLogService logs, IRequestService requests,
    HRM.Services.Interfaces.AppovalWF.IWorkflowEngine workflow,
    HRM.Services.Interfaces.AppovalWF.IWorkflowApproverResolver resolver,
    HRM.Services.Interfaces.AppovalWF.IWorkflowRequestContextResolver contextResolver)
{
    public sealed record Choice(int Id, string Name);
    public sealed record Assignment(int Id, int JobId, int PositionId, string Name, DateTime From,
        DateTime? To, int StateId, string State, int AuditId)
    {
        public string Version => $"{PositionId}:{From.Ticks}:{To?.Ticks}:{StateId}:{AuditId}";
        public string? PendingAction { get; init; }
        public bool HasApprovedRemoval { get; init; }
    }
    public sealed record Data(bool CanManage, List<Choice> Jobs, List<Choice> Positions, List<Assignment> Assignments);

    private async Task<(int Org, bool Manage)> ScopeAsync()
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("Select an organisation you can access.");
        var manage = actor.IsSuperAdministrator || await access.HasPermissionAsync("Staff.Positions.Manage") ||
            await access.HasPermissionAsync("Admin");
        if (!manage && !actor.IsHRStaff && !actor.IsHRHead && !actor.IsHRManager && !actor.IsOrganisationAdmin &&
            !await access.HasPermissionAsync("Position.Details"))
            throw new UnauthorizedAccessException("You do not have permission to view staff positions.");
        return (org, manage);
    }

    private static IQueryable<Position> Available(HrmTeContext db, int org) => db.Positions.Where(p =>
        p.IsActive != false && (p.OrganisationId == org || p.IsGlobal || p.OrganisationId == null &&
            db.JobPositions.Any(a => a.PositionId == p.PositionId && a.Job.OrganisationID == org)));

    public async Task<Data> LoadAsync(int staffId)
    {
        var (org, manage) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Jobs.AnyAsync(j => j.IndividualID == staffId && j.OrganisationID == org))
            throw new UnauthorizedAccessException("Staff not found in the current organisation.");
        var today = DateTime.UtcNow.AddHours(5).Date;
        var jobs = await db.Jobs.AsNoTracking().Where(j => j.IndividualID == staffId && j.OrganisationID == org &&
            j.JobStateId == 4 && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value > today))
            .OrderByDescending(j => j.JoinedDate).Select(j => new Choice(j.JobId,
                "Job " + j.JobId + " · " + j.OrganisationStructure.Name + " · " + j.JobType.TypeName)).ToListAsync();
        var positions = await Available(db, org).AsNoTracking().OrderBy(p => p.Name)
            .Select(p => new Choice(p.PositionId, p.Name)).ToListAsync();
        var assignments = await db.JobPositions.AsNoTracking().Where(a => a.Job.IndividualID == staffId && a.Job.OrganisationID == org)
            .OrderByDescending(a => a.FromDate).ThenByDescending(a => a.JobPositionId)
            .Select(a => new Assignment(a.JobPositionId, a.JobId, a.PositionId, a.Position.Name,
                a.FromDate, a.ToDate, a.JobPositionStateId, a.JobPositionState.StateName, a.OperationLogId)).ToListAsync();
        var changes = await db.StaffPositionChangeRequests.AsNoTracking()
            .Where(r => r.IndividualId == staffId && r.OrganisationId == org && r.OriginalJson != null &&
                (!r.Request.RequestState.IsFinalState || r.ChangeKind == "Remove" && r.Request.RequestStateId == 4))
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new { r.OriginalJson, r.ChangeKind, r.ToDate, r.Request.RequestState.IsFinalState }).ToListAsync();
        assignments = assignments.Select(assignment =>
        {
            var related = changes.Where(r => JsonSerializer.Deserialize<Assignment>(r.OriginalJson!)?.Id == assignment.Id).ToList();
            return assignment with
            {
                PendingAction = related.FirstOrDefault(r => !r.IsFinalState)?.ChangeKind,
                HasApprovedRemoval = related.Any(r => r.IsFinalState && r.ChangeKind == "Remove" && r.ToDate == assignment.To)
            };
        }).ToList();
        return new(manage, jobs, positions, assignments);
    }

    // Ending retains the original assignment and its linked requests/salary records.
    // Changing creates a successor instead of rewriting historical position names.
    public async Task SaveAsync(int staffId, int jobId, int positionId, DateTime from, DateTime? to,
        string reason, Assignment? original = null, bool change = false, bool remove = false)
    {
        var (org, manage) = await ScopeAsync();
        if (!manage) throw new UnauthorizedAccessException("You do not have permission to manage staff positions.");
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (remove && (original == null || !to.HasValue || change))
                throw new InvalidOperationException("Select the position and its last day.");
            await ValidateAndApplyAsync(db, org, staffId, jobId, positionId, from, to, reason, original, change, false);
            if (await db.StaffPositionChangeRequests.AnyAsync(r => r.JobId == jobId && r.OrganisationId == org && !r.Request.RequestState.IsFinalState))
                throw new InvalidOperationException("This employment already has a pending position request. Complete or cancel it before submitting another.");
            var kind = remove ? "Remove" : change ? "Change" : original == null ? "Assign" : "Dates";
            var type = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == RequestTypeName(kind))
                ?? throw new InvalidOperationException("Position requests are not available yet. Please contact HR.");
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.JOB_POSITION_UPDATE,
                $"Submit {kind} position request: staff {staffId}, job {jobId}, organisation {org}, user {actor.UserId}, reason {reason.Trim()}.");
            var request = await requests.CreateAsync(db, new HRM.DTOs.CreateRequestDto { RequestTypeId = type.RequestTypeId,
                ApplicantBusinessEntityId = staffId, OrganisationBusinessEntityId = org, EffectiveDate = remove ? to!.Value.Date : from.Date,
                OperationLogId = audit.OperationLogId });
            db.StaffPositionChangeRequests.Add(new() { RequestId = request.RequestId, Request = request, JobId = jobId, IndividualId = staffId, OrganisationId = org,
                ChangeKind = kind, PositionId = positionId, FromDate = from.Date, ToDate = to?.Date, Reason = reason.Trim(),
                OriginalJson = original == null ? null : JsonSerializer.Serialize(original), CreatedByUserId = actor.UserId, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            await workflow.StartAsync(db, request.RequestId, jobId);
            await tx.CommitAsync();
        }
        catch (WorkflowConfigurationException ex)
        {
            await tx.RollbackAsync(); await ex.NotifyAsync(factory); throw;
        }
    }

    private async Task ValidateAndApplyAsync(HrmTeContext db, int org, int staffId, int jobId, int positionId,
        DateTime from, DateTime? to, string reason, Assignment? original, bool change, bool apply)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new InvalidOperationException("Enter a reason of up to 500 characters.");
        from = from.Date; to = to?.Date;
        if (from.Year < 1900 || to.HasValue && to.Value < from)
            throw new InvalidOperationException("The end date must be on or after the start date.");
        var job = await db.Jobs.SingleOrDefaultAsync(j => j.JobId == jobId && j.IndividualID == staffId && j.OrganisationID == org);
        if (job == null || job.JobStateId != 4)
            throw new InvalidOperationException("Select an approved employment record in the current organisation.");
        if (from < job.JoinedDate.Date || job.TerminatedDate.HasValue && (!to.HasValue || to.Value >= job.TerminatedDate.Value.Date))
            throw new InvalidOperationException("The position dates must fall within the employment period.");
        HRM.Models.JobPosition? existing = null;
        if (original != null)
        {
            existing = await db.JobPositions.SingleOrDefaultAsync(a => a.JobPositionId == original.Id && a.JobId == jobId);
            if (existing == null || new Assignment(existing.JobPositionId, existing.JobId, existing.PositionId, "",
                existing.FromDate, existing.ToDate, existing.JobPositionStateId, "", existing.OperationLogId).Version != original.Version)
                throw new InvalidOperationException("This position changed. Refresh before trying again.");
            if (existing.JobPositionStateId != 4)
                throw new InvalidOperationException("Only approved positions can be managed here. Process pending requests through their approval flow.");
            if (existing.ToDate.HasValue && existing.ToDate.Value.Date <= DateTime.UtcNow.AddHours(5).Date)
                throw new InvalidOperationException("This position has ended or reaches its end date today. No further changes can be made.");
            if (!change && existing.PositionId != positionId)
                throw new InvalidOperationException("Use Change position to assign a different position.");
            if (change && (from <= existing.FromDate.Date || existing.ToDate.HasValue && from > existing.ToDate.Value.Date.AddDays(1)))
                throw new InvalidOperationException("The new position must start after the previous position started and without a gap in its period.");
        }
        if ((existing == null || change) && !await Available(db, org).AnyAsync(p => p.PositionId == positionId))
            throw new InvalidOperationException("Select an active position available to this organisation.");
        if (change && existing == null) throw new InvalidOperationException("Select the original position to change.");
        if (change && existing!.PositionId == positionId)
            throw new InvalidOperationException("Select a different position, or use Edit dates.");
        if (await db.JobPositions.AnyAsync(a => a.JobId == jobId && a.PositionId == positionId && a.JobPositionStateId == 4 &&
            (existing == null || a.JobPositionId != existing.JobPositionId) &&
            (!to.HasValue || a.FromDate <= to.Value) && (!a.ToDate.HasValue || a.ToDate.Value >= from)))
            throw new InvalidOperationException("This position already has an assignment overlapping those dates.");
        if (!apply) return;
        var before = existing == null ? null : new { existing.JobPositionId, existing.PositionId, existing.FromDate, existing.ToDate, existing.OperationLogId };
        var audit = await logs.CreateAsync(db, existing == null ? SharedConfig.OperationLogActionTypes.JOB_POSITION_CREATE :
            SharedConfig.OperationLogActionTypes.JOB_POSITION_UPDATE,
            JsonSerializer.Serialize(new { Action = change ? "Change staff position" : existing == null ? "Assign staff position" : "Update position dates",
                StaffId = staffId, JobId = jobId, Before = before, PositionId = positionId, From = from, To = to, Reason = reason.Trim() }));
        if (existing != null && !change)
        {
            existing.FromDate = from; existing.ToDate = to; existing.OperationLogId = audit.OperationLogId;
        }
        else
        {
            if (existing != null) { existing.ToDate = from.AddDays(-1); existing.OperationLogId = audit.OperationLogId; }
            db.JobPositions.Add(new HRM.Models.JobPosition { JobId = jobId, PositionId = positionId,
                FromDate = from, ToDate = to, JobPositionStateId = 4, OperationLogId = audit.OperationLogId });
        }
        await db.SaveChangesAsync();
    }
}
