using HRM.Models;
using HRM.Models.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace HRM.Services.Interfaces.AppovalWF
{
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IWorkflowRequestContextResolver _contextResolver;
        private readonly IWorkflowDefinitionService _definitionService;
        private readonly IWorkflowApproverResolver _approverResolver;
        private readonly IWorkflowActorProvider _actorProvider;
        private readonly IUserAccessService _userAccess;

        public WorkflowEngine(
            IDbContextFactory<HrmTeContext> dbFactory,
            IWorkflowRequestContextResolver contextResolver,
            IWorkflowDefinitionService definitionService,
            IWorkflowApproverResolver approverResolver,
            IWorkflowActorProvider actorProvider,
            IUserAccessService userAccess)
        {
            _dbFactory = dbFactory;
            _contextResolver = contextResolver;
            _definitionService = definitionService;
            _approverResolver = approverResolver;
            _actorProvider = actorProvider;
            _userAccess = userAccess;
        }


        public async Task<long> StartAsync(
            int requestId,
            int subjectJobId,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

            try
            {
                var instanceId = await StartAsync(
                    db, requestId, subjectJobId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return instanceId;
            }
            catch (HRM.Services.WorkflowConfigurationException ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                await ex.NotifyAsync(_dbFactory);
                throw;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task<long> StartAsync(
            HrmTeContext db,
            int requestId,
            int subjectJobId,
            CancellationToken cancellationToken = default)
        {
            var transaction = db.Database.CurrentTransaction
                ?? throw new InvalidOperationException(
                    "Workflow startup requires an active transaction.");

            // 1. Resolve the business context of the request.
            var context = await _contextResolver.ResolveAsync(
                db, requestId, subjectJobId, cancellationToken);

            // 2. Resolve the authenticated actor.
            var userId = await _actorProvider.GetCurrentUserIdAsync(
                cancellationToken);

            // 3. Verify organisation access.
            if (!await _userAccess.HasOrganisationAccessAsync(
                    context.OrganisationBusinessEntityId))
            {
                throw new UnauthorizedAccessException(
                    "You do not have access to this organisation.");
            }

            // 4. Select workflow using request type,
            // organisation, job type and effective date.
            WfWorkflowDefinition definition;
            var scopedRequestFlow = context.LeaveRequestKind.HasValue || await db.WfWorkflowDefinitions.AnyAsync(x => x.IsActive &&
                x.LeaveTypeId == null && x.LeaveRequestKind == 0 && x.RequestTypeId == context.RequestTypeId &&
                x.OrganisationBusinessEntityId == context.OrganisationBusinessEntityId &&
                x.WorkflowJobTypes.Any(j => j.IsActive && j.JobTypeId == context.JobTypeId), cancellationToken);
            if (scopedRequestFlow)
            {
                definition = await HRM.Services.Leave.LeaveFlowConfigurationService.ResolveAsync(db, context, cancellationToken);
            }
            else
            {
            var approvalRequestTypeId = context.RequestTypeId;
            if (context.FallbackApprovalRequestTypeId.HasValue &&
                !await db.WfWorkflowDefinitions.AsNoTracking().AnyAsync(x =>
                    x.RequestTypeId == context.RequestTypeId && x.IsActive &&
                    (x.OrganisationBusinessEntityId == context.OrganisationBusinessEntityId || x.OrganisationBusinessEntityId == null) &&
                    (!x.WorkflowJobTypes.Any() || x.WorkflowJobTypes.Any(m =>
                        m.IsActive && m.JobTypeId == context.JobTypeId)) &&
                    (x.EffectiveFrom == null || x.EffectiveFrom <= context.EffectiveDate) &&
                    (x.EffectiveTo == null || x.EffectiveTo >= context.EffectiveDate), cancellationToken))
            {
                approvalRequestTypeId = context.FallbackApprovalRequestTypeId.Value;
            }
            try
            {
            definition = await _definitionService.ResolveAsync(
                  approvalRequestTypeId,
                  context.OrganisationBusinessEntityId,
                  context.JobTypeId,
                  context.EffectiveDate,
                  cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                throw new HRM.Services.WorkflowConfigurationException(context, ex.Message);
            }
            }

            // 5. Load the first active stage.
            var stages = definition.Stages
                .Where(x => x.IsActive)
                .OrderBy(x => x.StageOrder)
                .ToList();

            if (stages.Count == 0)
                throw new HRM.Services.WorkflowConfigurationException(context,
                    "The workflow has no active stages.");

            var firstStage = stages[0];
            if (context.RequestStateId != firstStage.RequestStateId)
                await RequestTransitionPolicy.RequireAllowedAsync(db, context.RequestTypeId, context.RequestStateId, firstStage.RequestStateId, cancellationToken);

            // Validate every stage before accepting a leave submission, so it cannot become stranded later.
            if (scopedRequestFlow)
                foreach (var stage in stages)
                {
                    try
                    {
                    if ((await _approverResolver.ResolveAsync(stage.WorkflowStageId, context, cancellationToken)).Count == 0)
                        throw new InvalidOperationException($"No eligible approver for {stage.StageName}. Ask HR to configure the flow and supervisors.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new HRM.Services.WorkflowConfigurationException(context, $"{stage.StageName}: {ex.Message} Check User Group membership and team/department supervisors.");
                    }
                }

            // 6. Resolve approvers before making changes.
            IReadOnlyList<WorkflowApproverCandidate> approvers;
            try
            {
            approvers = await _approverResolver.ResolveAsync(
                firstStage.WorkflowStageId,
                context,
                cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                throw new HRM.Services.WorkflowConfigurationException(context, ex.Message);
            }

            if (approvers.Count == 0)
                throw new HRM.Services.WorkflowConfigurationException(context,
                    "The first stage has no eligible approvers.");

            if (approvers.Any(x => x.UserId <= 0))
                throw new HRM.Services.WorkflowConfigurationException(context,
                    "An approver has an invalid UserID.");

            // A user can qualify through more than one rule.
            // The approvals table should contain one decision
            // assignment per user for this stage.
            var assignments = approvers
                .GroupBy(x => x.UserId)
                .Select(g => g
                    .OrderBy(x => x.SequenceNumber)
                    .First())
                .ToList();

            // 7. Begin atomic workflow initiation.
            await db.Database.OpenConnectionAsync(
                cancellationToken);

            var connection = db.Database.GetDbConnection();
            var sqlTransaction = transaction.GetDbTransaction();

            // Lock the central request.
            var requestStateId = await ScalarAsync<int?>(
                connection,
                sqlTransaction,
                """
            SELECT RequestStateID
            FROM dbo.Requests WITH (UPDLOCK, HOLDLOCK)
            WHERE RequestID = @RequestID
            """,
                cancellationToken,
                ("@RequestID", requestId));

            if (!requestStateId.HasValue)
                throw new InvalidOperationException(
                    "The request does not exist.");

            // A final request or leave must not be restarted as a pending workflow.
            if (requestStateId.Value is HRM.Components.Shared.SharedConfig.RequestStates.APPROVED
                or HRM.Components.Shared.SharedConfig.RequestStates.REJECTED
                or HRM.Components.Shared.SharedConfig.RequestStates.CANCELLED ||
                await db.LeaveRequests.AsNoTracking().AnyAsync(x => x.RequestId == requestId &&
                    (x.Leave.LeaveStateId == HRM.Components.Shared.SharedConfig.LeaveStates.APPROVED ||
                     x.Leave.LeaveStateId == HRM.Components.Shared.SharedConfig.LeaveStates.REJECTED ||
                     x.Leave.LeaveStateId == HRM.Components.Shared.SharedConfig.LeaveStates.CANCELLED), cancellationToken))
                throw new InvalidOperationException("A closed request or leave application cannot start an approval workflow.");
            // Prevent duplicate workflow initiation.
            var existingInstance = await ScalarAsync<long?>(
                connection,
                sqlTransaction,
                """
            SELECT WorkflowRequestInstanceID
            FROM dbo.WF_RequestInstances
                WITH (UPDLOCK, HOLDLOCK)
            WHERE RequestID = @RequestID
            """,
                cancellationToken,
                ("@RequestID", requestId));

            if (existingInstance.HasValue)
                throw new InvalidOperationException(
                    "A workflow has already been started " +
                    "for this request.");

            // Validate the configured request-state transition.
            if (requestStateId.Value != firstStage.RequestStateId)
            {
                var allowed = await ScalarAsync<int?>(
                    connection,
                    sqlTransaction,
                    """
                SELECT TOP (1) 1
                FROM dbo.RequestTypesAllowedRequestStateTransitions
                WHERE RequestTypeID = @RequestTypeID
                  AND FromRequestStateID = @FromStateID
                  AND ToRequestStateID = @ToStateID
                """,
                    cancellationToken,
                    ("@RequestTypeID", context.RequestTypeId),
                    ("@FromStateID", requestStateId.Value),
                    ("@ToStateID", firstStage.RequestStateId));

                if (allowed != 1)
                    throw new InvalidOperationException(
                        "The transition to the first workflow " +
                        "stage is not configured for this request type.");
            }

            // 8. Create workflow request instance.
            var workflowInstanceId = await ScalarAsync<long>(
                connection,
                sqlTransaction,
                """
            INSERT INTO dbo.WF_RequestInstances
            (
                RequestID,
                WorkflowDefinitionID,
                WorkflowStatus,
                ExecutionStatus,
                StartedAt
            )
            OUTPUT INSERTED.WorkflowRequestInstanceID
            VALUES
            (
                @RequestID,
                @WorkflowDefinitionID,
                'InProgress',
                'Pending',
                SYSUTCDATETIME()
            )
            """,
                cancellationToken,
                ("@RequestID", requestId),
                ("@WorkflowDefinitionID",
                    definition.WorkflowDefinitionId));

            // 9. Create first stage instance.
            var stageInstanceId = await ScalarAsync<long>(
                connection,
                sqlTransaction,
                """
            INSERT INTO dbo.WF_RequestStageInstances
            (
                WorkflowRequestInstanceID,
                WorkflowStageID,
                AttemptNumber,
                StageStatus,
                StartedAt
            )
            OUTPUT INSERTED.RequestStageInstanceID
            VALUES
            (
                @WorkflowRequestInstanceID,
                @WorkflowStageID,
                1,
                'Pending',
                SYSUTCDATETIME()
            )
            """,
                cancellationToken,
                ("@WorkflowRequestInstanceID", workflowInstanceId),
                ("@WorkflowStageID", firstStage.WorkflowStageId));

            // 10. Assign approvers.
            foreach (var approver in assignments)
            {
                await ExecuteAsync(
                    connection,
                    sqlTransaction,
                    """
                INSERT INTO dbo.WF_RequestApprovals
                (
                    RequestStageInstanceID,
                    ApproverUserID,
                    StageApproverID,
                    ApprovalSequence,
                    Decision,
                    AssignedAt
                )
                VALUES
                (
                    @StageInstanceID,
                    @ApproverUserID,
                    @StageApproverID,
                    @ApprovalSequence,
                    'Pending',
                    SYSUTCDATETIME()
                )
                """,
                    cancellationToken,
                    ("@StageInstanceID", stageInstanceId),
                    ("@ApproverUserID", approver.UserId),
                    ("@StageApproverID", approver.StageApproverId),
                    ("@ApprovalSequence", approver.SequenceNumber));
            }

            // 11. Set current stage.
            await ExecuteAsync(
                connection,
                sqlTransaction,
                """
            UPDATE dbo.WF_RequestInstances
            SET CurrentRequestStageInstanceID = @StageInstanceID
            WHERE WorkflowRequestInstanceID = @WorkflowInstanceID
            """,
                cancellationToken,
                ("@StageInstanceID", stageInstanceId),
                ("@WorkflowInstanceID", workflowInstanceId));

            // 12. Use the tracked state write so notifications share this transaction.
            var stateRequest = await db.Requests.SingleAsync(x => x.RequestId == requestId, cancellationToken);
            db.Entry(stateRequest).Property(x => x.RequestStateId).OriginalValue = requestStateId.Value;
            stateRequest.RequestStateId = firstStage.RequestStateId;
            stateRequest.LastStateChangedByUserId = userId;
            stateRequest.LastStateChangedDate = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            // 13. Record audit action.
            await ExecuteAsync(
                connection,
                sqlTransaction,
                """
            INSERT INTO dbo.WF_RequestActions
            (
                WorkflowRequestInstanceID,
                RequestStageInstanceID,
                PerformedByUserID,
                ActionCode,
                FromRequestStateID,
                ToRequestStateID,
                Comments,
                PerformedAt
            )
            VALUES
            (
                @WorkflowInstanceID,
                @StageInstanceID,
                @UserID,
                'START',
                @FromStateID,
                @ToStateID,
                'Workflow initiated',
                SYSUTCDATETIME()
            )
            """,
                cancellationToken,
                ("@WorkflowInstanceID", workflowInstanceId),
                ("@StageInstanceID", stageInstanceId),
                ("@UserID", userId),
                ("@FromStateID", requestStateId.Value),
                ("@ToStateID", firstStage.RequestStateId));

            return workflowInstanceId;
        }



        public async Task CancelAsync(
            int requestId,
            string? reason,
            CancellationToken cancellationToken = default)
        {
            if (requestId <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestId));

            // Resolve authenticated user, not a browser-supplied UserID.
            var performedByUserId =
                await _actorProvider.GetCurrentUserIdAsync(
                    cancellationToken);

            if (performedByUserId <= 0)
                throw new UnauthorizedAccessException(
                    "Authenticated user was not found.");

       

            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            // 1. Load the request.
            var request = await db.Requests
                .FirstOrDefaultAsync(
                    x => x.RequestId == requestId,
                    cancellationToken);

            if (request == null)
                throw new InvalidOperationException(
                    $"Request {requestId} was not found.");

            // 2. Match the authenticated user to the request applicant.
            if (!request.ApplicantBusinessEntityId.HasValue)
                throw new InvalidOperationException(
                    "The request has no applicant.");

            await EnsureApplicantCanCancelAsync(
                db,
                request.ApplicantBusinessEntityId.Value,
                performedByUserId,
                cancellationToken);

            const int cancelledStateId = 5;

            if (request.RequestStateId == cancelledStateId)
            {
                // Already cancelled: no additional changes needed.
                return;
            }

            // 3. Check whether the transition is allowed.
            await RequestTransitionPolicy.RequireAllowedAsync(db, request.RequestTypeId, request.RequestStateId, cancelledStateId, cancellationToken);

            // 4. Applicant cancellation policy.
            int[] applicantCancellableStates = { 1, 2, 3, 12 };

            if (!applicantCancellableStates.Contains(request.RequestStateId))
            {
                throw new InvalidOperationException(
                    "The applicant cannot cancel a request in its current state.");
            }

            var previousStateId = request.RequestStateId;
            var now = DateTime.UtcNow;

            // 5. Find the current workflow instance, if any.
            var workflow = await db.WfRequestInstances
                .Where(x => x.RequestId == requestId)
                .OrderByDescending(x => x.WorkflowRequestInstanceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (workflow != null)
            {
                if (workflow.CompletedAt != null)
                {
                    throw new InvalidOperationException(
                        "The existing workflow is already completed. " +
                        "Its status must be reconciled before cancellation.");
                }

                // 6. Close pending approvals.
                var stageIds = await db.WfRequestStageInstances
                    .Where(x =>
                        x.WorkflowRequestInstanceId ==
                            workflow.WorkflowRequestInstanceId)
                    .Select(x => x.RequestStageInstanceId)
                    .ToListAsync(cancellationToken);

                var pendingApprovals = await db.WfRequestApprovals
                    .Where(x =>
                        stageIds.Contains(x.RequestStageInstanceId) &&
                        x.Decision == "Pending")
                    .ToListAsync(cancellationToken);

                foreach (var approval in pendingApprovals)
                {
                    approval.Decision = "Cancelled";
                    approval.DecisionAt = now;
                }

                // 7. Close pending stages.
                var pendingStages = await db.WfRequestStageInstances
                    .Where(x =>
                        x.WorkflowRequestInstanceId ==
                            workflow.WorkflowRequestInstanceId &&
                        x.StageStatus == "Pending")
                    .ToListAsync(cancellationToken);

                foreach (var stage in pendingStages)
                {
                    stage.StageStatus = "Cancelled";
                    stage.CompletedAt = now;
                }

                // 8. Close workflow.
                workflow.WorkflowStatus = "Cancelled";
                workflow.ExecutionStatus = "Completed";
                workflow.CompletedAt = now;

                // 9. Record workflow action.
                db.WfRequestActions.Add(new WfRequestAction
                {
                    WorkflowRequestInstanceId =
                        workflow.WorkflowRequestInstanceId,

                    RequestStageInstanceId =
                        workflow.CurrentRequestStageInstanceId,

                    PerformedByUserId = performedByUserId,

                    ActionCode = "CANCEL",

                    FromRequestStateId = previousStateId,
                    ToRequestStateId = cancelledStateId,

                    Comments = reason,
                    PerformedAt = now
                });
            }

            // 10. Update original request.
            request.RequestStateId = cancelledStateId;
            request.StateChangeRemarks = reason;
            request.LastStateChangedByUserId = performedByUserId;
            request.LastStateChangedDate = now;

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }



        private static async Task EnsureApplicantCanCancelAsync(
         HrmTeContext db,
         int applicantBusinessEntityId,
         int performedByUserId,
         CancellationToken cancellationToken)
        {
            var isApplicant = await db.Users
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == performedByUserId &&
                    x.BusinessEntityID == applicantBusinessEntityId &&
                    x.IsActive,
                    cancellationToken);

            if (!isApplicant)
            {
                throw new UnauthorizedAccessException(
                    "Only the request applicant can cancel this request.");
            }
        }





        private static async Task<T> ScalarAsync<T>(
            DbConnection connection,
            DbTransaction transaction,
            string sql,
            CancellationToken cancellationToken,
            params (string Name, object? Value)[] parameters)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            AddParameters(command, parameters);

            var result = await command.ExecuteScalarAsync(
                cancellationToken);

            if (result == null || result == DBNull.Value)
                return default!;

            return (T)Convert.ChangeType(
                result,
                Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }

        private static async Task ExecuteAsync(
            DbConnection connection,
            DbTransaction transaction,
            string sql,
            CancellationToken cancellationToken,
            params (string Name, object? Value)[] parameters)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            AddParameters(command, parameters);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static void AddParameters(
            DbCommand command,
            params (string Name, object? Value)[] parameters)
        {
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        
    }
}
