using System.Data;
using System.Text.Json;
using HRM.Components.Shared;
using HRM.DTOs.StaffTransfer;
using HRM.Enums;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

/// <summary>
/// Applies an approved internal staff transfer safely and idempotently on or after its effective date.
/// Core HR, Job, Position, Work Planning, and Organisational Access updates run inside a serializable SQL transaction;
/// physical door access is dispatched as durable security provisioning tasks.
/// </summary>
public sealed class StaffTransferActivationService(
    IDbContextFactory<HrmTeContext> factory,
    IOperationLogService logs,
    ILogger<StaffTransferActivationService>? logger = null) : IStaffTransferActivationService
{
    public static DateTime TodayMaldives => DateTime.UtcNow.AddHours(5).Date;

    public async Task<TransferActivationResult> ActivateAsync(
        int staffTransferId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            if (db.Database.IsSqlServer())
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource={"HRM.StaffTransfer.Activate:" + staffTransferId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result<0 THROW 51090,'Transfer activation is already in progress.',1;",
                    cancellationToken);
            }

            var transfer = await db.StaffTransfers
                .Include(t => t.JobDetail)
                .Include(t => t.WorkDetail)
                .Include(t => t.ReportingDetail)
                .Include(t => t.AccessChanges)
                .Include(t => t.DoorAccessChanges)
                .Include(t => t.ActivationLogs)
                .SingleOrDefaultAsync(t => t.StaffTransferId == staffTransferId, cancellationToken)
                ?? throw new InvalidOperationException("Staff transfer record was not found.");

            // Idempotency guard: if already Active, return immediately without duplicating history or assignments.
            if (transfer.Status == StaffTransferStatuses.Active && transfer.ActivatedAt.HasValue)
            {
                await tx.CommitAsync(cancellationToken);
                return new TransferActivationResult
                {
                    StaffTransferId = transfer.StaffTransferId,
                    Succeeded = true,
                    WasAlreadyActive = true,
                    ResultingStatus = StaffTransferStatuses.Active,
                    ResultingJobId = transfer.JobDetail?.ResultingJobId ?? transfer.CurrentJobId,
                    ResultingJobPositionId = transfer.JobDetail?.ResultingJobPositionId,
                    Message = "Transfer was already activated."
                };
            }

            if (transfer.Status is not (StaffTransferStatuses.Approved or StaffTransferStatuses.Scheduled or StaffTransferStatuses.ActivationFailed))
                throw new InvalidOperationException($"Transfer cannot be activated from status '{transfer.Status}'.");

            if (!transfer.FinalApprovedByUserId.HasValue || !transfer.FinalApprovedAt.HasValue)
                throw new InvalidOperationException("Transfer has not received final receiving department approval.");

            if (!transfer.ConfirmedVersion.HasValue || transfer.ConfirmedVersion.Value != transfer.CurrentVersion)
                throw new InvalidOperationException("Employee confirmation does not match the current approved version.");

            var today = TodayMaldives;
            var effectiveDate = transfer.EffectiveDate.Date;
            if (effectiveDate > today)
                throw new InvalidOperationException($"Transfer effective date ({effectiveDate:yyyy-MM-dd}) has not been reached yet.");

            var attempt = (transfer.ActivationLogs.Select(l => (int?)l.ExecutionAttempt).Max() ?? 0) + 1;
            var warnings = new List<string>();
            transfer.Status = StaffTransferStatuses.ActivationInProgress;
            transfer.UpdatedDate = DateTime.UtcNow;

            var jobDetail = transfer.JobDetail
                ?? throw new InvalidOperationException("Transfer job details are missing.");
            var workDetail = transfer.WorkDetail
                ?? throw new InvalidOperationException("Transfer work details are missing.");

            // Step 1: Validation
            var sourceJob = await db.Jobs.SingleOrDefaultAsync(
                j => j.JobId == transfer.CurrentJobId && j.IndividualID == transfer.IndividualId,
                cancellationToken)
                ?? throw new InvalidOperationException("Current employment record was not found.");

            if (sourceJob.JobStateId != SharedConfig.JobStates.APPROVED &&
                jobDetail.ResultingJobId == null)
                throw new InvalidOperationException("Source employment is no longer in Approved state.");

            var destStructureValid = await db.OrganisationStructures.AnyAsync(
                s => s.OrganisationStructureId == transfer.ToOrganisationStructureId &&
                     s.OrganisationBusinessEntityID == transfer.ToOrganisationId &&
                     s.OrganisationStructureStateId == 3,
                cancellationToken);
            if (!destStructureValid)
                throw new InvalidOperationException("Destination department/organisation structure is no longer active and approved.");

            RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.Validation,
                $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:Validation",
                new { effectiveDate, sourceJobId = sourceJob.JobId, transfer.ToOrganisationId, transfer.ToOrganisationStructureId });

            var auditLog = await logs.CreateAsync(
                db,
                SharedConfig.OperationLogActionTypes.JOB_UPDATE,
                $"Activate staff transfer #{transfer.StaffTransferId} for individual {transfer.IndividualId} effective {effectiveDate:yyyy-MM-dd}.");

            // Step 2: Job & Structure Reassignment + Effective-Dated Organisation History
            var targetJobId = jobDetail.ResultingJobId ?? sourceJob.JobId;
            var jobStepKey = $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:JobReassignment";
            if (!HasSucceededStep(transfer, StaffTransferActivationSteps.JobAndStructureReassignment, jobStepKey))
            {
                var priorDay = effectiveDate > sourceJob.JoinedDate.Date ? effectiveDate.AddDays(-1) : effectiveDate;

                // Close or seed previous ST_JobOrganisationHistories interval
                var openOrgHistory = await db.JobOrganisationHistories
                    .Where(h => h.JobId == sourceJob.JobId && h.EffectiveTo == null)
                    .OrderByDescending(h => h.EffectiveFrom)
                    .FirstOrDefaultAsync(cancellationToken);

                if (openOrgHistory is not null)
                {
                    if (openOrgHistory.StaffTransferId != transfer.StaffTransferId)
                        openOrgHistory.EffectiveTo = priorDay >= openOrgHistory.EffectiveFrom ? priorDay : openOrgHistory.EffectiveFrom;
                }
                else
                {
                    db.JobOrganisationHistories.Add(new JobOrganisationHistory
                    {
                        JobId = sourceJob.JobId,
                        IndividualId = transfer.IndividualId,
                        OrganisationId = transfer.FromOrganisationId,
                        OrganisationStructureId = transfer.FromOrganisationStructureId,
                        EffectiveFrom = sourceJob.JoinedDate.Date,
                        EffectiveTo = priorDay,
                        StaffTransferId = transfer.StaffTransferId,
                        ChangeReason = $"Prior placement before Transfer #{transfer.StaffTransferId}",
                        OperationLogId = auditLog.OperationLogId,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                if (jobDetail.TransferAppointmentMode == TransferAppointmentModes.NewEmploymentAppointment)
                {
                    sourceJob.TerminatedDate = priorDay;
                    sourceJob.JobStateId = SharedConfig.JobStates.TERMINATED_DUE_TO_PROMOTION;
                    sourceJob.OperationLogId = auditLog.OperationLogId;

                    var newJob = new Job
                    {
                        IndividualID = transfer.IndividualId,
                        OrganisationID = transfer.ToOrganisationId,
                        OrganisationStructureId = transfer.ToOrganisationStructureId,
                        JobStateId = SharedConfig.JobStates.APPROVED,
                        JobTypeId = jobDetail.DestinationJobTypeId ?? sourceJob.JobTypeId,
                        JoinedDate = effectiveDate,
                        BasicSalary = jobDetail.HasSalaryBenefitChange && jobDetail.ProposedBasicSalary.HasValue
                            ? jobDetail.ProposedBasicSalary.Value
                            : sourceJob.BasicSalary,
                        Sapnumber = sourceJob.Sapnumber,
                        ServiceId = sourceJob.ServiceId,
                        OperationLogId = auditLog.OperationLogId
                    };
                    db.Jobs.Add(newJob);
                    await db.SaveChangesAsync(cancellationToken);
                    targetJobId = newJob.JobId;
                }
                else
                {
                    sourceJob.OrganisationID = transfer.ToOrganisationId;
                    sourceJob.OrganisationStructureId = transfer.ToOrganisationStructureId;
                    if (jobDetail.DestinationJobTypeId.HasValue)
                        sourceJob.JobTypeId = jobDetail.DestinationJobTypeId.Value;
                    if (jobDetail.HasSalaryBenefitChange && jobDetail.ProposedBasicSalary.HasValue)
                        sourceJob.BasicSalary = jobDetail.ProposedBasicSalary.Value;
                    sourceJob.OperationLogId = auditLog.OperationLogId;
                    targetJobId = sourceJob.JobId;
                }

                jobDetail.ResultingJobId = targetJobId;

                if (!await db.JobOrganisationHistories.AnyAsync(
                    h => h.JobId == targetJobId && h.StaffTransferId == transfer.StaffTransferId && h.EffectiveTo == null,
                    cancellationToken))
                {
                    db.JobOrganisationHistories.Add(new JobOrganisationHistory
                    {
                        JobId = targetJobId,
                        IndividualId = transfer.IndividualId,
                        OrganisationId = transfer.ToOrganisationId,
                        OrganisationStructureId = transfer.ToOrganisationStructureId,
                        EffectiveFrom = effectiveDate,
                        EffectiveTo = null,
                        StaffTransferId = transfer.StaffTransferId,
                        ChangeReason = transfer.TransferReason,
                        OperationLogId = auditLog.OperationLogId,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.JobAndStructureReassignment,
                    jobStepKey, new { targetJobId, mode = jobDetail.TransferAppointmentMode });
            }

            // Step 3: Position & PF_JobStepHistory Update
            var posStepKey = $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:PositionHistory";
            if (!HasSucceededStep(transfer, StaffTransferActivationSteps.PositionAndStepHistory, posStepKey))
            {
                var destinationPositionId = jobDetail.DestinationPositionId ?? jobDetail.ExistingPositionId;
                var positionChanged = jobDetail.TransferAppointmentMode is TransferAppointmentModes.SameJobPositionChange or TransferAppointmentModes.NewEmploymentAppointment ||
                                      (jobDetail.DestinationPositionId.HasValue && jobDetail.DestinationPositionId != jobDetail.ExistingPositionId);

                if (positionChanged && destinationPositionId.HasValue)
                {
                    var priorDay = effectiveDate.AddDays(-1);
                    var activePositions = await db.JobPositions
                        .Where(jp => jp.JobId == sourceJob.JobId && jp.JobPositionStateId == SharedConfig.JobPositionStates.APPROVED &&
                                     (!jp.ToDate.HasValue || jp.ToDate.Value >= effectiveDate))
                        .ToListAsync(cancellationToken);

                    foreach (var activePos in activePositions)
                    {
                        activePos.ToDate = priorDay >= activePos.FromDate.Date ? priorDay : activePos.FromDate.Date;
                        activePos.OperationLogId = auditLog.OperationLogId;
                    }

                    var newJobPosition = new HRM.Models.JobPosition
                    {
                        JobId = targetJobId,
                        PositionId = destinationPositionId.Value,
                        FromDate = effectiveDate,
                        ToDate = null,
                        JobPositionStateId = SharedConfig.JobPositionStates.APPROVED,
                        OperationLogId = auditLog.OperationLogId
                    };
                    db.JobPositions.Add(newJobPosition);
                    await db.SaveChangesAsync(cancellationToken);
                    jobDetail.ResultingJobPositionId = newJobPosition.JobPositionId;
                }

                if (jobDetail.DestinationPositionMatrixId.HasValue && jobDetail.DestinationJobMatrixStepId.HasValue)
                {
                    var priorDay = effectiveDate.AddDays(-1);
                    var openSteps = await db.FrameworkJobStepHistories
                        .Where(s => s.JobId == sourceJob.JobId && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= effectiveDate))
                        .ToListAsync(cancellationToken);
                    foreach (var openStep in openSteps)
                    {
                        openStep.EffectiveTo = priorDay >= openStep.EffectiveFrom.Date ? priorDay : openStep.EffectiveFrom.Date;
                        openStep.OperationLogId = auditLog.OperationLogId;
                    }

                    var stepInfo = await db.FrameworkJobMatrixSteps.AsNoTracking()
                        .SingleOrDefaultAsync(s => s.JobMatrixStepId == jobDetail.DestinationJobMatrixStepId.Value, cancellationToken);
                    var matrixInfo = stepInfo is null
                        ? null
                        : await db.FrameworkJobMatrices.AsNoTracking()
                            .SingleOrDefaultAsync(m => m.JobMatrixId == stepInfo.JobMatrixId, cancellationToken);

                    if (stepInfo is not null && !await db.FrameworkJobStepHistories.AnyAsync(
                        s => s.JobId == targetJobId && s.EffectiveFrom == effectiveDate && s.JobMatrixStepId == stepInfo.JobMatrixStepId,
                        cancellationToken))
                    {
                        db.FrameworkJobStepHistories.Add(new FrameworkJobStepHistory
                        {
                            JobId = targetJobId,
                            PositionMatrixId = jobDetail.DestinationPositionMatrixId.Value,
                            JobMatrixStepId = stepInfo.JobMatrixStepId,
                            EffectiveFrom = effectiveDate,
                            EffectiveTo = null,
                            BasicSalarySnapshot = stepInfo.BasicSalaryOverride ?? matrixInfo?.BasicSalary ?? jobDetail.ProposedBasicSalary ?? 0m,
                            JobAllowanceSnapshot = stepInfo.JobAllowance,
                            OperationLogId = auditLog.OperationLogId
                        });
                    }
                }

                RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.PositionAndStepHistory,
                    posStepKey, new { jobDetail.ResultingJobPositionId, jobDetail.DestinationPositionMatrixId, jobDetail.DestinationJobMatrixStepId });
            }

            // Step 4: Attendance & Work Planning Reconciliation (preserve historical attendance, reconcile future plans)
            var reconciledCount = 0;
            var workStepKey = $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:WorkPlanning";
            if (!HasSucceededStep(transfer, StaffTransferActivationSteps.WorkPlanningReconciliation, workStepKey))
            {
                var scheduleDate = workDetail.ScheduleEffectiveDate.Date >= effectiveDate
                    ? workDetail.ScheduleEffectiveDate.Date
                    : effectiveDate;
                var priorDay = scheduleDate.AddDays(-1);

                // Close incompatible overlapping JobWorkTemplateAssignments from scheduleDate forward (do not delete history)
                var activeTemplateAssignments = await db.JobWorkTemplateAssignments
                    .Where(a => a.JobID == sourceJob.JobId && a.IsActive && (!a.EffectiveTo.HasValue || a.EffectiveTo.Value >= scheduleDate))
                    .ToListAsync(cancellationToken);

                foreach (var assignment in activeTemplateAssignments)
                {
                    if (assignment.EffectiveFrom.Date >= scheduleDate)
                    {
                        assignment.IsActive = false;
                        assignment.EffectiveTo = assignment.EffectiveFrom;
                    }
                    else
                    {
                        assignment.EffectiveTo = priorDay;
                    }
                    reconciledCount++;
                }

                var activeLegacyTemplates = await db.JobWorkTemplates
                    .Where(t => t.JobId == sourceJob.JobId && t.IsActive && (!t.EffectiveTo.HasValue || t.EffectiveTo.Value >= scheduleDate))
                    .ToListAsync(cancellationToken);

                foreach (var legacy in activeLegacyTemplates)
                {
                    if (legacy.EffectiveFrom.Date >= scheduleDate)
                    {
                        legacy.IsActive = false;
                        legacy.EffectiveTo = legacy.EffectiveFrom;
                    }
                    else
                    {
                        legacy.EffectiveTo = priorDay;
                    }
                }

                if (workDetail.ProposedWorkTemplateId.HasValue)
                {
                    var mask = workDetail.WorkingDaysMask is > 0 and <= 127 ? workDetail.WorkingDaysMask : 31;
                    db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment
                    {
                        JobID = targetJobId,
                        WorkTemplateID = workDetail.ProposedWorkTemplateId.Value,
                        EffectiveFrom = scheduleDate,
                        EffectiveTo = null,
                        IsActive = true,
                        SchedulePurpose = "Official",
                        ScheduleMode = string.IsNullOrWhiteSpace(workDetail.ProposedScheduleMode) ? "Standard" : workDetail.ProposedScheduleMode,
                        WorkingDaysMask = mask,
                        ScheduledStartTime = workDetail.StandardStartTime,
                        CreatedDate = DateTime.UtcNow
                    });

                    db.JobWorkTemplates.Add(new JobWorkTemplate
                    {
                        JobId = targetJobId,
                        WorkTemplateId = workDetail.ProposedWorkTemplateId.Value,
                        EffectiveFrom = scheduleDate,
                        EffectiveTo = null,
                        Sunday = (mask & 1) != 0,
                        Monday = (mask & 2) != 0,
                        Tuesday = (mask & 4) != 0,
                        Wednesday = (mask & 8) != 0,
                        Thursday = (mask & 16) != 0,
                        Friday = (mask & 32) != 0,
                        Saturday = (mask & 64) != 0,
                        Priority = 1,
                        IsActive = true,
                        OperationLogId = auditLog.OperationLogId
                    });
                    reconciledCount++;
                }
                else
                {
                    var hasScopeSchedule = await db.ScopeScheduleAssignments.AnyAsync(
                        s => s.OrganisationId == transfer.ToOrganisationId &&
                             (s.OrganisationStructureId == transfer.ToOrganisationStructureId || s.OrganisationStructureId == null) &&
                             s.IsActive && (!s.EffectiveTo.HasValue || s.EffectiveTo.Value >= scheduleDate),
                        cancellationToken);
                    if (!hasScopeSchedule)
                        warnings.Add("Receiving department has no assigned work template or scope schedule; HR should review the employee's work plan.");
                }

                if (workDetail.ReconcileFutureWorkPlans)
                {
                    // Relieve future planned/assigned work assignments starting on or after scheduleDate without touching past attendance
                    var futureOwners = await db.WorkAssignmentOwners
                        .Include(o => o.WorkAssignment)
                        .Where(o => o.JobId == sourceJob.JobId && o.IndividualId == transfer.IndividualId &&
                                    o.IsCurrentOwner && o.IsValid &&
                                    o.WorkAssignment.StartDateTime >= scheduleDate &&
                                    (o.WorkAssignment.WorkAssignmentStateId == SharedConfig.WorkAssignmentStates.PLANNED ||
                                     o.WorkAssignment.WorkAssignmentStateId == SharedConfig.WorkAssignmentStates.ASSIGNED))
                        .ToListAsync(cancellationToken);

                    foreach (var owner in futureOwners)
                    {
                        owner.IsCurrentOwner = false;
                        owner.EffectiveTo = scheduleDate;
                        owner.RelievedDate = DateTime.UtcNow;
                        owner.ReliefReason = $"Reconciled by Staff Transfer #{transfer.StaffTransferId} effective {scheduleDate:yyyy-MM-dd}";
                        owner.OperationLogId = auditLog.OperationLogId;
                        reconciledCount++;
                    }
                }

                RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.WorkPlanningReconciliation,
                    workStepKey, new { scheduleDate, workDetail.ProposedWorkTemplateId, reconciledCount, warnings });
            }

            // Step 5: Pending Approvals & Supervisory Routing
            var routingStepKey = $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:ApprovalRouting";
            if (!HasSucceededStep(transfer, StaffTransferActivationSteps.PendingApprovalsRouting, routingStepKey))
            {
                var routingRule = transfer.ReportingDetail?.OutstandingApprovalsRoutingRule ?? StaffTransferApprovalRoutingRules.RetainHistoricalReassignOpen;

                // Deactivate old department supervisor designation if employee is leaving FromOrganisationStructureId
                if (transfer.FromOrganisationStructureId != transfer.ToOrganisationStructureId)
                {
                    var oldSupervisors = await db.OrganisationStructureSupervisors
                        .Where(s => s.OrganisationStructureId == transfer.FromOrganisationStructureId &&
                                    s.SupervisorStaffId == transfer.IndividualId && s.IsActive)
                        .ToListAsync(cancellationToken);

                    foreach (var sup in oldSupervisors)
                    {
                        sup.IsActive = false;
                        sup.IsValid = false;
                        sup.OperationLogId = auditLog.OperationLogId;
                    }
                }

                RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.PendingApprovalsRouting,
                    routingStepKey, new { routingRule });
            }

            // Step 6: Organisational Access Provisioning
            var accessStepKey = $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:OrgAccess";
            if (!HasSucceededStep(transfer, StaffTransferActivationSteps.OrganisationalAccessProvisioning, accessStepKey))
            {
                foreach (var change in transfer.AccessChanges)
                {
                    if (change.RequiresSeparateSecurityAuth)
                    {
                        change.ProvisioningStatus = "RequiresManualAuth";
                    }
                    else if (change.TransitionAction is StaffTransferAccessActions.Revoke or StaffTransferAccessActions.RevokeOldGrantNew)
                    {
                        change.ProvisioningStatus = change.TransitionAction == StaffTransferAccessActions.Revoke ? "Revoked" : "Applied";
                        change.AppliedAtUtc = DateTime.UtcNow;
                        change.AppliedByUserId = transfer.FinalApprovedByUserId;
                    }
                    else
                    {
                        change.ProvisioningStatus = "Applied";
                        change.AppliedAtUtc = DateTime.UtcNow;
                        change.AppliedByUserId = transfer.FinalApprovedByUserId;
                    }
                }

                RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.OrganisationalAccessProvisioning,
                    accessStepKey, new { count = transfer.AccessChanges.Count });
            }

            // Step 7: Physical Door Access Provisioning Task Dispatch
            var dispatchedDoorTasks = 0;
            var doorStepKey = $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:DoorAccess";
            if (!HasSucceededStep(transfer, StaffTransferActivationSteps.DoorAccessTaskDispatch, doorStepKey))
            {
                foreach (var door in transfer.DoorAccessChanges)
                {
                    if (door.ProvisioningState == StaffTransferDoorAccessStates.StatePending)
                    {
                        door.ProvisioningState = StaffTransferDoorAccessStates.StateTaskGenerated;
                        dispatchedDoorTasks++;

                        if (door.SecurityOfficerUserId.HasValue)
                        {
                            db.UserTodos.Add(new UserTodo
                            {
                                UserId = door.SecurityOfficerUserId.Value,
                                Title = $"Door access provisioning — Staff Transfer #{transfer.StaffTransferId}",
                                Notes = $"Building: {door.BuildingName}, Area: {door.DepartmentOfficeArea ?? "General"}, Action: {door.TransitionAction}, Effective: {door.AccessStartDate:yyyy-MM-dd}",
                                Priority = door.IncludesServerRoom ? "High" : "Medium",
                                DueDate = door.AccessStartDate.Date,
                                CreatedAtUtc = DateTime.UtcNow,
                                UpdatedAtUtc = DateTime.UtcNow
                            });
                        }
                    }
                }

                RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.DoorAccessTaskDispatch,
                    doorStepKey, new { dispatchedDoorTasks });
            }

            // Step 8: Mark Transfer Active & Notify Stakeholders
            transfer.Status = StaffTransferStatuses.Active;
            transfer.IsOpen = false;
            transfer.ActivatedAt = DateTime.UtcNow;
            transfer.OperationLogId = auditLog.OperationLogId;
            transfer.UpdatedDate = DateTime.UtcNow;

            RecordStepIfNotSucceeded(db, transfer, attempt, StaffTransferActivationSteps.Completed,
                $"Transfer:{transfer.StaffTransferId}:V{transfer.CurrentVersion}:Completed",
                new { activatedAt = transfer.ActivatedAt, targetJobId });

            var employeeUserId = await db.Users.Where(u => u.BusinessEntityID == transfer.IndividualId && u.IsActive)
                .Select(u => (int?)u.UserId).FirstOrDefaultAsync(cancellationToken);
            if (employeeUserId.HasValue)
            {
                db.StaffNotifications.Add(new StaffNotification
                {
                    UserId = employeeUserId.Value,
                    IndividualId = transfer.IndividualId,
                    OrganisationId = transfer.ToOrganisationId,
                    Kind = "StaffTransferActivated",
                    Message = $"Your internal staff transfer (#{transfer.StaffTransferId}) is now active effective {effectiveDate:dd MMM yyyy}.",
                    Url = $"{Contracts.StaffTransferServiceContracts.OnlineConfirmationUrl}?id={transfer.StaffTransferId}",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new TransferActivationResult
            {
                StaffTransferId = transfer.StaffTransferId,
                Succeeded = true,
                WasAlreadyActive = false,
                ResultingStatus = StaffTransferStatuses.Active,
                ResultingJobId = targetJobId,
                ResultingJobPositionId = jobDetail.ResultingJobPositionId,
                ReconciledWorkPlanCount = reconciledCount,
                DispatchedDoorAccessTaskCount = dispatchedDoorTasks,
                Message = $"Transfer #{transfer.StaffTransferId} activated effective {effectiveDate:dd MMM yyyy}.",
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            logger?.LogError(ex, "Staff transfer activation failed for transfer {StaffTransferId}", staffTransferId);
            await RecordActivationFailureAsync(staffTransferId, ex.Message, cancellationToken);
            return new TransferActivationResult
            {
                StaffTransferId = staffTransferId,
                Succeeded = false,
                ResultingStatus = StaffTransferStatuses.ActivationFailed,
                Message = ex.Message
            };
        }
    }

    private static bool HasSucceededStep(StaffTransfer transfer, string stepCode, string idempotencyKey) =>
        transfer.ActivationLogs.Any(l =>
            l.StepCode == stepCode &&
            l.IdempotencyKey == idempotencyKey &&
            l.StepStatus == "Succeeded");

    private static void RecordStepIfNotSucceeded(
        HrmTeContext db,
        StaffTransfer transfer,
        int attempt,
        string stepCode,
        string idempotencyKey,
        object details)
    {
        if (HasSucceededStep(transfer, stepCode, idempotencyKey))
            return;

        var entry = new StaffTransferActivationLog
        {
            StaffTransferId = transfer.StaffTransferId,
            ExecutionAttempt = attempt,
            StepCode = stepCode,
            StepStatus = "Succeeded",
            IdempotencyKey = idempotencyKey,
            DetailsJson = JsonSerializer.Serialize(details),
            ExecutedByUserId = transfer.FinalApprovedByUserId,
            ExecutedAtUtc = DateTime.UtcNow
        };
        transfer.ActivationLogs.Add(entry);
        db.StaffTransferActivationLogs.Add(entry);
    }

    private async Task RecordActivationFailureAsync(int staffTransferId, string errorMessage, CancellationToken ct)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var transfer = await db.StaffTransfers
                .Include(t => t.ActivationLogs)
                .SingleOrDefaultAsync(t => t.StaffTransferId == staffTransferId, ct);
            if (transfer is null || transfer.Status == StaffTransferStatuses.Active)
                return;

            var attempt = (transfer.ActivationLogs.Select(l => (int?)l.ExecutionAttempt).Max() ?? 0) + 1;
            transfer.Status = StaffTransferStatuses.ActivationFailed;
            transfer.UpdatedDate = DateTime.UtcNow;

            db.StaffTransferActivationLogs.Add(new StaffTransferActivationLog
            {
                StaffTransferId = staffTransferId,
                ExecutionAttempt = attempt,
                StepCode = StaffTransferActivationSteps.Validation,
                StepStatus = "Failed",
                IdempotencyKey = $"Transfer:{staffTransferId}:Attempt:{attempt}:Failed",
                ErrorMessage = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage,
                ExecutedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
        }
        catch (Exception logEx)
        {
            logger?.LogWarning(logEx, "Could not persist activation failure state for transfer {StaffTransferId}", staffTransferId);
        }
    }
}

/// <summary>
/// Background scheduler that activates approved/scheduled transfers whose effective date has arrived.
/// </summary>
public sealed class StaffTransferActivationWorker(
    IServiceScopeFactory scopes,
    ILogger<StaffTransferActivationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<HrmTeContext>>();
                var activator = scope.ServiceProvider.GetRequiredService<IStaffTransferActivationService>();
                await using var db = await factory.CreateDbContextAsync(stoppingToken);

                var today = StaffTransferActivationService.TodayMaldives;
                var dueTransferIds = await db.StaffTransfers.AsNoTracking()
                    .Where(t => (t.Status == StaffTransferStatuses.Approved || t.Status == StaffTransferStatuses.Scheduled) &&
                                t.EffectiveDate <= today &&
                                t.ConfirmedVersion != null &&
                                t.ConfirmedVersion == t.CurrentVersion)
                    .OrderBy(t => t.EffectiveDate)
                    .ThenBy(t => t.StaffTransferId)
                    .Select(t => t.StaffTransferId)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var id in dueTransferIds)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    var result = await activator.ActivateAsync(id, stoppingToken);
                    if (!result.Succeeded)
                        logger.LogWarning("Scheduled activation failed for StaffTransfer #{StaffTransferId}: {Message}", id, result.Message);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "StaffTransferActivationWorker encountered an error while scanning due transfers.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
