using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class StaffOnboardingService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService logs, IWorkflowApproverResolver approvers, ILogger<StaffOnboardingService>? logger = null)
:global::HRM.Services.Contracts.StaffOnboardingServiceContracts,global::HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations{
    
    

    private async Task<(UserContextDto Actor, int Org)> ScopeAsync()
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("Select an organisation you can access.");
        return (actor, org);
    }

    private static bool IsHr(UserContextDto actor) => actor.IsSuperAdministrator || actor.IsHRStaff || actor.IsHRHead ||
        actor.IsHRManager || actor.IsOrganisationAdmin;

    private async Task<bool> CanSubmitAsync(UserContextDto actor) => IsHr(actor) || await access.HasPermissionAsync("Admin");
    private static string Name(Individual i) => string.Join(" ", new[] { i.FirstNameEnglish, i.MiddleNameEnglish, i.LastNameEnglish }
        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

    public async Task<StaffOnboardingData> LoadAsync(int? individualId = null)
    {
        var (actor, org) = await ScopeAsync();
        await using var db = await factory.CreateDbContextAsync();
        var canSubmit = await CanSubmitAsync(actor);
        var result = new StaffOnboardingData { CanSubmit = canSubmit,
            Organisation = await db.Organisations.Where(x => x.BusinessEntityID == org).Select(x => x.OrganisationName).SingleAsync() };
        if (individualId.HasValue)
        {
            if (!canSubmit) throw new UnauthorizedAccessException("Only HR or an organisation administrator can start onboarding.");
            await ValidatePersonAsync(db, individualId.Value, org, actor.IsSuperAdministrator);
            result.Individual = Name(await db.Individuals.SingleAsync(x => x.BusinessEntityId == individualId));
        }
        if (canSubmit)
        {
            var approvedStructures = db.OrganisationStructures.Where(x => x.OrganisationBusinessEntityID == org &&
                x.OrganisationStructureStateId == 3 && db.OrganisationStructureTypes.Any(t => t.OrganisationStructureTypeId == x.OrganisationStructureTypeId));
            result.StructureTypes = await db.OrganisationStructureTypes.Where(t => approvedStructures.Any(x => x.OrganisationStructureTypeId == t.OrganisationStructureTypeId))
                .OrderBy(t => t.TypeName).Select(t => new OnboardingOption(t.OrganisationStructureTypeId, t.TypeName)).ToListAsync();
            result.Departments = await approvedStructures.OrderBy(x => x.Name)
                .Select(x => new OnboardingDepartmentOption(x.OrganisationStructureId, x.Name, x.OrganisationStructureTypeId)).ToListAsync();
            result.JobTypes = await db.JobTypes.Where(x => x.IsActive != false).OrderBy(x => x.TypeName)
                .Select(x => new OnboardingOption(x.JobTypeId, x.TypeName)).ToListAsync();
            result.Positions = await PositionVacancy.Vacant(db,DateTime.UtcNow.AddHours(5).Date).Where(x => x.IsActive != false && (x.IsGlobal || x.OrganisationId == org))
                .OrderBy(x => x.Name).Select(x => new OnboardingOption(x.PositionId, x.Name)).ToListAsync();
        }
        for(var i=0;i<result.Positions.Count;i++){var p=result.Positions[i]; if(await db.Positions.AnyAsync(x=>x.PositionId==p.Id&&x.PositionMatrixId.HasValue))result.Positions[i]=p with{Pay=await PositionMatrixAssignments.SummaryAsync(db,p.Id)};} return result;
    }

    private static async Task ValidatePersonAsync(HrmTeContext db, int person, int org, bool super)
    {
        if (!await db.Individuals.AnyAsync(x => x.BusinessEntityId == person) ||
            (!super && await db.Jobs.AnyAsync(x => x.IndividualID == person) &&
             !await db.Jobs.AnyAsync(x => x.IndividualID == person && x.OrganisationID == org)))
            throw new UnauthorizedAccessException("This individual is not available in the current organisation’s registry.");
        if (await db.StaffOnboardingRequests.AnyAsync(x => x.IndividualId == person && x.IsOpen))
            throw new InvalidOperationException("An onboarding request is already processing for this individual.");
    }

    private static WorkflowRequestContextDto Context(StaffOnboardingRequest r) => new()
    {
        RequestId = r.RequestId, RequestTypeId = r.Request.RequestTypeId, RequestStateId = r.Request.RequestStateId,
        OrganisationBusinessEntityId = r.OrganisationId, ApplicantBusinessEntityId = r.IndividualId,
        JobTypeId = r.JobTypeId, SubjectOrganisationStructureId = r.OrganisationStructureId,
        EffectiveDate = DateOnly.FromDateTime(r.JoinedDate)
    };

    private async Task<List<WorkflowApproverCandidate>> EligibleAsync(HrmTeContext db, StaffOnboardingRequest r, int stage)
    {
        var candidates = await approvers.ResolveAsync(stage, Context(r));
        var allowed = await db.Users.Where(x => x.IsActive && x.LocalUserStateId == 1 &&
            x.BusinessEntityID != r.IndividualId &&
            x.UserOrganisation!.BusinessEntityID == r.OrganisationId).Select(x => x.UserId).ToListAsync();
        return candidates.Where(x => allowed.Contains(x.UserId)).DistinctBy(x => x.UserId).ToList();
    }

    private async Task ValidateFieldsAsync(HrmTeContext db, StaffOnboardingRequest r)
    {
        if (r.JoinedDate.Year < 1900 || r.JoinedDate.Year > 2200 || r.Remarks.Length > 1000)
            throw new InvalidOperationException("Enter a valid joining date and remarks of up to 1,000 characters.");
        if (!await db.OrganisationStructures.AnyAsync(x => x.OrganisationStructureId == r.OrganisationStructureId &&
            x.OrganisationBusinessEntityID == r.OrganisationId && x.OrganisationStructureStateId == 3 &&
            db.OrganisationStructureTypes.Any(t => t.OrganisationStructureTypeId == x.OrganisationStructureTypeId)))
            throw new InvalidOperationException("Select an approved department or organisation structure belonging to this organisation (Approved state 3).");
        if (!await db.JobTypes.AnyAsync(x => x.JobTypeId == r.JobTypeId && x.IsActive != false))
            throw new InvalidOperationException("Select an active Job Type.");
        if (r.PositionId.HasValue && !await db.Positions.AnyAsync(x => x.PositionId == r.PositionId && x.IsActive != false &&
            (x.IsGlobal || x.OrganisationId == r.OrganisationId)))
            throw new InvalidOperationException("Select an active position available to this organisation, or assign it later.");
        if(r.PositionId.HasValue)await PositionVacancy.RequireAsync(db,r.PositionId.Value,r.JoinedDate,null);
        if(r.PositionId.HasValue){var info=await PositionMatrixAssignments.SummaryAsync(db,r.PositionId.Value,r.JobMatrixStepId);if(info!=null&&(!r.JobMatrixStepId.HasValue||!info.Steps.Any(x=>x.Id==r.JobMatrixStepId)))throw new InvalidOperationException("Select the employee job pay step.");if(info==null&&r.JobMatrixStepId.HasValue)throw new InvalidOperationException("This position has no Job Matrix pay step.");}
        if (await db.Jobs.AnyAsync(x => x.IndividualID == r.IndividualId && x.JobStateId == 4 &&
            (!x.TerminatedDate.HasValue || x.TerminatedDate >= r.JoinedDate)))
            throw new InvalidOperationException("This individual already has approved employment overlapping the proposed joining date.");
        var identity = await IdentityAsync(db, r.IndividualId);
        if (await db.Users.AnyAsync(x => x.Username == identity && x.BusinessEntityID != r.IndividualId))
            throw new InvalidOperationException("The National ID is linked to another account. Contact the administrator.");
        if (await db.Users.CountAsync(x => x.BusinessEntityID == r.IndividualId) > 1)
            throw new InvalidOperationException("Multiple accounts are linked to this individual. Resolve them before onboarding.");
    }

    private static async Task<string> IdentityAsync(HrmTeContext db, int individual)
    {
        var ids = await db.Idcards.Where(x => x.BusinessEntityId == individual && x.IdcardNumber != null && x.IdcardNumber != "")
            .Select(x => x.IdcardNumber.Trim()).Distinct().ToListAsync();
        if (ids.Count != 1) throw new InvalidOperationException("A single National ID must be recorded before an eFaas employee account can be created.");
        return ids[0];
    }

    private async Task<WfWorkflowDefinition> DefinitionAsync(HrmTeContext db, StaffOnboardingRequest r)
    {
        var date = DateOnly.FromDateTime(r.JoinedDate);
        var definition = await db.WfWorkflowDefinitions.Include(x => x.Stages).ThenInclude(x => x.Approvers)
            .Include(x => x.WorkflowJobTypes).Where(x => x.IsActive && x.RequestTypeId == r.Request.RequestTypeId &&
                x.LeaveTypeId == null && (x.LeaveRequestKind == null || x.LeaveRequestKind == 0) &&
                (x.OrganisationBusinessEntityId == r.OrganisationId || x.OrganisationBusinessEntityId == null) &&
                (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= date) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date) &&
                (!x.WorkflowJobTypes.Any() || x.WorkflowJobTypes.Any(j => j.JobTypeId == r.JobTypeId && j.IsActive)))
            .OrderByDescending(x => x.OrganisationBusinessEntityId.HasValue).ThenByDescending(x => x.WorkflowJobTypes.Any())
            .ThenByDescending(x => x.VersionNumber).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Configure Job Position Request approval stages for this tenant and Job Type in Settings → Configurations → Request Approval Flows.");
        var stages = definition.Stages.Where(x => x.IsActive).OrderBy(x => x.StageOrder).ToList();
        if (stages.Count == 0) throw new InvalidOperationException("Level 0 is reserved for later. Configure at least one approval stage.");
        if (stages.Select(x => x.RequestStateId).Distinct().Count() != stages.Count)
            throw new InvalidOperationException("Use a distinct processing state for each approval stage.");
        foreach (var stage in stages)
        {
            if (!await db.RequestStates.AnyAsync(x => x.RequestStateId == stage.RequestStateId && x.IsProcessingState && !x.IsFinalState))
                throw new InvalidOperationException("Each approval stage must use a processing state.");
            var candidates = await EligibleAsync(db, r, stage.WorkflowStageId);
            if (candidates.Count == 0 || (stage.ApprovalMode == "Quorum" && (stage.MinimumApprovals is null or < 1 || stage.MinimumApprovals > candidates.Count)))
                throw new InvalidOperationException($"Configure sufficient eligible approvers for {stage.StageName}. The individual cannot approve their own appointment.");
        }
        var route = new[] { 1 }.Concat(stages.Select(x => x.RequestStateId)).Append(4).ToArray();
        foreach (var (from, to) in route.Zip(route.Skip(1)))
            await RequestTransitionPolicy.RequireAllowedAsync(db, r.Request.RequestTypeId, from, to);
        return definition;
    }

    public async Task<ServiceResult> SubmitAsync(StaffOnboardingForm model)
    {
        var (actor, org) = await ScopeAsync();
        if (!await CanSubmitAsync(actor)) return ServiceResult.Fail("Only HR or an organisation administrator can start onboarding.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Serialize submissions and final approval for the same registry identity.
            await LockPersonAsync(db, model.IndividualId);
            await ValidatePersonAsync(db, model.IndividualId, org, actor.IsSuperAdministrator);
            var type = await db.RequestTypes.SingleOrDefaultAsync(x => x.TypeName == RequestTypeName)
                ?? throw new InvalidOperationException("The Job Position Request type is missing. Apply the onboarding setup script.");
            var request = new Request { RequestTypeId = type.RequestTypeId, RequestStateId = 1, ApplicationDate = DateTime.UtcNow.AddHours(5),
                RequestEffectiveDate = model.JoinedDate.Date, ApplicantBusinessEntityId = model.IndividualId,
                OrganisationBusinessEntityID = org, LastStateChangedByUserId = actor.UserId, LastStateChangedDate = DateTime.UtcNow };
            var r = new StaffOnboardingRequest { Request = request, IndividualId = model.IndividualId, OrganisationId = org,
                OrganisationStructureId = model.OrganisationStructureId, JobTypeId = model.JobTypeId, PositionId = model.PositionId, JobMatrixStepId=model.JobMatrixStepId,
                JoinedDate = model.JoinedDate.Date, Remarks = model.Remarks?.Trim() ?? "", CreatedByUserId = actor.UserId, CreatedAtUtc = DateTime.UtcNow };
            await ValidateFieldsAsync(db, r);
            var definition = await DefinitionAsync(db, r);
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.JOB_POSITION_REQUEST_CREATE, $"Submitted staff onboarding for Individual {r.IndividualId} in organisation {org}.");
            db.AllowStaffAppointment(request);
            request.OperationLogId = audit.OperationLogId;
            db.StaffOnboardingRequests.Add(r); await db.SaveChangesAsync();
            request.ReferenceNumber = $"ONB-{org}-{request.RequestId}";
            var instance = new WfRequestInstance { RequestId = request.RequestId, WorkflowDefinitionId = definition.WorkflowDefinitionId,
                StartedAt = DateTime.UtcNow, ExecutionStatus = "NotStarted" };
            db.WfRequestInstances.Add(instance);
            var first = definition.Stages.Where(x => x.IsActive).OrderBy(x => x.StageOrder).First();
            request.RequestStateId = first.RequestStateId;
            await OpenStageAsync(db, r, instance, first);
            Audit(db, instance, null, actor.UserId, "Submit", 1, first.RequestStateId, r.Remarks);
            await NotifyCreatorAsync(db, r, 1, first.RequestStateId);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok($"Onboarding request {request.ReferenceNumber} submitted. No User account will be created until final approval.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or DbUpdateException)
        {
            await tx.RollbackAsync();
            logger?.LogError(ex, "Onboarding submission failed for individual {IndividualId} in organisation {OrganisationId}", model.IndividualId, org);
            return ServiceResult.Fail(DatabaseMessage(ex));
        }
    }

    private static async Task LockPersonAsync(HrmTeContext db, int person)
    {
        if (db.Database.IsSqlServer())
            await db.Individuals.FromSqlInterpolated($"SELECT * FROM dbo.BE_Individuals WITH (UPDLOCK,HOLDLOCK) WHERE BusinessEntityID={person}").LoadAsync();
    }

    private async Task OpenStageAsync(HrmTeContext db, StaffOnboardingRequest r, WfRequestInstance instance, WfWorkflowStage stage)
    {
        var candidates = await EligibleAsync(db, r, stage.WorkflowStageId);
        if (candidates.Count == 0) throw new InvalidOperationException($"No independent approvers are available for {stage.StageName}.");
        var current = new WfRequestStageInstance { WorkflowRequestInstance = instance, WorkflowStage = stage, StartedAt = DateTime.UtcNow };
        db.WfRequestStageInstances.Add(current);
        foreach (var candidate in candidates)
            current.Approvals.Add(new WfRequestApproval { ApproverUserId = candidate.UserId, StageApproverId = candidate.StageApproverId,
                ApprovalSequence = candidate.SequenceNumber, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        instance.CurrentRequestStageInstanceId = current.RequestStageInstanceId;
        foreach (var candidate in candidates.Where(x => stage.ApprovalMode != "Sequential" || x.SequenceNumber == candidates.Min(a => a.SequenceNumber)))
            db.RequestNotifications.Add(new RequestNotification { RequestId = r.RequestId, UserId = candidate.UserId,
                NotificationKind = "ActionRequired", RequestStageInstanceId = current.RequestStageInstanceId,
                FromStateId = r.Request.RequestStateId, ToStateId = stage.RequestStateId,
                ReferenceNumber = r.Request.ReferenceNumber ?? "Onboarding", CreatedAtUtc = DateTime.UtcNow });
    }

    private async Task<bool> CanApproveAsync(HrmTeContext db, StaffOnboardingRequest r, WfRequestStageInstance? stage, int actor)
    {
        if (!r.IsOpen || stage == null || stage.StageStatus != "Pending" || stage.CompletedAt.HasValue ||
            r.Request.RequestStateId != stage.WorkflowStage.RequestStateId) return false;
        var own = stage.Approvals.SingleOrDefault(x => x.ApproverUserId == actor && x.Decision == "Pending");
        if (own == null || (stage.WorkflowStage.ApprovalMode == "Sequential" && stage.Approvals.Any(x => x.Decision == "Pending" && x.ApprovalSequence < own.ApprovalSequence))) return false;
        try { return (await EligibleAsync(db, r, stage.WorkflowStageId)).Any(x => x.UserId == actor); }
        catch (InvalidOperationException) { return false; }
    }

    public async Task<List<StaffOnboardingRow>> GetRequestsAsync(int? requestId = null, bool pendingOnly = false)
    {
        var (actor, org) = await ScopeAsync(); var hr = await CanSubmitAsync(actor);
        await using var db = await factory.CreateDbContextAsync();
        var requests = await db.StaffOnboardingRequests.Include(x => x.Request).ThenInclude(x => x.RequestState).Include(x => x.Individual)
            .Where(x => x.OrganisationId == org && (!requestId.HasValue || x.RequestId == requestId) && (!pendingOnly || x.IsOpen) &&
                (hr || x.CreatedByUserId == actor.UserId || db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.UserId &&
                    a.RequestStageInstance.WorkflowRequestInstance.RequestId == x.RequestId)))
            .OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync();
        var rows = new List<StaffOnboardingRow>();
        foreach (var r in requests)
        {
            var instance = await InstanceAsync(db, r.RequestId);
            var canApprove = await CanApproveAsync(db, r, instance?.CurrentStage, actor.UserId);
            var assignedIds = instance?.CurrentStage?.Approvals.Where(x => x.Decision == "Pending").Select(x => x.ApproverUserId).ToList() ?? [];
            var assignedNames = await db.Users.Where(x => assignedIds.Contains(x.UserId)).Select(x => x.FullName ?? x.Username).ToListAsync();
            rows.Add(new StaffOnboardingRow { RequestId = r.RequestId, Reference = r.Request.ReferenceNumber ?? $"ONB-{r.RequestId}",
                Individual = Name(r.Individual), Department = await db.OrganisationStructures.Where(x => x.OrganisationStructureId == r.OrganisationStructureId).Select(x => x.Name).SingleAsync(),
                JobType = await db.JobTypes.Where(x => x.JobTypeId == r.JobTypeId).Select(x => x.TypeName).SingleAsync(),
                Position = r.PositionId.HasValue ? (await db.Positions.Where(x => x.PositionId == r.PositionId).Select(x => x.Name).SingleAsync())+(r.JobMatrixStepId.HasValue?" · Job pay step "+await db.FrameworkJobMatrixSteps.Where(x=>x.JobMatrixStepId==r.JobMatrixStepId).Select(x=>x.StepNumber).SingleAsync():"") : "Assign later",
                JoinedDate = r.JoinedDate, Remarks = r.Remarks, StateId = r.Request.RequestStateId,
                Status = r.Request.RequestState.IsFinalState ? r.Request.RequestState.StateName.Trim() : $"Processing · {r.Request.RequestState.StateName.Trim()}",
                Stage = instance?.CurrentStage?.WorkflowStage.StageName, IsOpen = r.IsOpen,
                CanApprove = canApprove,
                ApprovalInformation = !r.IsOpen || canApprove ? null : actor.IndividualId == r.IndividualId ? "You cannot approve your own appointment." :
                    assignedNames.Count > 0 ? "Awaiting the assigned approvers: " + string.Join(", ", assignedNames) : "No pending approvers are available. Contact your organisation’s HR administrator.",
                CanCancel = r.IsOpen && r.CreatedByUserId == actor.UserId,
                AccountStatus = r.UserId.HasValue ? "Account linked after approval; existing account restrictions are preserved" : r.IsOpen ? "Account will be created after approval" : "No account created by this request",
                History = instance == null ? [] : await db.WfRequestActions.Where(x => x.WorkflowRequestInstanceId == instance.WorkflowRequestInstanceId)
                    .OrderBy(x => x.PerformedAt).Select(x => x.ActionCode + ": " + (x.Comments ?? "")).ToListAsync() });
        }
        return rows;
    }

    private static Task<WfRequestInstance?> InstanceAsync(HrmTeContext db, int requestId) => db.WfRequestInstances
        .Include(x => x.WorkflowDefinition).ThenInclude(x => x.Stages)
        .Include(x => x.CurrentStage).ThenInclude(x => x!.WorkflowStage)
        .Include(x => x.CurrentStage).ThenInclude(x => x!.Approvals).SingleOrDefaultAsync(x => x.RequestId == requestId);

    public async Task<ServiceResult> ActAsync(int requestId, int expectedState, string action, string? comment)
    {
        var (actor, org) = await ScopeAsync();
        if (action is not ("Approve" or "Reject" or "Cancel")) return ServiceResult.Fail("Select a supported onboarding action.");
        if (comment?.Length > 2000) return ServiceResult.Fail("Use a comment of up to 2,000 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var query = db.Database.IsSqlServer() ? db.StaffOnboardingRequests.FromSqlInterpolated($"SELECT * FROM dbo.SO_StaffOnboardingRequests WITH (UPDLOCK,HOLDLOCK) WHERE RequestId={requestId}") : db.StaffOnboardingRequests.Where(x => x.RequestId == requestId);
            var r = await query.Include(x => x.Request).SingleOrDefaultAsync(x => x.OrganisationId == org)
                ?? throw new UnauthorizedAccessException("This onboarding request is not available in the selected organisation.");
            if (!r.IsOpen || r.Request.RequestStateId != expectedState) throw new InvalidOperationException("The request changed. Refresh before acting.");
            var instance = await InstanceAsync(db, requestId) ?? throw new InvalidOperationException("The approval workflow is missing.");
            var stage = instance.CurrentStage ?? throw new InvalidOperationException("The current approval stage is missing.");
            if (instance.WorkflowStatus != "InProgress" || instance.CompletedAt.HasValue)
                throw new InvalidOperationException("This workflow is already closed.");
            if (action == "Cancel")
            {
                if (r.CreatedByUserId != actor.UserId) throw new UnauthorizedAccessException("Only the submitting HR officer can cancel this request.");
            }
            else if (!await CanApproveAsync(db, r, stage, actor.UserId))
                throw new UnauthorizedAccessException("You are not an eligible independent approver for the current stage.");
            db.AllowStaffAppointment(r.Request);
            var from = r.Request.RequestStateId;
            var to = action == "Cancel" ? 5 : action == "Reject" ? 6 : from;
            if (action is "Reject" or "Cancel" && string.IsNullOrWhiteSpace(comment))
                throw new InvalidOperationException("Enter a reason for rejecting or cancelling this request.");
            if (action == "Approve")
            {
                var own = stage.Approvals.Single(x => x.ApproverUserId == actor.UserId);
                own.Decision = "Approved"; own.DecisionAt = DateTime.UtcNow; own.Comments = comment?.Trim();
                var count = stage.Approvals.Count(x => x.Decision == "Approved");
                var done = stage.WorkflowStage.ApprovalMode switch
                {
                    "Any" => count > 0,
                    "All" or "Sequential" => count == stage.Approvals.Count,
                    "Quorum" => count >= (stage.WorkflowStage.MinimumApprovals ?? int.MaxValue),
                    _ => throw new InvalidOperationException("The stage approval mode is unsupported.")
                };
                if (done)
                {
                    var next = instance.WorkflowDefinition.Stages.Where(x => x.IsActive && x.StageOrder > stage.WorkflowStage.StageOrder).OrderBy(x => x.StageOrder).FirstOrDefault();
                    to = next?.RequestStateId ?? 4;
                    await RequestTransitionPolicy.RequireAllowedAsync(db, r.Request.RequestTypeId, from, to);
                    stage.StageStatus = "Completed"; stage.CompletedAt = DateTime.UtcNow;
                    foreach (var pending in stage.Approvals.Where(x => x.Decision == "Pending")) pending.Decision = "Skipped";
                    if (next != null) await OpenStageAsync(db, r, instance, next);
                    else
                    {
                        await LockPersonAsync(db, r.IndividualId);
                        await ValidateFieldsAsync(db, r);
                        await ActivateAsync(db, r, actor);
                        r.IsOpen = false; instance.WorkflowStatus = "Approved"; instance.ExecutionStatus = "Succeeded"; instance.CompletedAt = DateTime.UtcNow;
                    }
                }
                else if (stage.WorkflowStage.ApprovalMode == "Sequential")
                {
                    var remaining = stage.Approvals.Where(x => x.Decision == "Pending").ToList();
                    foreach (var approval in remaining.Where(x => x.ApprovalSequence == remaining.Min(a => a.ApprovalSequence)))
                        if (!await db.RequestNotifications.AnyAsync(x => x.UserId == approval.ApproverUserId &&
                            x.RequestStageInstanceId == stage.RequestStageInstanceId && x.NotificationKind == "ActionRequired"))
                            db.RequestNotifications.Add(new RequestNotification { UserId = approval.ApproverUserId, RequestId = r.RequestId,
                                RequestStageInstanceId = stage.RequestStageInstanceId, NotificationKind = "ActionRequired", FromStateId = from,
                                ToStateId = from, ReferenceNumber = r.Request.ReferenceNumber ?? "Onboarding", CreatedAtUtc = DateTime.UtcNow });
                }
            }
            else
            {
                await RequestTransitionPolicy.RequireAllowedAsync(db, r.Request.RequestTypeId, from, to);
                r.IsOpen = false; stage.StageStatus = action == "Reject" ? "Rejected" : "Cancelled"; stage.CompletedAt = DateTime.UtcNow;
                foreach (var approval in stage.Approvals.Where(x => x.Decision == "Pending"))
                {
                    approval.Decision = action == "Reject" && approval.ApproverUserId == actor.UserId ? "Rejected" : "Skipped";
                    approval.DecisionAt = DateTime.UtcNow; approval.Comments = comment?.Trim();
                }
                instance.WorkflowStatus = stage.StageStatus; instance.CompletedAt = DateTime.UtcNow;
            }
            if (await db.RequestStates.AnyAsync(x => x.RequestStateId == to && x.CommentRequired) && string.IsNullOrWhiteSpace(comment))
                throw new InvalidOperationException("Enter a comment for this status change.");
            r.Request.RequestStateId = to; r.Request.LastStateChangedByUserId = actor.UserId;
            r.Request.LastStateChangedDate = DateTime.UtcNow; r.Request.StateChangeRemarks = comment?.Trim();
            Audit(db, instance, stage, actor.UserId, action, from, to, comment);
            await NotifyCreatorAsync(db, r, from, to);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok(to == 4 ? "Onboarding approved. Employment is active and the employee account is created or linked. Address and contact details can be completed later." : "Onboarding action saved.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or DbUpdateException)
        {
            await tx.RollbackAsync();
            logger?.LogError(ex, "Onboarding action {Action} failed for request {RequestId} in organisation {OrganisationId}", action, requestId, org);
            return ServiceResult.Fail(DatabaseMessage(ex));
        }
    }

    private async Task ActivateAsync(HrmTeContext db, StaffOnboardingRequest r, UserContextDto actor)
    {
        var individual = await db.Individuals.SingleAsync(x => x.BusinessEntityId == r.IndividualId);
        var username = await IdentityAsync(db, r.IndividualId);
        var userOrg = await db.UserOrganisations.Where(x => x.BusinessEntityID == r.OrganisationId).Select(x => (int?)x.UserOrganisationID).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Configure a User Organisation for this tenant before approving onboarding.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.JOB_CREATE, $"Approved onboarding {r.Request.ReferenceNumber}: employment and employee account for Individual {r.IndividualId}.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.BusinessEntityID == r.IndividualId);
        if (user == null)
        {
            user = new User { BusinessEntityID = r.IndividualId, Username = username, FullName = Name(individual), Email = "",
                UserKey = Guid.NewGuid(), SsouserKey = Guid.NewGuid(), SsouserStateId = 3, LocalUserStateId = 1,
                IsActive = true, IsApproved = true, AdminPortalAccessEnabled = false, UserOrganisationId = userOrg, ApplicationName = "HRM", CreationDate = DateTime.UtcNow,
                CreatedByUserId = actor.UserId, CreatedContextId = 1, RequestId = r.RequestId, OperationLogId = audit.OperationLogId };
            db.Users.Add(user);
        }
        else if (user.Username != username || user.UserOrganisationId != userOrg)
            throw new InvalidOperationException("The existing account identity or home organisation differs. An administrator must resolve the account before onboarding can be approved.");
        if (!await db.Staffs.AnyAsync(x => x.IndividualId == r.IndividualId))
            db.Staffs.Add(new Staff { IndividualId = r.IndividualId, OperationLogId = audit.OperationLogId });
        await db.SaveChangesAsync();
        var job = new Job { IndividualID = r.IndividualId, OrganisationID = r.OrganisationId, OrganisationStructureId = r.OrganisationStructureId,
            JobTypeId = r.JobTypeId, JobStateId = 4, JoinedDate = r.JoinedDate, OperationLogId = audit.OperationLogId };
        db.Jobs.Add(job); await db.SaveChangesAsync();
        if (r.PositionId.HasValue)
        {
            await PositionMatrixAssignments.ValidateAsync(db,r.OrganisationId,r.PositionId.Value,r.JoinedDate,null,selectedStepId:r.JobMatrixStepId);
            var position = new HRM.Models.JobPosition { JobId = job.JobId, PositionId = r.PositionId.Value, FromDate = r.JoinedDate, JobPositionStateId = 4, OperationLogId = audit.OperationLogId };
            db.JobPositions.Add(position); await db.SaveChangesAsync();
            await PositionMatrixAssignments.SnapshotAsync(db,r.OrganisationId,position,audit.OperationLogId,selectedStepId:r.JobMatrixStepId);
            db.JobPositionsRequests.Add(new JobPositionsRequest { JobPositionId = position.JobPositionId, RequestId = r.RequestId, OperationLogId = audit.OperationLogId });
        }
        db.JobRequests.Add(new JobRequest { JobId = job.JobId, RequestId = r.RequestId });
        db.RequestJobs.Add(new RequestJob { JobId = job.JobId, RequestId = r.RequestId, IsValid = true, OperationLogId = audit.OperationLogId });
        r.JobId = job.JobId; r.UserId = user.UserId;
        db.StaffNotifications.Add(new StaffNotification { UserId = user.UserId, IndividualId = r.IndividualId, OrganisationId = r.OrganisationId,
            Kind = "Onboarding", Message = "Your employment appointment has been approved. Please complete your address and contact details in My Profile.",
            Url = "/profile", CreatedAtUtc = DateTime.UtcNow });
    }

    private static void Audit(HrmTeContext db, WfRequestInstance instance, WfRequestStageInstance? stage, int actor, string action, int from, int to, string? comment) =>
        db.WfRequestActions.Add(new WfRequestAction { WorkflowRequestInstance = instance, RequestStageInstance = stage,
            PerformedByUserId = actor, ActionCode = action, FromRequestStateId = from, ToRequestStateId = to,
            Comments = comment?.Trim(), PerformedAt = DateTime.UtcNow });

    private static async Task NotifyCreatorAsync(HrmTeContext db, StaffOnboardingRequest r, int from, int to)
    {
        if (await db.Users.AnyAsync(x => x.UserId == r.CreatedByUserId && x.IsActive && x.LocalUserStateId == 1 && x.UserOrganisation!.BusinessEntityID == r.OrganisationId))
            db.RequestNotifications.Add(new RequestNotification { UserId = r.CreatedByUserId, RequestId = r.RequestId,
                NotificationKind = "StateChanged", FromStateId = from, ToStateId = to,
                ReferenceNumber = r.Request.ReferenceNumber ?? "Onboarding", CreatedAtUtc = DateTime.UtcNow });
    }

    private static string DatabaseMessage(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException) return "Another user changed this request. Refresh before acting.";
        if (ex is not DbUpdateException) return ex.Message;
        var sql = ex.GetBaseException() as Microsoft.Data.SqlClient.SqlException;
        return sql?.Number switch
        {
            2601 or 2627 => "A matching onboarding, employment link or account already exists. Refresh the request or contact HR to check the existing record.",
            547 => "The action conflicts with a configured database status or related record. No changes were saved. Please contact your administrator; the technical details have been logged.",
            _ => "The onboarding action could not be saved. No changes were applied. Please contact your administrator; the technical details have been logged."
        };
    }
}
