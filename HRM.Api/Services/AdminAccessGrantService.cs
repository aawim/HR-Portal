using System.Data;
using System.Text.Json;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class AdminAccessGrantService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService logs, IWorkflowApproverResolver resolver, IUserContextService contexts)
:global::HRM.Services.Contracts.AdminAccessGrantServiceContracts,global::HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations{
    
    
    private async Task<(UserContextDto Actor, int Org)> Scope(int? selected)
    {
        var actor = await access.RequireContextAsync();
        actor.IsSuperAdministrator = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var current = await access.GetCurrentOrganisationIdAsync();
        var org = actor.IsSuperAdministrator && selected.GetValueOrDefault() > 0 ? selected!.Value : current;
        if (actor.IsSuperAdministrator && org <= 0)
        {
            await using var db = await factory.CreateDbContextAsync();
            org = await db.UserOrganisations.OrderBy(o => o.Organisation.OrganisationName).Select(o => o.BusinessEntityID).FirstOrDefaultAsync();
        }
        if (org <= 0 || (!actor.IsSuperAdministrator && (selected.HasValue && selected != current || !await access.HasOrganisationAccessAsync(org))))
            throw new UnauthorizedAccessException("Use your current organisation.");
        return (actor, org);
    }
    private async Task<bool> Manager(UserContextDto actor) => actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin");
    private static int[] Ids(string json) => JsonSerializer.Deserialize<int[]>(json) ?? [];
    private static Task<WfRequestInstance?> Instance(HrmTeContext db, int id) => db.WfRequestInstances
        .Include(x => x.WorkflowDefinition).ThenInclude(x => x.Stages)
        .Include(x => x.CurrentStage).ThenInclude(x => x!.WorkflowStage)
        .Include(x => x.CurrentStage).ThenInclude(x => x!.Approvals).SingleOrDefaultAsync(x => x.RequestId == id);

    private async Task<WorkflowRequestContextDto> Context(HrmTeContext db, AdminAccessGrantRequest r)
    {
        var job = await db.Jobs.SingleAsync(x => x.JobId == r.JobId && x.OrganisationID == r.OrganisationId);
        return new WorkflowRequestContextDto { RequestId = r.RequestId, RequestTypeId = r.Request.RequestTypeId,
            RequestStateId = r.Request.RequestStateId, ApplicantBusinessEntityId = job.IndividualID,
            SubjectJobId = job.JobId, JobTypeId = job.JobTypeId, OrganisationBusinessEntityId = r.OrganisationId,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5)) };
    }
    private async Task<List<WorkflowApproverCandidate>> Candidates(HrmTeContext db, AdminAccessGrantRequest r, int stage)
    {
        var resolved = await resolver.ResolveAsync(stage, await Context(db, r));
        var ids = resolved.Select(x => x.UserId).ToArray();
        var eligible = await db.Users.Where(u => ids.Contains(u.UserId) && u.UserId != r.UserId && u.IsActive &&
            u.LocalUserStateId == 1 && u.AdminPortalAccessEnabled && u.AdminPortalAccessOrganisationId == u.UserOrganisationId &&
            u.UserOrganisation!.BusinessEntityID == r.OrganisationId).Select(u => u.UserId).ToListAsync();
        return resolved.Where(x => eligible.Contains(x.UserId)).DistinctBy(x => x.UserId).ToList();
    }
    private async Task<bool> CanApprove(HrmTeContext db, AdminAccessGrantRequest r, WfRequestStageInstance? stage, int actor)
    {
        if (!r.IsOpen || stage == null || stage.StageStatus != "Pending" || stage.CompletedAt.HasValue || r.Request.RequestStateId != stage.WorkflowStage.RequestStateId) return false;
        var own = stage.Approvals.SingleOrDefault(x => x.ApproverUserId == actor && x.Decision == "Pending");
        if (own == null || stage.WorkflowStage.ApprovalMode == "Sequential" && stage.Approvals.Any(x => x.Decision == "Pending" && x.ApprovalSequence < own.ApprovalSequence)) return false;
        try { return (await Candidates(db, r, stage.WorkflowStageId)).Any(x => x.UserId == actor); }
        catch (InvalidOperationException) { return false; }
    }
    public async Task<AdminAccessGrantData> LoadAsync(int? organisationId = null)
    {
        var (actor, org) = await Scope(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var manage = await Manager(actor);
        var data = new AdminAccessGrantData { IsSuperAdmin = actor.IsSuperAdministrator, CanManage = manage, OrganisationId = org };
        data.Organisations = await db.Organisations.Where(o => actor.IsSuperAdministrator || o.BusinessEntityID == org)
            .OrderBy(o => o.OrganisationName).Select(o => new AdminAccessOption(o.BusinessEntityID, o.OrganisationName)).ToListAsync();
        if (!data.Organisations.Any(o => o.Id == org)) throw new UnauthorizedAccessException("Organisation not found.");
        var userOrg = await db.UserOrganisations.Where(o => o.BusinessEntityID == org).Select(o => o.UserOrganisationID).SingleOrDefaultAsync();
        data.OrganisationPermissionIds = await db.UserOrganisationRoles.Where(p => p.UserOrganisationID == userOrg && p.IsActive).Select(p => p.RoleID).ToListAsync();
        data.Groups = await db.UserGroups.Where(g => g.UserOrganisationId == userOrg).OrderBy(g => g.GroupName)
            .Select(g => new AdminAccessOption(g.UserGroupId, g.GroupName)
            {
                PermissionIds = g.UserGroupRoles.Where(p => p.IsActive).Select(p => p.RoleId).ToList()
            }).ToListAsync();
        data.Permissions = await db.Roles.OrderBy(r => r.RoleKey).Select(r => new AdminAccessOption(r.RoleID, r.RoleKey + " · " + r.Name)).ToListAsync();
        if (manage)
            data.Accounts = await db.Users.Where(u => u.UserOrganisationId == userOrg).OrderBy(u => u.FullName)
                .Select(u => new AdminAccessAccount { Id = u.UserId, IndividualId = u.BusinessEntityID, Name = u.FullName ?? u.Username,
                    Enabled = u.AdminPortalAccessEnabled && u.AdminPortalAccessOrganisationId == u.UserOrganisationId,
                    GroupIds = u.UserAssignedUserGroups.Where(g => g.UserGroup.UserOrganisationId == userOrg).Select(g => g.UserGroupId).ToList(),
                    PermissionIds = u.UserRoles.Where(r => r.IsActive && r.UserOrganisationId == userOrg).Select(r => r.RoleId).ToList() }).ToListAsync();
        var requests = await db.AdminAccessGrantRequests.Include(r => r.Request).ThenInclude(r => r.RequestState)
            .Where(r => r.OrganisationId == org && (manage || r.UserId == actor.UserId || r.CreatedByUserId == actor.UserId ||
                db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.UserId && a.RequestStageInstance.WorkflowRequestInstance.RequestId == r.RequestId)))
            .OrderByDescending(r => r.CreatedAtUtc).Take(100).ToListAsync();
        foreach (var r in requests)
        {
            var instance = await Instance(db, r.RequestId);
            data.Requests.Add(new AdminAccessGrantRow { Id = r.RequestId, StateId = r.Request.RequestStateId,
                Reference = r.Request.ReferenceNumber ?? $"AAG-{r.RequestId}", Reason = r.Reason,
                Name = await db.Users.Where(u => u.UserId == r.UserId).Select(u => u.FullName ?? u.Username).SingleAsync(),
                Status = !r.IsOpen && !r.Request.RequestState.IsFinalState ? "Closed · Admin access revoked" : (r.Request.RequestState.IsFinalState ? "" : "Processing · ") + r.Request.RequestState.StateName.Trim(),
                Stage = instance?.CurrentStage?.WorkflowStage.StageName ?? "", CanCancel = r.IsOpen && r.CreatedByUserId == actor.UserId,
                CanApprove = await CanApprove(db, r, instance?.CurrentStage, actor.UserId),
                Assignments = "User Groups: " + string.Join(", ", data.Groups.Where(g => Ids(r.GroupIdsJson).Contains(g.Id)).Select(g => g.Name)) +
                    "; Direct permissions: " + string.Join(", ", data.Permissions.Where(p => Ids(r.PermissionIdsJson).Contains(p.Id)).Select(p => p.Name)),
                History = instance == null ? [] : await db.WfRequestActions.Where(a => a.WorkflowRequestInstanceId == instance.WorkflowRequestInstanceId)
                    .OrderBy(a => a.PerformedAt).Select(a => a.ActionCode + ": " + (a.Comments ?? "")).ToListAsync() });
        }
        return data;
    }
    private static async Task<User> Target(HrmTeContext db, int userId, int org) => await db.Users
        .Include(u => u.UserRoles).Include(u => u.UserAssignedUserGroups).ThenInclude(g => g.UserGroup)
        .SingleOrDefaultAsync(u => u.UserId == userId && u.UserOrganisation!.BusinessEntityID == org)
        ?? throw new UnauthorizedAccessException("Account is not in this organisation.");
    private static async Task ValidateAssignments(HrmTeContext db, User target, int[] groups, int[] permissions)
    {
        var allowed = await db.UserOrganisationRoles.Where(p => p.UserOrganisationID == target.UserOrganisationId && p.IsActive).Select(p => p.RoleID).ToListAsync();
        if (permissions.Except(allowed).Any()) throw new InvalidOperationException("Some direct permissions are not enabled for this organisation. Ask Super Admin to enable them under Organisation Permissions.");
        if (!target.IsActive || target.LocalUserStateId != 1) throw new InvalidOperationException("The target account must be active.");
        if (await db.UserGroups.CountAsync(g => groups.Contains(g.UserGroupId) && g.UserOrganisationId == target.UserOrganisationId) != groups.Length ||
            await db.Roles.CountAsync(p => permissions.Contains(p.RoleID)) != permissions.Length)
            throw new InvalidOperationException("Select valid User Groups from this organisation and valid permissions.");
        var hasAdmin = await db.Roles.AnyAsync(p => p.ContextId == 1 && permissions.Contains(p.RoleID) && allowed.Contains(p.RoleID)) ||
            await db.UserGroupRoles.AnyAsync(g => groups.Contains(g.UserGroupId) && g.IsActive && g.Role.ContextId == 1 && allowed.Contains(g.RoleId));
        if (!hasAdmin) throw new InvalidOperationException("Select at least one Admin-context permission or User Group containing one.");
    }
    private static async Task ValidateSuperGrant(HrmTeContext db, int[] groups, int[] permissions, bool super)
    {
        if (!super && (await db.Roles.AnyAsync(p => permissions.Contains(p.RoleID) && p.RoleKey == "Super.Admin.Manage") ||
            await db.UserGroupRoles.AnyAsync(g => groups.Contains(g.UserGroupId) && g.IsActive && g.Role.RoleKey == "Super.Admin.Manage")))
            throw new UnauthorizedAccessException("Only Super Admin can assign Super Admin permissions.");
    }
    public async Task<ServiceResult> SubmitAsync(int organisationId, int userId, IEnumerable<int> groupIds, IEnumerable<int> permissionIds, string reason)
    {
        var (actor, org) = await Scope(organisationId);
        if (!await Manager(actor)) throw new UnauthorizedAccessException("Only an administrator can submit an Admin access grant.");
        if (userId == actor.UserId) return ServiceResult.Fail("You cannot request changes to your own Admin access.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) return ServiceResult.Fail("Enter a reason of up to 1,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var target = await Target(db, userId, org);
            var groups = groupIds.Distinct().ToArray(); var permissions = permissionIds.Distinct().ToArray();
            await ValidateAssignments(db, target, groups, permissions);
            await ValidateSuperGrant(db, groups, permissions, actor.IsSuperAdministrator);
            if (await db.AdminAccessGrantRequests.AnyAsync(r => r.UserId == userId && r.IsOpen)) throw new InvalidOperationException("This account already has a pending access request.");
            var today = DateTime.UtcNow.AddHours(5).Date;
            var job = await db.Jobs.Where(j => j.IndividualID == target.BusinessEntityID && j.OrganisationID == org && j.JobStateId == 4 &&
                j.JoinedDate <= today && (j.TerminatedDate == null || j.TerminatedDate > today) &&
                db.JobPositions.Any(p => p.JobId == j.JobId && p.JobPositionStateId == 4 && p.FromDate.Date <= today &&
                    (!p.ToDate.HasValue || p.ToDate.Value.Date > today))).OrderByDescending(j => j.JoinedDate).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("The account needs active approved employment to resolve the Job Type approval flow.");
            var type = await db.RequestTypes.SingleOrDefaultAsync(t => t.TypeName == TypeName)
                ?? throw new InvalidOperationException("Apply the Admin access grant database setup.");
            var request = new Request { RequestTypeId = type.RequestTypeId, RequestStateId = 1, ApplicantBusinessEntityId = target.BusinessEntityID,
                OrganisationBusinessEntityID = org, ApplicationDate = today, RequestEffectiveDate = today,
                LastStateChangedByUserId = actor.UserId, LastStateChangedDate = DateTime.UtcNow };
            var r = new AdminAccessGrantRequest { Request = request, UserId = userId, OrganisationId = org, JobId = job.JobId,
                CreatedByUserId = actor.UserId, CreatedAtUtc = DateTime.UtcNow, Reason = reason.Trim(),
                GroupIdsJson = JsonSerializer.Serialize(groups), PermissionIdsJson = JsonSerializer.Serialize(permissions) };
            var date = DateOnly.FromDateTime(today);
            var definition = await db.WfWorkflowDefinitions.Include(d => d.Stages).Include(d => d.WorkflowJobTypes)
                .Where(d => d.IsActive && d.RequestTypeId == type.RequestTypeId && d.LeaveTypeId == null && (d.LeaveRequestKind == null || d.LeaveRequestKind == 0) &&
                    d.OrganisationBusinessEntityId == org && (!d.EffectiveFrom.HasValue || d.EffectiveFrom <= date) && (!d.EffectiveTo.HasValue || d.EffectiveTo >= date) &&
                    (!d.WorkflowJobTypes.Any() || d.WorkflowJobTypes.Any(j => j.IsActive && j.JobTypeId == job.JobTypeId)))
                .OrderByDescending(d => d.WorkflowJobTypes.Any()).ThenByDescending(d => d.VersionNumber).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Configure Admin access grant for this organisation and Job Type under Request Approval Flows.");
            var stages = definition.Stages.Where(s => s.IsActive).OrderBy(s => s.StageOrder).ToList();
            if (stages.Count == 0 || stages.Select(s => s.RequestStateId).Distinct().Count() != stages.Count)
                throw new InvalidOperationException("Configure at least one stage with distinct processing states. Level 0 is reserved.");
            foreach (var stage in stages)
            {
                if (!await db.RequestStates.AnyAsync(s => s.RequestStateId == stage.RequestStateId && s.IsProcessingState && !s.IsFinalState))
                    throw new InvalidOperationException("Approval stages must use processing states.");
                var candidates = await Candidates(db, r, stage.WorkflowStageId);
                if (candidates.Count == 0 || stage.ApprovalMode == "Quorum" && (stage.MinimumApprovals is null or < 1 || stage.MinimumApprovals > candidates.Count))
                    throw new InvalidOperationException($"Configure enough independent approvers with Admin access for {stage.StageName}.");
            }
            var route = new[] { 1 }.Concat(stages.Select(s => s.RequestStateId)).Append(4).ToArray();
            foreach (var (from, to) in route.Zip(route.Skip(1))) await RequestTransitionPolicy.RequireAllowedAsync(db, type.RequestTypeId, from, to);
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE, $"Submitted Admin access grant for User {userId}: {r.Reason}");
            request.OperationLogId = audit.OperationLogId; db.AdminAccessGrantRequests.Add(r); await db.SaveChangesAsync();
            request.ReferenceNumber = $"AAG-{org}-{request.RequestId}";
            var instance = new WfRequestInstance { RequestId = request.RequestId, WorkflowDefinitionId = definition.WorkflowDefinitionId, StartedAt = DateTime.UtcNow };
            db.WfRequestInstances.Add(instance);
            await Open(db, r, instance, stages[0]); request.RequestStateId = stages[0].RequestStateId;
            Audit(db, instance, null, actor.UserId, "Submit", 1, request.RequestStateId, r.Reason);
            Notify(db, r, actor.UserId, 1, request.RequestStateId); Notify(db, r, userId, 1, request.RequestStateId);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok($"{request.ReferenceNumber} submitted. Access and assignments change only after final approval.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The account or request changed. Refresh and try again."); }
    }
    private static void Notify(HrmTeContext db, AdminAccessGrantRequest r, int user, int from, int to, long? stage = null) =>
        db.RequestNotifications.Add(new RequestNotification { RequestId = r.RequestId, UserId = user, FromStateId = from, ToStateId = to,
            NotificationKind = stage.HasValue ? "ActionRequired" : "StateChanged", RequestStageInstanceId = stage,
            ReferenceNumber = r.Request.ReferenceNumber ?? "Admin access grant", CreatedAtUtc = DateTime.UtcNow });
    private async Task Open(HrmTeContext db, AdminAccessGrantRequest r, WfRequestInstance instance, WfWorkflowStage definition)
    {
        var candidates = await Candidates(db, r, definition.WorkflowStageId);
        if (candidates.Count == 0) throw new InvalidOperationException("No eligible independent approvers remain for this stage.");
        var stage = new WfRequestStageInstance { WorkflowRequestInstance = instance, WorkflowStage = definition, StartedAt = DateTime.UtcNow };
        db.WfRequestStageInstances.Add(stage);
        foreach (var candidate in candidates) stage.Approvals.Add(new WfRequestApproval { ApproverUserId = candidate.UserId,
            StageApproverId = candidate.StageApproverId, ApprovalSequence = candidate.SequenceNumber, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(); instance.CurrentRequestStageInstanceId = stage.RequestStageInstanceId;
        foreach (var c in candidates.Where(c => definition.ApprovalMode != "Sequential" || c.SequenceNumber == candidates.Min(x => x.SequenceNumber)))
            Notify(db, r, c.UserId, r.Request.RequestStateId, definition.RequestStateId, stage.RequestStageInstanceId);
    }
    private static void Audit(HrmTeContext db, WfRequestInstance i, WfRequestStageInstance? s, int actor, string action, int from, int to, string? comment) =>
        db.WfRequestActions.Add(new WfRequestAction { WorkflowRequestInstance = i, RequestStageInstance = s, PerformedByUserId = actor,
            ActionCode = action, FromRequestStateId = from, ToRequestStateId = to, Comments = comment, PerformedAt = DateTime.UtcNow });

    public async Task<ServiceResult> ActAsync(int organisationId, int id, int expectedState, string action, string? comment)
    {
        var (actor, org) = await Scope(organisationId);
        if (action is not ("Approve" or "Reject" or "Cancel")) return ServiceResult.Fail("Unsupported action.");
        if (comment?.Length > 2000 || action != "Approve" && string.IsNullOrWhiteSpace(comment)) return ServiceResult.Fail("Enter an action comment of up to 2,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var query = db.Database.IsSqlServer() ? db.AdminAccessGrantRequests.FromSqlInterpolated($"SELECT * FROM dbo.AA_AdminAccessGrantRequests WITH (UPDLOCK,HOLDLOCK) WHERE RequestId={id}") : db.AdminAccessGrantRequests.Where(r => r.RequestId == id);
            var r = await query.Include(r => r.Request).SingleOrDefaultAsync(r => r.OrganisationId == org)
                ?? throw new UnauthorizedAccessException("Request not available in this organisation.");
            if (!r.IsOpen || r.Request.RequestStateId != expectedState) throw new InvalidOperationException("Request changed. Refresh before acting.");
            var instance = await Instance(db, id) ?? throw new InvalidOperationException("Workflow is missing.");
            var stage = instance.CurrentStage ?? throw new InvalidOperationException("Current stage is missing.");
            if (instance.WorkflowStatus != "InProgress" || instance.CompletedAt.HasValue) throw new InvalidOperationException("Workflow is closed.");
            if (action == "Cancel" ? r.CreatedByUserId != actor.UserId : !await CanApprove(db, r, stage, actor.UserId))
                throw new UnauthorizedAccessException("You cannot perform this action. The recipient cannot approve their own access.");
            var from = r.Request.RequestStateId; var to = action == "Reject" ? 6 : action == "Cancel" ? 5 : from;
            if (action == "Approve")
            {
                var own = stage.Approvals.Single(a => a.ApproverUserId == actor.UserId);
                own.Decision = "Approved"; own.DecisionAt = DateTime.UtcNow; own.Comments = comment;
                var count = stage.Approvals.Count(a => a.Decision == "Approved");
                var done = stage.WorkflowStage.ApprovalMode switch { "Any" => count > 0, "All" or "Sequential" => count == stage.Approvals.Count,
                    "Quorum" => count >= (stage.WorkflowStage.MinimumApprovals ?? int.MaxValue), _ => throw new InvalidOperationException("Unsupported approval mode.") };
                if (done)
                {
                    var next = instance.WorkflowDefinition.Stages.Where(s => s.IsActive && s.StageOrder > stage.WorkflowStage.StageOrder).OrderBy(s => s.StageOrder).FirstOrDefault();
                    to = next?.RequestStateId ?? 4;
                    await RequestTransitionPolicy.RequireAllowedAsync(db, r.Request.RequestTypeId, from, to);
                    stage.StageStatus = "Completed"; stage.CompletedAt = DateTime.UtcNow;
                    foreach (var a in stage.Approvals.Where(a => a.Decision == "Pending")) a.Decision = "Skipped";
                    if (next != null) await Open(db, r, instance, next);
                    else
                    {
                        var target = await Target(db, r.UserId, org);
                        var job = await db.Jobs.SingleAsync(j => j.JobId == r.JobId);
                        if (job.IndividualID != target.BusinessEntityID || job.JobStateId != 4 || job.OrganisationID != org ||
                            job.TerminatedDate.HasValue && job.TerminatedDate <= DateTime.UtcNow.AddHours(5).Date)
                            throw new InvalidOperationException("The approved employment changed. Submit a new request.");
                        var positionDay = DateTime.UtcNow.AddHours(5).Date;
                        if (job.JoinedDate > positionDay || !await db.JobPositions.AnyAsync(p => p.JobId == job.JobId &&
                            p.JobPositionStateId == 4 && p.FromDate.Date <= positionDay && (!p.ToDate.HasValue || p.ToDate.Value.Date > positionDay)))
                            throw new InvalidOperationException("This employee no longer has an approved active job position. Admin access cannot be granted.");
                        if (!await db.WfWorkflowDefinitions.AnyAsync(d => d.WorkflowDefinitionId == instance.WorkflowDefinitionId &&
                            (!d.WorkflowJobTypes.Any() || d.WorkflowJobTypes.Any(j => j.IsActive && j.JobTypeId == job.JobTypeId))))
                            throw new InvalidOperationException("The employment Job Type changed. Submit a new request with the applicable approval flow.");
                        var groups = Ids(r.GroupIdsJson); var permissions = Ids(r.PermissionIdsJson);
                        await ValidateAssignments(db, target, groups, permissions);
                        var submitterPermissions = await new EffectivePermissionService(factory).GetEffectivePermissionsAsync(r.CreatedByUserId);
                        await ValidateSuperGrant(db, groups, permissions, submitterPermissions.Any(p => p.RoleKey == "Super.Admin.Manage"));
                        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE, $"Approved Admin access grant {id} for User {target.UserId}");
                        await UserAccessAssignmentWriter.ApplyAsync(db, target, groups, permissions, audit.OperationLogId);
                        target.AdminPortalAccessEnabled = true; target.AdminPortalAccessOrganisationId = target.UserOrganisationId;
                        target.LastUpdatedByUserId = actor.UserId; target.LastUpdatedDate = DateTime.Now; target.OperationLogId = audit.OperationLogId;
                        r.IsOpen = false; instance.WorkflowStatus = "Approved"; instance.ExecutionStatus = "Succeeded"; instance.CompletedAt = DateTime.UtcNow;
                    }
                }
                else if (stage.WorkflowStage.ApprovalMode == "Sequential")
                {
                    var pending = stage.Approvals.Where(a => a.Decision == "Pending").ToList();
                    foreach (var a in pending.Where(a => a.ApprovalSequence == pending.Min(p => p.ApprovalSequence)))
                        Notify(db, r, a.ApproverUserId, from, from, stage.RequestStageInstanceId);
                }
            }
            else
            {
                await RequestTransitionPolicy.RequireAllowedAsync(db, r.Request.RequestTypeId, from, to);
                r.IsOpen = false; stage.StageStatus = action == "Reject" ? "Rejected" : "Cancelled"; stage.CompletedAt = DateTime.UtcNow;
                instance.WorkflowStatus = stage.StageStatus; instance.CompletedAt = DateTime.UtcNow;
                foreach (var a in stage.Approvals.Where(a => a.Decision == "Pending")) { a.Decision = a.ApproverUserId == actor.UserId && action == "Reject" ? "Rejected" : "Skipped"; a.DecisionAt = DateTime.UtcNow; }
            }
            if (await db.RequestStates.AnyAsync(s => s.RequestStateId == to && s.CommentRequired) && string.IsNullOrWhiteSpace(comment))
                throw new InvalidOperationException("Enter a comment for this state change.");
            r.Request.RequestStateId = to; r.Request.LastStateChangedByUserId = actor.UserId; r.Request.LastStateChangedDate = DateTime.UtcNow; r.Request.StateChangeRemarks = comment;
            Audit(db, instance, stage, actor.UserId, action, from, to, comment);
            foreach (var user in new[] { r.CreatedByUserId, r.UserId }.Distinct()) Notify(db, r, user, from, to);
            await db.SaveChangesAsync(); await tx.CommitAsync(); await contexts.InvalidateAsync(r.UserId);
            return ServiceResult.Ok(to == 4 ? "Admin access and assignments granted after final approval." : "Request action saved.");
        }
        catch (InvalidOperationException ex) { await tx.RollbackAsync(); return ServiceResult.Fail(ex.Message); }
        catch (DbUpdateException) { await tx.RollbackAsync(); return ServiceResult.Fail("The request changed. Refresh and try again."); }
    }
    public async Task<ServiceResult> RevokeAsync(int organisationId, int userId, string reason)
    {
        var (actor, org) = await Scope(organisationId);
        if (!await Manager(actor)) throw new UnauthorizedAccessException("Only an administrator can revoke access.");
        if (actor.UserId == userId) return ServiceResult.Fail("You cannot revoke your own access.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) return ServiceResult.Fail("Enter a reason of up to 1,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var target = await Target(db, userId, org);
        var pendingGrants=await db.AdminAccessGrantRequests.Include(r=>r.Request).Where(r=>r.UserId==userId&&r.OrganisationId==org&&r.IsOpen).ToListAsync();
        foreach(var pending in pendingGrants)
        {
            pending.IsOpen=false;
            var instance=await Instance(db,pending.RequestId);
            if(instance!=null)
            {
                instance.WorkflowStatus="AccessRevoked";instance.ExecutionStatus="Revoked";instance.CompletedAt=DateTime.UtcNow;
                if(instance.CurrentStage!=null){instance.CurrentStage.StageStatus="Revoked";instance.CurrentStage.CompletedAt=DateTime.UtcNow;foreach(var approval in instance.CurrentStage.Approvals.Where(a=>a.Decision=="Pending"))approval.Decision="Skipped";}
                db.WfRequestActions.Add(new(){WorkflowRequestInstance=instance,PerformedByUserId=actor.UserId,ActionCode="EmergencyRevoke",FromRequestStateId=pending.Request.RequestStateId,ToRequestStateId=pending.Request.RequestStateId,Comments=reason.Trim(),PerformedAt=DateTime.UtcNow});
            }
            db.RequestNotifications.Add(new(){RequestId=pending.RequestId,UserId=target.UserId,NotificationKind="RequestAction",FromStateId=pending.Request.RequestStateId,ToStateId=pending.Request.RequestStateId,ReferenceNumber=pending.Request.ReferenceNumber??"Access request closed",CreatedAtUtc=DateTime.UtcNow});
        }
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USER_ROLE_UPDATE, $"Revoked Admin access for User {userId}: {reason.Trim()}");
        target.AdminPortalAccessEnabled = false; target.AdminPortalAccessOrganisationId = null;
        target.OperationLogId = audit.OperationLogId; target.LastUpdatedByUserId = actor.UserId; target.LastUpdatedDate = DateTime.Now;
        db.StaffNotifications.Add(new StaffNotification { UserId = target.UserId, IndividualId = target.BusinessEntityID,
            OrganisationId = org, Kind = "AdminAccess", Message = "Your Admin portal access has been revoked. Contact your organisation administrator for details.",
            Url = "/online/admin-access", CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(); await tx.CommitAsync(); await contexts.InvalidateAsync(userId);
        return ServiceResult.Ok("Admin access revoked.");
    }
}
