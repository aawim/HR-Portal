using HRM.Models;
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
            // 1. Resolve the business context of the request.
            var context = await _contextResolver.ResolveAsync(
                requestId, cancellationToken);

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
            var definition = await _definitionService.ResolveAsync(
                context.RequestTypeId,
                context.OrganisationBusinessEntityId,
                context.JobTypeId,
                context.EffectiveDate,
                cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            // 5. Load the first active stage.
            var stages = await db.WfWorkflowStages
                .AsNoTracking()
                .Where(x =>
                    x.WorkflowDefinitionId ==
                        definition.WorkflowDefinitionId &&
                    x.IsActive)
                .OrderBy(x => x.StageOrder)
                .ToListAsync(cancellationToken);

            if (stages.Count == 0)
                throw new InvalidOperationException(
                    "The workflow has no active stages.");

            var firstStage = stages[0];

            // 6. Resolve approvers before making changes.
            var approvers = await _approverResolver.ResolveAsync(
                firstStage.WorkflowStageId,
                context,
                cancellationToken);

            if (approvers.Count == 0)
                throw new InvalidOperationException(
                    "The first stage has no eligible approvers.");

            if (approvers.Any(x => x.UserId <= 0))
                throw new InvalidOperationException(
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

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var connection = db.Database.GetDbConnection();
            var sqlTransaction = transaction.GetDbTransaction();

            try
            {
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

                // 12. Update existing request state.
                await ExecuteAsync(
                    connection,
                    sqlTransaction,
                    """
                UPDATE dbo.Requests
                SET RequestStateID = @NewStateID,
                    LastStateChangedByUserID = @UserID,
                    LastStateChangedDate = SYSUTCDATETIME()
                WHERE RequestID = @RequestID
                """,
                    cancellationToken,
                    ("@NewStateID", firstStage.RequestStateId),
                    ("@UserID", userId),
                    ("@RequestID", requestId));

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

                await transaction.CommitAsync(cancellationToken);

                return workflowInstanceId;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
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
