using HRM.Components.Shared;
using HRM.Constants;
using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Attendance.Reconciliation;

public sealed class AttendanceReconciliationService : IAttendanceReconciliationService
{
    private readonly IDbContextFactory<HrmTeContext> _dbFactory;
    private readonly IWorkPlanGenerator? _workPlanGenerator;
    private readonly IUserAccessService? _access;
    private readonly IOperationLogService? _operationLogs;

    public AttendanceReconciliationService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IWorkPlanGenerator? workPlanGenerator = null,
        IUserAccessService? access = null,
        IOperationLogService? operationLogs = null)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _workPlanGenerator = workPlanGenerator;
        _access = access;
        _operationLogs = operationLogs;
    }

    public async Task<AttendanceReconciliationResult> ReconcileAsync(
        int individualId,
        int jobId,
        int organisationId,
        DateOnly workDate,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await ReconcileInternalAsync(
            db,
            individualId,
            jobId,
            organisationId,
            workDate,
            persistChanges: true,
            allowFinalizedOverride: true,
            triggerSource: "DailyReconciliation",
            initiatedByUserId: null,
            operationLogId: null,
            cancellationToken: cancellationToken);
    }

    public async Task<AttendanceBatchReconciliationResult> ReconcileRangeAsync(
        AttendanceBatchReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ToDate < request.FromDate)
            throw new ArgumentException("ToDate must be greater than or equal to FromDate.", nameof(request));

        int? actorUserId = null;
        int organisationId = request.OrganisationId;
        if (_access is not null)
        {
            var actor = await _access.GetContextAsync();
            if (actor is not null)
            {
                actorUserId = actor.UserId;
                if (organisationId <= 0)
                    organisationId = await _access.GetCurrentOrganisationIdAsync();
            }
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        int? operationLogId = null;
        if (!request.DryRunOnly && _operationLogs is not null)
        {
            operationLogId = await _operationLogs.CreateLogAsync(1, "Historical attendance reconciliation");
        }

        AttendanceReconciliationRun? runRecord = null;
        if (!request.DryRunOnly)
        {
            runRecord = new AttendanceReconciliationRun
            {
                OrganisationId = organisationId,
                IndividualId = request.IndividualId,
                JobId = request.JobId,
                FromWorkDate = request.FromDate.ToDateTime(TimeOnly.MinValue),
                ToWorkDate = request.ToDate.ToDateTime(TimeOnly.MinValue),
                TriggerSource = "HistoricalBatch",
                PolicyVersion = AttendanceReconciliationPolicy.CurrentVersion,
                InitiatedByUserId = actorUserId,
                OperationLogId = operationLogId,
                StartedAtUtc = DateTime.UtcNow,
                Status = "Running",
                SummaryMessage = request.Reason
            };
            db.AttendanceReconciliationRuns.Add(runRecord);
            await db.SaveChangesAsync(cancellationToken);
        }

        var jobsQuery = db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == organisationId && j.IndividualID > 0);

        if (request.IndividualId.HasValue && request.IndividualId.Value > 0)
            jobsQuery = jobsQuery.Where(j => j.IndividualID == request.IndividualId.Value);

        if (request.JobId.HasValue && request.JobId.Value > 0)
            jobsQuery = jobsQuery.Where(j => j.JobId == request.JobId.Value);

        var targetJobs = await jobsQuery
            .Select(j => new
            {
                j.JobId,
                IndividualId = j.IndividualID,
                j.OrganisationID
            })
            .ToListAsync(cancellationToken);

        var individualIds = targetJobs.Select(x => x.IndividualId).Distinct().ToList();
        var names = await db.Individuals.AsNoTracking()
            .Where(i => individualIds.Contains(i.BusinessEntityId))
            .ToDictionaryAsync(
                i => i.BusinessEntityId,
                i => string.Join(" ", new[] { i.FirstNameEnglish, i.MiddleNameEnglish, i.LastNameEnglish }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                cancellationToken);

        var batchResult = new AttendanceBatchReconciliationResult
        {
            ReconciliationRunId = runRecord?.AttendanceReconciliationRunId,
            IsDryRun = request.DryRunOnly,
            PolicyVersion = AttendanceReconciliationPolicy.CurrentVersion
        };

        for (var d = request.FromDate; d <= request.ToDate; d = d.AddDays(1))
        {
            var workDateTime = d.ToDateTime(TimeOnly.MinValue);
            foreach (var job in targetJobs)
            {
                var existingPlan = await db.WorkPlans.AsNoTracking()
                    .FirstOrDefaultAsync(
                        wp => wp.IndividualId == job.IndividualId &&
                              wp.JobId == job.JobId &&
                              wp.WorkDate == workDateTime &&
                              wp.IsValid,
                        cancellationToken);

                var hasLogs = await db.AttendanceLogs.AsNoTracking()
                    .AnyAsync(
                        l => l.IndividualId == job.IndividualId &&
                             l.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
                             l.Date >= workDateTime.AddHours(-4) &&
                             l.Date < workDateTime.AddDays(1).AddHours(6),
                        cancellationToken);

                if (existingPlan is null && !hasLogs)
                    continue;

                batchResult.TotalEmployeeDaysEvaluated++;

                if (existingPlan is { IsFinalized: true } && !request.AllowFinalizedPlanOverride)
                {
                    batchResult.TotalSkippedFinalizedOrPayrollLocked++;
                    batchResult.Changes.Add(new AttendanceBatchReconciliationDayDiffDto
                    {
                        IndividualId = job.IndividualId,
                        JobId = job.JobId,
                        EmployeeName = names.GetValueOrDefault(job.IndividualId, $"Staff #{job.IndividualId}"),
                        WorkDate = d,
                        WasSkippedLockedOrFinalized = true,
                        ChangeSummary = "Skipped because work plan is finalized and AllowFinalizedPlanOverride is false."
                    });
                    continue;
                }

                // Capture before snapshot
                var beforeObligation = await db.AttendanceObligations.AsNoTracking()
                    .Where(o => o.IndividualId == job.IndividualId && o.JobId == job.JobId && o.WorkDate == workDateTime && o.IsValid && o.ObligationType == "Official")
                    .OrderByDescending(o => o.AttendanceObligationId)
                    .FirstOrDefaultAsync(cancellationToken);

                var reconciled = await ReconcileInternalAsync(
                    db,
                    job.IndividualId,
                    job.JobId,
                    job.OrganisationID,
                    d,
                    persistChanges: !request.DryRunOnly,
                    allowFinalizedOverride: request.AllowFinalizedPlanOverride,
                    triggerSource: request.DryRunOnly ? "HistoricalDryRun" : "HistoricalBatch",
                    initiatedByUserId: actorUserId,
                    operationLogId: operationLogId,
                    parentRunId: runRecord?.AttendanceReconciliationRunId,
                    cancellationToken: cancellationToken);

                batchResult.TotalSharedEventsLinked += reconciled.SharedEventCount;
                batchResult.TotalConflictsFlagged += reconciled.ConflictCount;
                batchResult.TotalEventsReclassified += reconciled.ResolvedEventCount;

                var beforeState = beforeObligation?.ObligationState ?? "Unreconciled";
                var afterState = reconciled.OfficialState.ToString();
                var changed = beforeObligation is null ||
                              beforeState != afterState ||
                              beforeObligation.ActualStartDateTime != reconciled.OfficialCheckIn ||
                              beforeObligation.ActualEndDateTime != reconciled.OfficialCheckOut ||
                              beforeObligation.CreditedWorkedMinutes != reconciled.CreditedOfficialMinutes;

                if (changed)
                {
                    batchResult.TotalDaysChanged++;
                    batchResult.Changes.Add(new AttendanceBatchReconciliationDayDiffDto
                    {
                        IndividualId = job.IndividualId,
                        JobId = job.JobId,
                        EmployeeName = names.GetValueOrDefault(job.IndividualId, $"Staff #{job.IndividualId}"),
                        WorkDate = d,
                        BeforeOfficialState = beforeState,
                        AfterOfficialState = afterState,
                        BeforeOfficialCheckIn = beforeObligation?.ActualStartDateTime,
                        AfterOfficialCheckIn = reconciled.OfficialCheckIn,
                        BeforeOfficialCheckOut = beforeObligation?.ActualEndDateTime,
                        AfterOfficialCheckOut = reconciled.OfficialCheckOut,
                        BeforeWorkedMinutes = beforeObligation?.CreditedWorkedMinutes ?? 0,
                        AfterCreditedOfficialMinutes = reconciled.CreditedOfficialMinutes,
                        AfterCreditedDutyMinutes = reconciled.CreditedDutyMinutes,
                        SharedEventsResolved = reconciled.SharedEventCount,
                        ConflictsDetected = reconciled.ConflictCount,
                        ChangeSummary = reconciled.SummaryMessage
                    });
                }
            }
        }

        batchResult.SummaryMessage =
            $"Evaluated {batchResult.TotalEmployeeDaysEvaluated} employee-day(s); " +
            $"{batchResult.TotalDaysChanged} updated, {batchResult.TotalSharedEventsLinked} shared event(s), " +
            $"{batchResult.TotalConflictsFlagged} conflict(s), {batchResult.TotalSkippedFinalizedOrPayrollLocked} skipped.";

        if (runRecord is not null)
        {
            runRecord.CompletedAtUtc = DateTime.UtcNow;
            runRecord.TotalDaysEvaluated = batchResult.TotalEmployeeDaysEvaluated;
            runRecord.TotalEventsProcessed = batchResult.TotalEventsReclassified;
            runRecord.ConflictCount = batchResult.TotalConflictsFlagged;
            runRecord.Status = "Completed";
            runRecord.SummaryMessage = batchResult.SummaryMessage;
            await db.SaveChangesAsync(cancellationToken);
        }

        return batchResult;
    }

    public async Task<AttendanceReconciliationResult> ApplyManualOverrideAsync(
        AttendanceManualOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("A mandatory audit reason is required for manual attendance overrides.", nameof(request));

        int actorUserId = 1;
        int organisationId = request.OrganisationId;
        if (_access is not null)
        {
            var actor = await _access.GetContextAsync();
            if (actor is not null)
            {
                actorUserId = actor.UserId;
                if (organisationId <= 0)
                    organisationId = await _access.GetCurrentOrganisationIdAsync();
            }
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var workDateTime = request.WorkDate.ToDateTime(TimeOnly.MinValue);

        var workPlan = await db.WorkPlans.AsNoTracking()
            .FirstOrDefaultAsync(
                wp => wp.IndividualId == request.IndividualId &&
                      wp.JobId == request.JobId &&
                      wp.WorkDate == workDateTime &&
                      wp.IsValid,
                cancellationToken);

        if (workPlan is { IsFinalized: true } && !request.AllowFinalizedPlanOverride)
        {
            throw new InvalidOperationException(
                "Cannot apply a manual override to a finalized work plan unless AllowFinalizedPlanOverride is explicitly authorized.");
        }

        int? operationLogId = null;
        if (_operationLogs is not null)
        {
            operationLogId = await _operationLogs.CreateLogAsync(1, $"Manual attendance override: {request.Reason}");
        }

        var existingOverrides = await db.AttendanceManualOverrides
            .Where(o => o.IndividualId == request.IndividualId &&
                        o.JobId == request.JobId &&
                        o.WorkDate == workDateTime &&
                        o.AttendanceLogId == request.AttendanceLogId &&
                        o.TargetSchedulePurpose == request.TargetSchedulePurpose &&
                        o.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var prev in existingOverrides)
            prev.IsActive = false;

        var newOverride = new AttendanceManualOverride
        {
            OrganisationId = organisationId > 0 ? organisationId : workPlan?.OrganisationBusinessEntityId ?? 1,
            IndividualId = request.IndividualId,
            JobId = request.JobId,
            WorkDate = workDateTime,
            AttendanceLogId = request.AttendanceLogId,
            WorkPlanId = request.WorkPlanId ?? workPlan?.WorkPlanId,
            WorkAssignmentId = request.WorkAssignmentId,
            WorkPlanSegmentId = request.WorkPlanSegmentId,
            OverrideAction = string.IsNullOrWhiteSpace(request.OverrideAction) ? "AssignPrimary" : request.OverrideAction.Trim(),
            TargetSchedulePurpose = string.IsNullOrWhiteSpace(request.TargetSchedulePurpose) ? "Official" : request.TargetSchedulePurpose.Trim(),
            TargetClockTypeId = request.TargetClockTypeId,
            Reason = request.Reason.Trim(),
            CreatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow,
            OperationLogId = operationLogId,
            IsActive = true
        };

        db.AttendanceManualOverrides.Add(newOverride);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var prev in existingOverrides)
            prev.SupersededByOverrideId = newOverride.AttendanceManualOverrideId;
        if (existingOverrides.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return await ReconcileInternalAsync(
            db,
            request.IndividualId,
            request.JobId,
            newOverride.OrganisationId,
            request.WorkDate,
            persistChanges: true,
            allowFinalizedOverride: request.AllowFinalizedPlanOverride,
            triggerSource: "ManualOverride",
            initiatedByUserId: actorUserId,
            operationLogId: operationLogId,
            cancellationToken: cancellationToken);
    }

    internal async Task<AttendanceReconciliationResult> ReconcileInternalAsync(
        HrmTeContext db,
        int individualId,
        int jobId,
        int organisationId,
        DateOnly workDate,
        bool persistChanges,
        bool allowFinalizedOverride,
        string triggerSource,
        int? initiatedByUserId,
        int? operationLogId,
        long? parentRunId = null,
        CancellationToken cancellationToken = default)
    {
        var workDateTime = workDate.ToDateTime(TimeOnly.MinValue);

        if (organisationId <= 0)
        {
            organisationId = await db.Jobs.AsNoTracking()
                .Where(j => j.JobId == jobId)
                .Select(j => j.OrganisationID)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // 1. Load or generate WorkPlan
        var workPlan = await LoadWorkPlanAsync(db, individualId, jobId, workDateTime, cancellationToken);
        if (workPlan is null && _workPlanGenerator is not null && organisationId > 0)
        {
            await _workPlanGenerator.GenerateOrGetAsync(individualId, jobId, organisationId, workDate, cancellationToken);
            workPlan = await LoadWorkPlanAsync(db, individualId, jobId, workDateTime, cancellationToken);
        }

        var result = new AttendanceReconciliationResult
        {
            IndividualId = individualId,
            JobId = jobId,
            OrganisationId = organisationId > 0 ? organisationId : workPlan?.OrganisationBusinessEntityId ?? 0,
            WorkDate = workDate,
            PolicyVersion = AttendanceReconciliationPolicy.CurrentVersion
        };

        // 2. Build Planned Obligations (Official, Official Break/Lunch, and Duties)
        var obligations = BuildObligations(workPlan, workDateTime);
        var officialObligations = obligations.Where(o => o.ObligationType == "Official").OrderBy(o => o.ScheduledStart).ToList();
        var breakObligations = obligations.Where(o => o.ObligationType == "OfficialBreak").OrderBy(o => o.ScheduledStart).ToList();
        var dutyObligations = obligations.Where(o => o.ObligationType == "Duty").OrderBy(o => o.ScheduledStart).ThenBy(o => o.Priority).ThenBy(o => o.WorkAssignmentId).ToList();

        // 3. Determine Raw Event Retrieval Window (handling overnight assignments cleanly)
        var windowStart = workDateTime.AddHours(-3);
        var windowEnd = workDateTime.AddDays(1).AddHours(3);
        if (obligations.Count > 0)
        {
            var minSched = obligations.Min(o => o.ScheduledStart.AddMinutes(-Math.Max(o.CheckInGraceBeforeMinutes, 180)));
            var maxSched = obligations.Max(o => o.ScheduledEnd.AddMinutes(Math.Max(o.CheckOutGraceAfterMinutes, 240)));
            if (minSched < windowStart) windowStart = minSched;
            if (maxSched > windowEnd) windowEnd = maxSched;
        }

        var hasOvernightObligation = obligations.Any(o => o.ScheduledEnd.Date > workDateTime.Date);
        var nextDayPlans = await db.WorkPlans.AsNoTracking()
            .Include(wp => wp.WorkAssignments)
            .Include(wp => wp.WorkPlanSegments)
            .Where(wp => wp.IndividualId == individualId && wp.JobId == jobId && wp.WorkDate == workDateTime.AddDays(1) && wp.IsValid)
            .ToListAsync(cancellationToken);
        var nextDayEarliestStart = nextDayPlans
            .SelectMany(wp => wp.WorkAssignments.Where(a => a.IsValid && a.CancelledDate == null).Select(a => a.StartDateTime.AddMinutes(-Math.Max(a.GraceMinutes, 60))))
            .DefaultIfEmpty(workDateTime.AddDays(1).AddHours(6))
            .Min();

        var rawLogs = await db.AttendanceLogs.AsNoTracking()
            .Where(l => l.IndividualId == individualId &&
                        l.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
                        l.Date >= windowStart &&
                        l.Date <= windowEnd)
            .OrderBy(l => l.Date)
            .ThenBy(l => l.AttendanceLogId)
            .ToListAsync(cancellationToken);

        // Filter out events belonging strictly to the previous or next calendar date unless covered by this date's obligations
        rawLogs = rawLogs.Where(l =>
        {
            if (l.Date.Date == workDateTime.Date)
                return true;

            if (l.Date.Date < workDateTime.Date)
            {
                return obligations.Any(o =>
                    l.Date >= o.ScheduledStart.AddMinutes(-Math.Max(o.CheckInGraceBeforeMinutes, 120)) &&
                    l.Date <= o.ScheduledEnd.AddMinutes(Math.Max(o.CheckOutGraceAfterMinutes, 120)));
            }

            // l.Date.Date > workDateTime.Date
            if (!hasOvernightObligation)
            {
                // Allow slightly post-midnight checkout only if an obligation ended at or near midnight
                return obligations.Any(o =>
                    o.ScheduledEnd >= workDateTime.AddHours(22) &&
                    l.Date <= o.ScheduledEnd.AddMinutes(Math.Max(o.CheckOutGraceAfterMinutes, 120)) &&
                    l.Date < nextDayEarliestStart);
            }

            return obligations.Any(o =>
                o.ScheduledEnd.Date > workDateTime.Date &&
                l.Date <= o.ScheduledEnd.AddMinutes(Math.Max(o.CheckOutGraceAfterMinutes, 180)) &&
                l.Date < nextDayEarliestStart);
        }).ToList();

        result.RawEventCount = rawLogs.Count;

        // Load active manual overrides for this day
        var manualOverrides = await db.AttendanceManualOverrides.AsNoTracking()
            .Where(o => o.IndividualId == individualId &&
                        o.JobId == jobId &&
                        o.WorkDate == workDateTime &&
                        o.IsActive)
            .OrderByDescending(o => o.AttendanceManualOverrideId)
            .ToListAsync(cancellationToken);

        // 4. Match Clock Events Deterministically
        MatchClockEventsDeterministically(
            rawLogs,
            officialObligations,
            breakObligations,
            dutyObligations,
            manualOverrides,
            workPlan,
            result);

        // 5. Evaluate Obligation States, Durations, Breaks, and Conflicts
        EvaluateObligationsAndDurations(
            workDateTime,
            workPlan,
            officialObligations,
            breakObligations,
            dutyObligations,
            manualOverrides,
            result);

        // 6. Persist Reconciled State if requested
        if (persistChanges && (workPlan is not null || rawLogs.Count > 0))
        {
            if (workPlan is not { IsFinalized: true } || allowFinalizedOverride)
            {
                await PersistReconciliationAsync(
                    db,
                    result,
                    workPlan,
                    triggerSource,
                    initiatedByUserId,
                    operationLogId,
                    parentRunId,
                    cancellationToken);
            }
        }

        return result;
    }

    private static async Task<WorkPlan?> LoadWorkPlanAsync(
        HrmTeContext db,
        int individualId,
        int jobId,
        DateTime workDateTime,
        CancellationToken cancellationToken)
    {
        return await db.WorkPlans
            .AsNoTracking()
            .Include(wp => wp.WorkAssignments)
            .Include(wp => wp.WorkPlanSegments)
                .ThenInclude(s => s.WorkSegmentType)
            .Include(wp => wp.WorkPlanSegments)
                .ThenInclude(s => s.WorkTemplateSegment)
            .Where(wp => wp.IndividualId == individualId &&
                         wp.JobId == jobId &&
                         wp.WorkDate == workDateTime &&
                         wp.IsValid)
            .OrderByDescending(wp => wp.IsManual)
            .ThenByDescending(wp => wp.WorkPlanId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed class ObligationRuntime
    {
        public long? WorkPlanId { get; init; }
        public long? WorkAssignmentId { get; init; }
        public int? WorkPlanSegmentId { get; init; }
        public int? CheckInSegmentId { get; init; }
        public int? CheckOutSegmentId { get; init; }
        public string CheckInSegmentName { get; init; } = string.Empty;
        public string CheckOutSegmentName { get; init; } = string.Empty;
        public string ObligationType { get; init; } = "Official"; // Official, OfficialBreak, Duty
        public string ScheduleName { get; init; } = string.Empty;
        public int Priority { get; init; } = 1;
        public DateTime ScheduledStart { get; init; }
        public DateTime ScheduledEnd { get; init; }
        public int CheckInGraceBeforeMinutes { get; init; } = 60;
        public int CheckInGraceAfterMinutes { get; init; } = 60;
        public int CheckOutGraceBeforeMinutes { get; init; } = 30;
        public int CheckOutGraceAfterMinutes { get; init; } = 90;
        public bool RequiresAttendance { get; init; } = true;
        public bool RequiresCheckOut { get; init; } = true;
        public bool IsFlexibleBreak { get; init; }

        public AttendanceObligationState State { get; set; } = AttendanceObligationState.NotStarted;
        public int? StartLogId { get; set; }
        public int? EndLogId { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }
        public int? BreakOutLogId { get; set; }
        public int? BreakInLogId { get; set; }
        public DateTime? BreakOutTime { get; set; }
        public DateTime? BreakInTime { get; set; }
        public bool SharesStartEvent { get; set; }
        public bool SharesEndEvent { get; set; }
        public string StatusReason { get; set; } = string.Empty;
    }

    private static List<ObligationRuntime> BuildObligations(WorkPlan? workPlan, DateTime workDateTime)
    {
        var list = new List<ObligationRuntime>();
        if (workPlan is null)
            return list;

        var activeAssignments = workPlan.WorkAssignments
            .Where(a => a.IsValid && a.CancelledDate == null)
            .OrderBy(a => a.SchedulePurpose == "Official" ? 0 : 1)
            .ThenBy(a => a.StartDateTime)
            .ThenBy(a => a.Priority)
            .ThenBy(a => a.WorkAssignmentId)
            .ToList();

        var segments = workPlan.WorkPlanSegments
            .OrderBy(s => s.SequenceNumber)
            .ThenBy(s => s.StartDateTime)
            .ThenBy(s => s.WorkPlanSegmentId)
            .ToList();

        // Build from WorkAssignments paired with their WorkPlanSegments
        int dutyIndex = 0;
        foreach (var assignment in activeAssignments)
        {
            var purpose = string.Equals(assignment.SchedulePurpose, "Duty", StringComparison.OrdinalIgnoreCase)
                ? "Duty"
                : "Official";

            var matchingSegments = segments
                .Where(s =>
                    string.Equals(SegmentPurpose(s, activeAssignments), purpose, StringComparison.OrdinalIgnoreCase) &&
                    s.StartDateTime < assignment.EndDateTime.AddHours(2) &&
                    s.EndDateTime > assignment.StartDateTime.AddHours(-2))
                .ToList();

            var checkInSeg = matchingSegments.FirstOrDefault(s => IsCheckInSegment(s))
                ?? matchingSegments.FirstOrDefault(s => IsWorkSegment(s));
            var checkOutSeg = matchingSegments.LastOrDefault(s => IsCheckOutSegment(s))
                ?? matchingSegments.LastOrDefault(s => IsWorkSegment(s));

            var start = assignment.StartDateTime;
            var end = assignment.EndDateTime;
            if (end <= start)
                end = end.AddDays(1);

            list.Add(new ObligationRuntime
            {
                WorkPlanId = workPlan.WorkPlanId,
                WorkAssignmentId = assignment.WorkAssignmentId,
                WorkPlanSegmentId = checkInSeg?.WorkPlanSegmentId ?? checkOutSeg?.WorkPlanSegmentId,
                CheckInSegmentId = checkInSeg?.WorkPlanSegmentId,
                CheckOutSegmentId = checkOutSeg?.WorkPlanSegmentId ?? checkInSeg?.WorkPlanSegmentId,
                CheckInSegmentName = checkInSeg?.Name ?? $"{assignment.Name} Check-In",
                CheckOutSegmentName = checkOutSeg?.Name ?? $"{assignment.Name} Check-Out",
                ObligationType = purpose,
                ScheduleName = string.IsNullOrWhiteSpace(assignment.Name) ? purpose : assignment.Name,
                Priority = purpose == "Official" ? 1 : 10 + (++dutyIndex),
                ScheduledStart = start,
                ScheduledEnd = end,
                CheckInGraceBeforeMinutes = Math.Max(checkInSeg?.GraceBeforeMinutes ?? 0, Math.Max(assignment.GraceMinutes, 90)),
                CheckInGraceAfterMinutes = Math.Max(checkInSeg?.GraceAfterMinutes ?? 0, Math.Max(assignment.GraceMinutes, 60)),
                CheckOutGraceBeforeMinutes = Math.Max(checkOutSeg?.GraceBeforeMinutes ?? 0, 30),
                CheckOutGraceAfterMinutes = Math.Max(checkOutSeg?.GraceAfterMinutes ?? 0, 90),
                RequiresAttendance = assignment.RequiresAttendance || matchingSegments.Any(s => s.RequiresAttendance),
                RequiresCheckOut = assignment.RequiresCheckOut || matchingSegments.Any(s => IsCheckOutSegment(s) && s.RequiresAttendance)
            });
        }

        // Fallback if WorkPlan has segments but no WorkAssignments rows
        if (list.Count == 0 && segments.Count > 0)
        {
            var officialSegs = segments.Where(s => SegmentPurpose(s, activeAssignments) == "Official" && !IsBreakSegment(s)).ToList();
            if (officialSegs.Count > 0)
            {
                var checkInSeg = officialSegs.FirstOrDefault(IsCheckInSegment) ?? officialSegs.First();
                var checkOutSeg = officialSegs.LastOrDefault(IsCheckOutSegment) ?? officialSegs.Last();
                var start = checkInSeg.StartDateTime;
                var end = checkOutSeg.WorkSegmentTypeId == 1 && checkOutSeg != checkInSeg
                    ? checkOutSeg.StartDateTime
                    : checkOutSeg.EndDateTime;
                if (end <= start)
                    end = checkOutSeg.EndDateTime > start ? checkOutSeg.EndDateTime : start.AddHours(6);

                list.Add(new ObligationRuntime
                {
                    WorkPlanId = workPlan.WorkPlanId,
                    WorkPlanSegmentId = checkInSeg.WorkPlanSegmentId,
                    CheckInSegmentId = checkInSeg.WorkPlanSegmentId,
                    CheckOutSegmentId = checkOutSeg.WorkPlanSegmentId,
                    CheckInSegmentName = checkInSeg.Name,
                    CheckOutSegmentName = checkOutSeg.Name,
                    ObligationType = "Official",
                    ScheduleName = "Official Working Hours",
                    Priority = 1,
                    ScheduledStart = start,
                    ScheduledEnd = end,
                    CheckInGraceBeforeMinutes = Math.Max(checkInSeg.GraceBeforeMinutes, 90),
                    CheckInGraceAfterMinutes = Math.Max(checkInSeg.GraceAfterMinutes, 60),
                    CheckOutGraceBeforeMinutes = Math.Max(checkOutSeg.GraceBeforeMinutes, 30),
                    CheckOutGraceAfterMinutes = Math.Max(checkOutSeg.GraceAfterMinutes, 90),
                    RequiresAttendance = officialSegs.Any(s => s.RequiresAttendance),
                    RequiresCheckOut = officialSegs.Count > 1 || checkOutSeg.RequiresAttendance
                });
            }

            var dutySegs = segments.Where(s => SegmentPurpose(s, activeAssignments) == "Duty" && !IsBreakSegment(s)).ToList();
            if (dutySegs.Count > 0)
            {
                var checkInSeg = dutySegs.FirstOrDefault(IsCheckInSegment) ?? dutySegs.First();
                var checkOutSeg = dutySegs.LastOrDefault(IsCheckOutSegment) ?? dutySegs.Last();
                var start = checkInSeg.StartDateTime;
                var end = checkOutSeg.EndDateTime > start ? checkOutSeg.EndDateTime : start.AddHours(2);

                list.Add(new ObligationRuntime
                {
                    WorkPlanId = workPlan.WorkPlanId,
                    WorkPlanSegmentId = checkInSeg.WorkPlanSegmentId,
                    CheckInSegmentId = checkInSeg.WorkPlanSegmentId,
                    CheckOutSegmentId = checkOutSeg.WorkPlanSegmentId,
                    CheckInSegmentName = checkInSeg.Name,
                    CheckOutSegmentName = checkOutSeg.Name,
                    ObligationType = "Duty",
                    ScheduleName = "Additional Duty",
                    Priority = 11,
                    ScheduledStart = start,
                    ScheduledEnd = end,
                    CheckInGraceBeforeMinutes = Math.Max(checkInSeg.GraceBeforeMinutes, 60),
                    CheckInGraceAfterMinutes = Math.Max(checkInSeg.GraceAfterMinutes, 60),
                    CheckOutGraceBeforeMinutes = Math.Max(checkOutSeg.GraceBeforeMinutes, 30),
                    CheckOutGraceAfterMinutes = Math.Max(checkOutSeg.GraceAfterMinutes, 90),
                    RequiresAttendance = dutySegs.Any(s => s.RequiresAttendance),
                    RequiresCheckOut = dutySegs.Count > 1 || checkOutSeg.RequiresAttendance
                });
            }
        }

        // Add Official Break / Lunch obligations from Break/Lunch segments
        var breakSegments = segments.Where(IsBreakSegment).ToList();
        foreach (var bSeg in breakSegments)
        {
            var start = bSeg.StartDateTime;
            var end = bSeg.EndDateTime > start ? bSeg.EndDateTime : start.AddMinutes(60);
            list.Add(new ObligationRuntime
            {
                WorkPlanId = workPlan.WorkPlanId,
                WorkPlanSegmentId = bSeg.WorkPlanSegmentId,
                CheckInSegmentId = bSeg.WorkPlanSegmentId,
                CheckOutSegmentId = bSeg.WorkPlanSegmentId,
                CheckInSegmentName = bSeg.Name,
                CheckOutSegmentName = bSeg.Name,
                ObligationType = "OfficialBreak",
                ScheduleName = string.IsNullOrWhiteSpace(bSeg.Name) ? "Official Lunch Break" : bSeg.Name,
                Priority = 2,
                ScheduledStart = start,
                ScheduledEnd = end,
                CheckInGraceBeforeMinutes = Math.Max(bSeg.GraceBeforeMinutes, 30),
                CheckInGraceAfterMinutes = Math.Max(bSeg.GraceAfterMinutes, 45),
                CheckOutGraceBeforeMinutes = Math.Max(bSeg.GraceBeforeMinutes, 30),
                CheckOutGraceAfterMinutes = Math.Max(bSeg.GraceAfterMinutes, 45),
                RequiresAttendance = bSeg.RequiresAttendance,
                RequiresCheckOut = bSeg.RequiresAttendance,
                IsFlexibleBreak = (end - start).TotalMinutes > 60
            });
        }

        return list;
    }

    private static string SegmentPurpose(WorkPlanSegment segment, List<WorkAssignment> assignments)
    {
        if (!string.IsNullOrWhiteSpace(segment.SchedulePurpose))
            return segment.SchedulePurpose;
        if (segment.WorkSegmentType?.Code is string code && code.StartsWith("DUTY", StringComparison.OrdinalIgnoreCase))
            return "Duty";
        if (segment.Name.Contains("Duty", StringComparison.OrdinalIgnoreCase) ||
            segment.Name.Contains("Shift", StringComparison.OrdinalIgnoreCase))
            return "Duty";
        if (segment.Name.Contains("Official", StringComparison.OrdinalIgnoreCase))
            return "Official";

        var matchedAssignment = assignments
            .Where(a => segment.StartDateTime >= a.StartDateTime.AddMinutes(-90) &&
                        segment.EndDateTime <= a.EndDateTime.AddMinutes(90))
            .OrderBy(a => Math.Abs((a.StartDateTime - segment.StartDateTime).TotalMinutes))
            .FirstOrDefault();

        return matchedAssignment?.SchedulePurpose ?? "Official";
    }

    private static bool IsBreakSegment(WorkPlanSegment s) =>
        !IsCheckInSegment(s) &&
        !IsCheckOutSegment(s) &&
        (string.Equals(s.WorkSegmentType?.Code, "LUNCH", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(s.WorkSegmentType?.Code, "BREAK", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(s.WorkSegmentType?.Code, "BREAK_START", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(s.WorkSegmentType?.Code, "BREAK_END", StringComparison.OrdinalIgnoreCase) ||
         s.Name.Contains("Lunch", StringComparison.OrdinalIgnoreCase) ||
         s.Name.Contains("Break", StringComparison.OrdinalIgnoreCase));

    private static bool IsCheckInSegment(WorkPlanSegment s) =>
        string.Equals(s.WorkSegmentType?.Code, "CHECK_IN", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(s.WorkSegmentType?.Code, "DUTY_CHECK_IN", StringComparison.OrdinalIgnoreCase) ||
        s.Name.Contains("Check-In", StringComparison.OrdinalIgnoreCase) ||
        s.Name.Contains("Check In", StringComparison.OrdinalIgnoreCase);

    private static bool IsCheckOutSegment(WorkPlanSegment s) =>
        string.Equals(s.WorkSegmentType?.Code, "CHECK_OUT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(s.WorkSegmentType?.Code, "DUTY_CHECK_OUT", StringComparison.OrdinalIgnoreCase) ||
        s.Name.Contains("Check-Out", StringComparison.OrdinalIgnoreCase) ||
        s.Name.Contains("Check Out", StringComparison.OrdinalIgnoreCase);

    private static bool IsWorkSegment(WorkPlanSegment s) =>
        s.WorkSegmentTypeId == 1 && !IsCheckInSegment(s) && !IsCheckOutSegment(s) && !IsBreakSegment(s);

    private static AttendanceClockType? GetExplicitDeviceDirection(AttendanceLog log)
    {
        static AttendanceClockType? MapDeviceMode(int mode) => mode switch
        {
            1 => AttendanceClockType.CheckIn,
            2 => AttendanceClockType.CheckOut,
            3 => AttendanceClockType.BreakOut,
            4 => AttendanceClockType.BreakIn,
            _ => null
        };

        if (log.ActualInOutMode.HasValue)
        {
            var mapped = MapDeviceMode(log.ActualInOutMode.Value);
            if (mapped.HasValue)
                return mapped;
        }

        // Online clock uses AttendanceLogModeId == 2 with InOutModeId == 3 as automatic mode
        if (log.AttendanceLogModeId == 2 && log.InOutModeId == 3)
            return null;

        // Physical terminal direction (1 = CheckIn, 2 = CheckOut, 3 = BreakOut, 4 = BreakIn)
        if (log.AttendanceLogModeId != 2 && log.InOutModeId is >= 1 and <= 4)
            return MapDeviceMode(log.InOutModeId);

        return null;
    }

    private static void MatchClockEventsDeterministically(
        List<AttendanceLog> rawLogs,
        List<ObligationRuntime> officialObligations,
        List<ObligationRuntime> breakObligations,
        List<ObligationRuntime> dutyObligations,
        List<AttendanceManualOverride> manualOverrides,
        WorkPlan? workPlan,
        AttendanceReconciliationResult result)
    {
        var primaryOfficial = officialObligations.FirstOrDefault();

        for (int i = 0; i < rawLogs.Count; i++)
        {
            var log = rawLogs[i];
            var clockTime = log.Date;
            var explicitDir = GetExplicitDeviceDirection(log);

            var eventDto = new AttendanceEventInterpretationDto
            {
                AttendanceLogId = log.AttendanceLogId,
                LogDateTime = clockTime,
                RecordedInOutModeId = log.InOutModeId,
                ActualInOutModeId = log.ActualInOutMode,
                AttendanceLogModeId = log.AttendanceLogModeId,
                IsDeviceDirectionExplicit = explicitDir.HasValue
            };

            // Step 0: Check active Manual Override for this specific AttendanceLogId
            var logOverrides = manualOverrides
                .Where(o => o.AttendanceLogId == log.AttendanceLogId)
                .ToList();

            if (logOverrides.Any(o => string.Equals(o.OverrideAction, "SuppressDuplicate", StringComparison.OrdinalIgnoreCase)))
            {
                MarkDuplicateSuppressed(eventDto, workPlan, "ManualOverride", logOverrides.First().Reason);
                result.DuplicateSuppressedCount++;
                result.Events.Add(eventDto);
                continue;
            }

            if (logOverrides.Count > 0)
            {
                ApplyManualOverridesToEvent(log, eventDto, logOverrides, officialObligations, breakObligations, dutyObligations, workPlan);
                result.ResolvedEventCount++;
                if (eventDto.IsSharedAcrossObligations)
                    result.SharedEventCount++;
                result.Events.Add(eventDto);
                continue;
            }

            // Step 1: Duplicate / Noisy Punch Suppression
            // If within DefaultDuplicateSuppressionSeconds (120s) of the previous valid non-duplicate event
            // AND no distinct open schedule boundary requires a separate punch at this timestamp
            if (i > 0)
            {
                var prevEvent = result.Events.LastOrDefault(e => !e.IsDuplicateSuppressed);
                if (prevEvent is not null)
                {
                    var secondsSincePrev = (clockTime - prevEvent.LogDateTime).TotalSeconds;
                    if (secondsSincePrev >= 0 && secondsSincePrev <= AttendanceReconciliationPolicy.DefaultDuplicateSuppressionSeconds)
                    {
                        bool hasUnstartedCoincidentDuty = dutyObligations.Any(d =>
                            !d.StartLogId.HasValue &&
                            IsWithinCheckInWindow(clockTime, d));

                        if (!hasUnstartedCoincidentDuty)
                        {
                            MarkDuplicateSuppressed(
                                eventDto,
                                workPlan,
                                "DuplicateSuppression",
                                $"Suppressed duplicate clock tap {secondsSincePrev:0}s after event #{prevEvent.AttendanceLogId} ({prevEvent.LogDateTime:HH:mm:ss}).");
                            result.DuplicateSuppressedCount++;
                            result.Events.Add(eventDto);
                            continue;
                        }
                    }
                }
            }

            // Step 2: Evaluate candidate obligations for this timestamp
            var remainingLogs = rawLogs.Skip(i + 1).ToList();
            ResolveSingleEventAssociations(
                log,
                clockTime,
                explicitDir,
                remainingLogs,
                primaryOfficial,
                breakObligations,
                dutyObligations,
                workPlan,
                eventDto,
                result);

            result.Events.Add(eventDto);
        }
    }

    private static void ResolveSingleEventAssociations(
        AttendanceLog log,
        DateTime clockTime,
        AttendanceClockType? explicitDir,
        List<AttendanceLog> futureLogs,
        ObligationRuntime? official,
        List<ObligationRuntime> breaks,
        List<ObligationRuntime> duties,
        WorkPlan? workPlan,
        AttendanceEventInterpretationDto eventDto,
        AttendanceReconciliationResult result)
    {
        var associations = new List<AttendanceEventAssociationDto>();

        // Check candidate states
        bool officialNeedsCheckIn = official is not null && (!official.StartLogId.HasValue ||
            // If an earlier punch was very early and this punch is much closer to ScheduledStart before ScheduledStart + GraceAfter
            (official.ActualStart.HasValue &&
             !duties.Any(d => !d.StartLogId.HasValue && IsWithinCheckInWindow(clockTime, d)) &&
             clockTime <= official.ScheduledStart.AddMinutes(official.CheckInGraceAfterMinutes) &&
             Math.Abs((clockTime - official.ScheduledStart).TotalMinutes) < Math.Abs((official.ActualStart.Value - official.ScheduledStart).TotalMinutes)));

        bool inOfficialCheckInWindow = official is not null &&
            clockTime >= official.ScheduledStart.AddMinutes(-Math.Max(official.CheckInGraceBeforeMinutes, 180)) &&
            clockTime <= official.ScheduledStart.AddMinutes(Math.Max(official.CheckInGraceAfterMinutes, 120));

        bool inOfficialCheckOutWindow = official is not null &&
            clockTime >= official.ScheduledEnd.AddMinutes(-official.CheckOutGraceBeforeMinutes) &&
            clockTime <= official.ScheduledEnd.AddMinutes(Math.Max(official.CheckOutGraceAfterMinutes, 180));

        // Active break candidate covering clockTime
        var activeBreak = breaks.FirstOrDefault(b =>
            clockTime >= b.ScheduledStart.AddMinutes(-b.CheckInGraceBeforeMinutes) &&
            clockTime <= b.ScheduledEnd.AddMinutes(b.CheckOutGraceAfterMinutes));

        // Open duty needing check-out around clockTime (including delayed duty exit up to 60 mins after ScheduledEnd)
        var openDutyForExit = duties
            .Where(d => d.StartLogId.HasValue && !d.EndLogId.HasValue &&
                        clockTime > d.ActualStart!.Value &&
                        clockTime >= d.ScheduledEnd.AddMinutes(-d.CheckOutGraceBeforeMinutes) &&
                        clockTime <= d.ScheduledEnd.AddMinutes(Math.Max(d.CheckOutGraceAfterMinutes, 60)))
            .OrderBy(d => Math.Abs((clockTime - d.ScheduledEnd).TotalMinutes))
            .FirstOrDefault();

        // Unstarted duty needing check-in around clockTime
        var dutyForEntry = duties
            .Where(d => !d.StartLogId.HasValue && IsWithinCheckInWindow(clockTime, d))
            .OrderBy(d => Math.Abs((clockTime - d.ScheduledStart).TotalMinutes))
            .ThenBy(d => d.Priority)
            .FirstOrDefault();

        // Check if multiple distinct unstarted duties have identical window at clockTime (ambiguous conflict)
        var competingDutiesForEntry = duties
            .Where(d => !d.StartLogId.HasValue &&
                        clockTime >= d.ScheduledStart.AddMinutes(-30) &&
                        clockTime <= d.ScheduledStart.AddMinutes(30))
            .ToList();

        // =====================================================================
        // CASE 1: Explicit BreakOut / BreakIn OR Mid-day Official Lunch + Delayed Duty Exit
        // =====================================================================
        if (activeBreak is not null && official is not null && official.StartLogId.HasValue && !official.EndLogId.HasValue)
        {
            bool wantsBreakOut = !activeBreak.BreakOutLogId.HasValue &&
                                 explicitDir != AttendanceClockType.BreakIn &&
                                 explicitDir != AttendanceClockType.CheckIn;
            bool wantsBreakIn = activeBreak.BreakOutLogId.HasValue &&
                                !activeBreak.BreakInLogId.HasValue &&
                                explicitDir != AttendanceClockType.BreakOut &&
                                explicitDir != AttendanceClockType.CheckOut;

            if (wantsBreakOut && openDutyForExit is not null)
            {
                // Shared event: Official BreakOut (Primary) + Delayed/Coincident Duty CheckOut (Secondary)
                activeBreak.BreakOutLogId = log.AttendanceLogId;
                activeBreak.BreakOutTime = clockTime;
                official.BreakOutLogId = log.AttendanceLogId;
                official.BreakOutTime = clockTime;
                official.State = AttendanceObligationState.OnBreak;

                openDutyForExit.EndLogId = log.AttendanceLogId;
                openDutyForExit.ActualEnd = clockTime;
                openDutyForExit.SharesEndEvent = true;
                openDutyForExit.State = AttendanceObligationState.Completed;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    official,
                    activeBreak.WorkPlanSegmentId ?? official.CheckOutSegmentId,
                    activeBreak.ScheduleName,
                    AttendanceClockType.BreakOut,
                    "OfficialBreakOut",
                    isPrimary: true,
                    matchMethod: "SharedDelayedDutyExitAndLunch",
                    matchReason: $"Official lunch break out at {clockTime:HH:mm} (shared with {openDutyForExit.ScheduleName} check-out).",
                    confidence: "PolicyShared"));

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    openDutyForExit,
                    openDutyForExit.CheckOutSegmentId,
                    openDutyForExit.CheckOutSegmentName,
                    AttendanceClockType.CheckOut,
                    "DutyCheckOut",
                    isPrimary: false,
                    matchMethod: "SharedDelayedDutyExitAndLunch",
                    matchReason: $"Closes {openDutyForExit.ScheduleName} ({openDutyForExit.ScheduledStart:HH:mm}-{openDutyForExit.ScheduledEnd:HH:mm}) at {clockTime:HH:mm}; credited duration remains clamped to scheduled end {openDutyForExit.ScheduledEnd:HH:mm}.",
                    confidence: "PolicyShared"));

                FinalizeEventInterpretation(eventDto, associations, result);
                return;
            }

            if (wantsBreakOut && openDutyForExit is null && dutyForEntry is null && !inOfficialCheckOutWindow)
            {
                activeBreak.BreakOutLogId = log.AttendanceLogId;
                activeBreak.BreakOutTime = clockTime;
                official.BreakOutLogId = log.AttendanceLogId;
                official.BreakOutTime = clockTime;
                official.State = AttendanceObligationState.OnBreak;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    official,
                    activeBreak.WorkPlanSegmentId ?? official.CheckInSegmentId,
                    activeBreak.ScheduleName,
                    AttendanceClockType.BreakOut,
                    "OfficialBreakOut",
                    isPrimary: true,
                    matchMethod: explicitDir == AttendanceClockType.BreakOut ? "DeviceDirection" : "OfficialBreakWindow",
                    matchReason: $"Official break out recorded within {activeBreak.ScheduleName} window ({activeBreak.ScheduledStart:HH:mm}-{activeBreak.ScheduledEnd:HH:mm}).",
                    confidence: "Deterministic"));

                FinalizeEventInterpretation(eventDto, associations, result);
                return;
            }

            if (wantsBreakIn && !inOfficialCheckOutWindow)
            {
                activeBreak.BreakInLogId = log.AttendanceLogId;
                activeBreak.BreakInTime = clockTime;
                official.BreakInLogId = log.AttendanceLogId;
                official.BreakInTime = clockTime;
                official.State = AttendanceObligationState.ClockedIn;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    official,
                    activeBreak.WorkPlanSegmentId ?? official.CheckInSegmentId,
                    activeBreak.ScheduleName,
                    AttendanceClockType.BreakIn,
                    "OfficialBreakIn",
                    isPrimary: true,
                    matchMethod: explicitDir == AttendanceClockType.BreakIn ? "DeviceDirection" : "OfficialBreakWindow",
                    matchReason: $"Official break return recorded at {clockTime:HH:mm}.",
                    confidence: "Deterministic"));

                // Also check if a post-lunch duty starts at the same time
                if (dutyForEntry is not null && Math.Abs((clockTime - dutyForEntry.ScheduledStart).TotalMinutes) <= 30)
                {
                    bool hasDedicatedFutureDutyIn = futureLogs.Any(fl =>
                        fl.Date > clockTime &&
                        fl.Date <= dutyForEntry.ScheduledStart.AddMinutes(dutyForEntry.CheckInGraceAfterMinutes) &&
                        Math.Abs((fl.Date - dutyForEntry.ScheduledStart).TotalMinutes) <= 30);

                    if (!hasDedicatedFutureDutyIn)
                    {
                        dutyForEntry.StartLogId = log.AttendanceLogId;
                        dutyForEntry.ActualStart = clockTime;
                        dutyForEntry.SharesStartEvent = true;
                        dutyForEntry.State = AttendanceObligationState.ClockedIn;

                        associations.Add(CreateAssociation(
                            log.AttendanceLogId,
                            dutyForEntry,
                            dutyForEntry.CheckInSegmentId,
                            dutyForEntry.CheckInSegmentName,
                            AttendanceClockType.CheckIn,
                            "DutyCheckIn",
                            isPrimary: false,
                            matchMethod: "SharedCoincidentStart",
                            matchReason: $"Shared break return and {dutyForEntry.ScheduleName} check-in at {clockTime:HH:mm}.",
                            confidence: "PolicyShared"));
                    }
                }

                FinalizeEventInterpretation(eventDto, associations, result);
                return;
            }
        }

        // =====================================================================
        // CASE 2: Official Check-In (Priority 1) + Optional Coincident Duty Check-In / Pre-Official Duty Check-Out
        // =====================================================================
        bool isStrictlyPreOfficialDutyCheckIn =
            official is not null &&
            dutyForEntry is not null &&
            dutyForEntry.ScheduledStart < official.ScheduledStart.AddMinutes(-15) &&
            Math.Abs((clockTime - dutyForEntry.ScheduledStart).TotalMinutes) < Math.Abs((clockTime - official.ScheduledStart).TotalMinutes);

        bool isStrictlyPostOfficialDutyEvent =
            official is not null &&
            clockTime > official.ScheduledEnd.AddMinutes(15) &&
            ((dutyForEntry is not null && Math.Abs((clockTime - dutyForEntry.ScheduledStart).TotalMinutes) < Math.Abs((clockTime - official.ScheduledEnd).TotalMinutes)) ||
             (openDutyForExit is not null && openDutyForExit.ScheduledEnd > official.ScheduledEnd) ||
             duties.Any(d => d.ScheduledStart > official.ScheduledEnd && Math.Abs((clockTime - d.ScheduledEnd).TotalMinutes) <= 45));

        if (official is not null && officialNeedsCheckIn && inOfficialCheckInWindow && !isStrictlyPreOfficialDutyCheckIn && !isStrictlyPostOfficialDutyEvent && explicitDir != AttendanceClockType.CheckOut)
        {
            official.StartLogId = log.AttendanceLogId;
            official.ActualStart = clockTime;
            official.State = AttendanceObligationState.ClockedIn;

            associations.Add(CreateAssociation(
                log.AttendanceLogId,
                official,
                official.CheckInSegmentId,
                official.CheckInSegmentName,
                AttendanceClockType.CheckIn,
                "OfficialCheckIn",
                isPrimary: true,
                matchMethod: explicitDir == AttendanceClockType.CheckIn ? "DeviceDirection" : "OfficialBoundaryWindow",
                matchReason: $"Primary official check-in for {official.ScheduleName} ({official.ScheduledStart:HH:mm}-{official.ScheduledEnd:HH:mm}).",
                confidence: "Deterministic"));

            // If a pre-official duty ends at/near the official start time (e.g., 06:00-08:00 duty ending at 08:00), share the 08:00 event as DutyCheckOut!
            if (openDutyForExit is not null &&
                Math.Abs((openDutyForExit.ScheduledEnd - official.ScheduledStart).TotalMinutes) <= 30)
            {
                openDutyForExit.EndLogId = log.AttendanceLogId;
                openDutyForExit.ActualEnd = clockTime;
                openDutyForExit.SharesEndEvent = true;
                openDutyForExit.State = AttendanceObligationState.Completed;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    openDutyForExit,
                    openDutyForExit.CheckOutSegmentId,
                    openDutyForExit.CheckOutSegmentName,
                    AttendanceClockType.CheckOut,
                    "DutyCheckOut",
                    isPrimary: false,
                    matchMethod: "SharedAdjacentBoundary",
                    matchReason: $"Shared boundary closing {openDutyForExit.ScheduleName} ({openDutyForExit.ScheduledStart:HH:mm}-{openDutyForExit.ScheduledEnd:HH:mm}) at official start.",
                    confidence: "PolicyShared"));
            }

            // Scenario B: Does an approved Duty also start at/near the same time?
            // Share the check-in event ONLY if there is no separate dedicated duty check-in punch in futureLogs within the duty's check-in window!
            if (dutyForEntry is not null &&
                Math.Abs((dutyForEntry.ScheduledStart - official.ScheduledStart).TotalMinutes) <= 30)
            {
                bool hasSecondDedicatedCheckInPunch = futureLogs.Any(fl =>
                    fl.Date > clockTime &&
                    fl.Date <= dutyForEntry.ScheduledStart.AddMinutes(Math.Min(dutyForEntry.CheckInGraceAfterMinutes, 45)) &&
                    (dutyForEntry.ScheduledEnd - fl.Date).TotalMinutes > 15);

                if (!hasSecondDedicatedCheckInPunch)
                {
                    dutyForEntry.StartLogId = log.AttendanceLogId;
                    dutyForEntry.ActualStart = clockTime;
                    dutyForEntry.SharesStartEvent = true;
                    dutyForEntry.State = AttendanceObligationState.ClockedIn;

                    associations.Add(CreateAssociation(
                        log.AttendanceLogId,
                        dutyForEntry,
                        dutyForEntry.CheckInSegmentId,
                        dutyForEntry.CheckInSegmentName,
                        AttendanceClockType.CheckIn,
                        "DutyCheckIn",
                        isPrimary: false,
                        matchMethod: "SharedCoincidentStart",
                        matchReason: $"Shared coincident check-in for {dutyForEntry.ScheduleName} ({dutyForEntry.ScheduledStart:HH:mm}-{dutyForEntry.ScheduledEnd:HH:mm}); official attendance retains primary classification.",
                        confidence: "PolicyShared"));
                }
            }

            FinalizeEventInterpretation(eventDto, associations, result);
            return;
        }

        // =====================================================================
        // CASE 3: Official Check-Out (Priority 1) + Optional Coincident Duty Check-Out / Post-Official Duty Check-In
        // =====================================================================
        if (official is not null && !official.EndLogId.HasValue && inOfficialCheckOutWindow && !isStrictlyPostOfficialDutyEvent &&
            (official.StartLogId.HasValue || explicitDir == AttendanceClockType.CheckOut || clockTime >= official.ScheduledEnd.AddMinutes(-official.CheckOutGraceBeforeMinutes)))
        {
            // Ensure we don't steal an earlier duty check-in if clockTime is strictly a duty start before official end
            bool isStrictlyDutyStartBeforeOfficialEnd =
                dutyForEntry is not null &&
                clockTime < official.ScheduledEnd.AddMinutes(-15) &&
                Math.Abs((clockTime - dutyForEntry.ScheduledStart).TotalMinutes) <= 15;

            if (!isStrictlyDutyStartBeforeOfficialEnd && explicitDir != AttendanceClockType.CheckIn)
            {
                official.EndLogId = log.AttendanceLogId;
                official.ActualEnd = clockTime;
                official.State = official.StartLogId.HasValue
                    ? AttendanceObligationState.Completed
                    : AttendanceObligationState.MissingClockIn;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    official,
                    official.CheckOutSegmentId,
                    official.CheckOutSegmentName,
                    AttendanceClockType.CheckOut,
                    "OfficialCheckOut",
                    isPrimary: true,
                    matchMethod: explicitDir == AttendanceClockType.CheckOut ? "DeviceDirection" : "OfficialBoundaryWindow",
                    matchReason: $"Primary official check-out for {official.ScheduleName} ({official.ScheduledStart:HH:mm}-{official.ScheduledEnd:HH:mm}).",
                    confidence: "Deterministic"));

                // Scenario C: If a duty also ends at or near the official end time, share the check-out!
                var coincidentDuties = duties
                    .Where(d => !d.EndLogId.HasValue &&
                                Math.Abs((d.ScheduledEnd - official.ScheduledEnd).TotalMinutes) <= 30 &&
                                clockTime >= d.ScheduledEnd.AddMinutes(-d.CheckOutGraceBeforeMinutes) &&
                                clockTime <= d.ScheduledEnd.AddMinutes(d.CheckOutGraceAfterMinutes))
                    .ToList();

                foreach (var coDuty in coincidentDuties)
                {
                    coDuty.EndLogId = log.AttendanceLogId;
                    coDuty.ActualEnd = clockTime;
                    coDuty.SharesEndEvent = true;
                    coDuty.State = coDuty.StartLogId.HasValue
                        ? AttendanceObligationState.Completed
                        : AttendanceObligationState.MissingClockIn;

                    associations.Add(CreateAssociation(
                        log.AttendanceLogId,
                        coDuty,
                        coDuty.CheckOutSegmentId,
                        coDuty.CheckOutSegmentName,
                        AttendanceClockType.CheckOut,
                        "DutyCheckOut",
                        isPrimary: false,
                        matchMethod: "SharedCoincidentEnd",
                        matchReason: $"Shared coincident check-out for {coDuty.ScheduleName} ({coDuty.ScheduledStart:HH:mm}-{coDuty.ScheduledEnd:HH:mm}).",
                        confidence: "PolicyShared"));
                }

                // Adjacent post-official duty starting at official end (e.g., 14:00-16:00 duty starting at 14:00)
                if (dutyForEntry is not null &&
                    Math.Abs((dutyForEntry.ScheduledStart - official.ScheduledEnd).TotalMinutes) <= 30)
                {
                    bool hasSeparatePostDutyInPunch = futureLogs.Any(fl =>
                        fl.Date > clockTime &&
                        fl.Date <= dutyForEntry.ScheduledStart.AddMinutes(30) &&
                        (dutyForEntry.ScheduledEnd - fl.Date).TotalMinutes > 15);

                    if (!hasSeparatePostDutyInPunch)
                    {
                        dutyForEntry.StartLogId = log.AttendanceLogId;
                        dutyForEntry.ActualStart = clockTime;
                        dutyForEntry.SharesStartEvent = true;
                        dutyForEntry.State = AttendanceObligationState.ClockedIn;

                        associations.Add(CreateAssociation(
                            log.AttendanceLogId,
                            dutyForEntry,
                            dutyForEntry.CheckInSegmentId,
                            dutyForEntry.CheckInSegmentName,
                            AttendanceClockType.CheckIn,
                            "DutyCheckIn",
                            isPrimary: false,
                            matchMethod: "SharedAdjacentBoundary",
                            matchReason: $"Shared boundary starting {dutyForEntry.ScheduleName} ({dutyForEntry.ScheduledStart:HH:mm}-{dutyForEntry.ScheduledEnd:HH:mm}) at official end.",
                            confidence: "PolicyShared"));
                    }
                }

                FinalizeEventInterpretation(eventDto, associations, result);
                return;
            }
        }

        // =====================================================================
        // CASE 4: Consecutive Duty Boundary (Duty A Check-Out + Duty B Check-In)
        // =====================================================================
        if (openDutyForExit is not null && dutyForEntry is not null && openDutyForExit != dutyForEntry &&
            Math.Abs((openDutyForExit.ScheduledEnd - dutyForEntry.ScheduledStart).TotalMinutes) <= 30)
        {
            openDutyForExit.EndLogId = log.AttendanceLogId;
            openDutyForExit.ActualEnd = clockTime;
            openDutyForExit.State = AttendanceObligationState.Completed;

            associations.Add(CreateAssociation(
                log.AttendanceLogId,
                openDutyForExit,
                openDutyForExit.CheckOutSegmentId,
                openDutyForExit.CheckOutSegmentName,
                AttendanceClockType.CheckOut,
                "DutyCheckOut",
                isPrimary: true,
                matchMethod: "DutyBoundaryWindow",
                matchReason: $"Closes {openDutyForExit.ScheduleName} ({openDutyForExit.ScheduledStart:HH:mm}-{openDutyForExit.ScheduledEnd:HH:mm}); official attendance remains open.",
                confidence: "Deterministic"));

            // Share with consecutive Duty B start if no separate punch follows immediately for Duty B
            bool hasSeparateNextDutyPunch = futureLogs.Any(fl =>
                fl.Date > clockTime &&
                fl.Date <= dutyForEntry.ScheduledStart.AddMinutes(30));

            if (!hasSeparateNextDutyPunch)
            {
                dutyForEntry.StartLogId = log.AttendanceLogId;
                dutyForEntry.ActualStart = clockTime;
                dutyForEntry.SharesStartEvent = true;
                dutyForEntry.State = AttendanceObligationState.ClockedIn;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    dutyForEntry,
                    dutyForEntry.CheckInSegmentId,
                    dutyForEntry.CheckInSegmentName,
                    AttendanceClockType.CheckIn,
                    "DutyCheckIn",
                    isPrimary: false,
                    matchMethod: "SharedConsecutiveDutyBoundary",
                    matchReason: $"Shared consecutive transition into {dutyForEntry.ScheduleName} ({dutyForEntry.ScheduledStart:HH:mm}-{dutyForEntry.ScheduledEnd:HH:mm}).",
                    confidence: "PolicyShared"));
            }

            FinalizeEventInterpretation(eventDto, associations, result);
            return;
        }

        // =====================================================================
        // CASE 5: Open Duty Check-Out (including delayed duty exit at e.g. 12:30 for 10:00-12:00 duty)
        // Never terminates or replaces Official attendance!
        // =====================================================================
        if (openDutyForExit is not null && explicitDir != AttendanceClockType.CheckIn)
        {
            openDutyForExit.EndLogId = log.AttendanceLogId;
            openDutyForExit.ActualEnd = clockTime;
            openDutyForExit.State = AttendanceObligationState.Completed;

            var delayedMinutes = Math.Max(0, (int)Math.Round((clockTime - openDutyForExit.ScheduledEnd).TotalMinutes));
            var reason = delayedMinutes > 0
                ? $"Delayed duty check-out ({delayedMinutes}m after {openDutyForExit.ScheduledEnd:HH:mm}) for {openDutyForExit.ScheduleName}. Credited duty duration remains clamped to scheduled end {openDutyForExit.ScheduledEnd:HH:mm}; official attendance continues."
                : $"Duty check-out for {openDutyForExit.ScheduleName} ({openDutyForExit.ScheduledStart:HH:mm}-{openDutyForExit.ScheduledEnd:HH:mm}); official attendance continues.";

            associations.Add(CreateAssociation(
                log.AttendanceLogId,
                openDutyForExit,
                openDutyForExit.CheckOutSegmentId,
                openDutyForExit.CheckOutSegmentName,
                AttendanceClockType.CheckOut,
                "DutyCheckOut",
                isPrimary: true,
                matchMethod: explicitDir == AttendanceClockType.CheckOut ? "DeviceDirection" : "DutyBoundaryWindow",
                matchReason: reason,
                confidence: "Deterministic"));

            FinalizeEventInterpretation(eventDto, associations, result);
            return;
        }

        // =====================================================================
        // CASE 6: Duty Check-In (within or outside official hours)
        // Never ends or interrupts Official attendance!
        // =====================================================================
        if (dutyForEntry is not null && explicitDir != AttendanceClockType.CheckOut)
        {
            if (competingDutiesForEntry.Count > 1 &&
                competingDutiesForEntry.All(d => d.ScheduledStart == dutyForEntry.ScheduledStart && d.ScheduledEnd == dutyForEntry.ScheduledEnd))
            {
                // Ambiguous identical competing duties
                dutyForEntry.State = AttendanceObligationState.Conflict;
                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    dutyForEntry,
                    dutyForEntry.CheckInSegmentId,
                    dutyForEntry.CheckInSegmentName,
                    AttendanceClockType.CheckIn,
                    "AmbiguousDutyCheckIn",
                    isPrimary: true,
                    matchMethod: "AmbiguousConflict",
                    matchReason: $"Ambiguous check-in at {clockTime:HH:mm}: multiple overlapping duties ({string.Join(", ", competingDutiesForEntry.Select(d => d.ScheduleName))}) share the same boundary.",
                    confidence: "Conflict"));

                result.ConflictCount++;
                FinalizeEventInterpretation(eventDto, associations, result);
                return;
            }

            dutyForEntry.StartLogId = log.AttendanceLogId;
            dutyForEntry.ActualStart = clockTime;
            dutyForEntry.State = AttendanceObligationState.ClockedIn;

            associations.Add(CreateAssociation(
                log.AttendanceLogId,
                dutyForEntry,
                dutyForEntry.CheckInSegmentId,
                dutyForEntry.CheckInSegmentName,
                AttendanceClockType.CheckIn,
                "DutyCheckIn",
                isPrimary: true,
                matchMethod: explicitDir == AttendanceClockType.CheckIn ? "DeviceDirection" : "DutyBoundaryWindow",
                matchReason: $"Duty check-in for {dutyForEntry.ScheduleName} ({dutyForEntry.ScheduledStart:HH:mm}-{dutyForEntry.ScheduledEnd:HH:mm}); official attendance remains active.",
                confidence: "Deterministic"));

            FinalizeEventInterpretation(eventDto, associations, result);
            return;
        }

        // =====================================================================
        // CASE 7: Duty Check-Out when Duty Check-In was missed (Missing Duty Check-In)
        // =====================================================================
        var unstartedDutyForExit = duties
            .Where(d => !d.StartLogId.HasValue && !d.EndLogId.HasValue &&
                        clockTime >= d.ScheduledEnd.AddMinutes(-d.CheckOutGraceBeforeMinutes) &&
                        clockTime <= d.ScheduledEnd.AddMinutes(d.CheckOutGraceAfterMinutes))
            .OrderBy(d => Math.Abs((clockTime - d.ScheduledEnd).TotalMinutes))
            .FirstOrDefault();

        if (unstartedDutyForExit is not null && explicitDir != AttendanceClockType.CheckIn)
        {
            unstartedDutyForExit.EndLogId = log.AttendanceLogId;
            unstartedDutyForExit.ActualEnd = clockTime;
            unstartedDutyForExit.State = AttendanceObligationState.MissingClockIn;

            associations.Add(CreateAssociation(
                log.AttendanceLogId,
                unstartedDutyForExit,
                unstartedDutyForExit.CheckOutSegmentId,
                unstartedDutyForExit.CheckOutSegmentName,
                AttendanceClockType.CheckOut,
                "DutyCheckOut",
                isPrimary: true,
                matchMethod: "DutyBoundaryWindow",
                matchReason: $"Duty check-out recorded at {clockTime:HH:mm} for {unstartedDutyForExit.ScheduleName}, but duty check-in at {unstartedDutyForExit.ScheduledStart:HH:mm} is missing.",
                confidence: "Deterministic"));

            FinalizeEventInterpretation(eventDto, associations, result);
            return;
        }

        // =====================================================================
        // CASE 8: Late Official Check-In or Early Official Check-Out ( only when no duty owns the window)
        // =====================================================================
        if (official is not null)
        {
            if (!official.StartLogId.HasValue && clockTime < official.ScheduledEnd && explicitDir != AttendanceClockType.CheckOut)
            {
                official.StartLogId = log.AttendanceLogId;
                official.ActualStart = clockTime;
                official.State = AttendanceObligationState.ClockedIn;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    official,
                    official.CheckInSegmentId,
                    official.CheckInSegmentName,
                    AttendanceClockType.CheckIn,
                    "OfficialCheckIn",
                    isPrimary: true,
                    matchMethod: explicitDir == AttendanceClockType.CheckIn ? "DeviceDirection" : "ChronologicalSequence",
                    matchReason: $"Late official check-in recorded at {clockTime:HH:mm} for {official.ScheduleName} ({official.ScheduledStart:HH:mm}-{official.ScheduledEnd:HH:mm}).",
                    confidence: "Deterministic"));

                FinalizeEventInterpretation(eventDto, associations, result);
                return;
            }

            // Explicit device CheckOut during official hours when no duty is open
            if (official.StartLogId.HasValue && !official.EndLogId.HasValue &&
                explicitDir == AttendanceClockType.CheckOut &&
                clockTime > official.ActualStart!.Value &&
                clockTime <= official.ScheduledEnd.AddMinutes(official.CheckOutGraceAfterMinutes) &&
                !duties.Any(d => d.StartLogId.HasValue && !d.EndLogId.HasValue))
            {
                official.EndLogId = log.AttendanceLogId;
                official.ActualEnd = clockTime;
                official.State = AttendanceObligationState.Completed;

                associations.Add(CreateAssociation(
                    log.AttendanceLogId,
                    official,
                    official.CheckOutSegmentId,
                    official.CheckOutSegmentName,
                    AttendanceClockType.CheckOut,
                    "OfficialCheckOut",
                    isPrimary: true,
                    matchMethod: "DeviceDirection",
                    matchReason: $"Explicit device official check-out at {clockTime:HH:mm} before scheduled end {official.ScheduledEnd:HH:mm}.",
                    confidence: "Deterministic"));

                FinalizeEventInterpretation(eventDto, associations, result);
                return;
            }
        }

        // =====================================================================
        // CASE 9: Unresolved / Ambiguous Conflict (Never invent a false completion!)
        // =====================================================================
        result.UnresolvedEventCount++;
        result.ConflictCount++;

        associations.Add(new AttendanceEventAssociationDto
        {
            AttendanceLogId = log.AttendanceLogId,
            WorkPlanId = workPlan?.WorkPlanId,
            SchedulePurpose = "Unresolved",
            ScheduleName = "Unresolved / Review Required",
            SegmentName = "Unresolved Event",
            ClockType = explicitDir ?? AttendanceClockType.Unresolved,
            LogicalEventType = "Ambiguous",
            IsPrimaryClassification = true,
            MatchMethod = "ManualReviewRequired",
            MatchReason = $"Clock event at {clockTime:HH:mm} falls outside configured check-in/check-out/break windows and cannot be safely inferred without manual review.",
            ConfidenceState = "Conflict",
            PolicyVersion = AttendanceReconciliationPolicy.CurrentVersion
        });

        eventDto.Associations = associations;
        eventDto.PrimaryPurpose = "Unresolved";
        eventDto.PrimaryEventLabel = "Conflict / Review Required";
        eventDto.ConfidenceState = "Conflict";
        eventDto.SummaryReason = associations[0].MatchReason;
    }

    private static bool IsWithinCheckInWindow(DateTime clockTime, ObligationRuntime obligation) =>
        clockTime >= obligation.ScheduledStart.AddMinutes(-obligation.CheckInGraceBeforeMinutes) &&
        clockTime <= obligation.ScheduledStart.AddMinutes(obligation.CheckInGraceAfterMinutes) &&
        clockTime < obligation.ScheduledEnd;

    private static AttendanceEventAssociationDto CreateAssociation(
        int attendanceLogId,
        ObligationRuntime obligation,
        int? segmentId,
        string segmentName,
        AttendanceClockType clockType,
        string logicalEventType,
        bool isPrimary,
        string matchMethod,
        string matchReason,
        string confidence)
    {
        int lateMinutes = 0;
        int earlyMinutes = 0;
        if (clockType == AttendanceClockType.CheckIn && obligation.ActualStart.HasValue)
        {
            lateMinutes = AttendanceTiming.CalculateLateMinutes(
                obligation.ScheduledStart,
                obligation.ActualStart.Value,
                obligation.ObligationType == "Duty" ? obligation.CheckInGraceAfterMinutes : 0);
        }
        else if (clockType == AttendanceClockType.CheckOut && obligation.ActualEnd.HasValue)
        {
            earlyMinutes = AttendanceTiming.CalculateEarlyDepartureMinutes(
                obligation.ScheduledEnd,
                obligation.ActualEnd.Value);
        }

        return new AttendanceEventAssociationDto
        {
            AttendanceLogId = attendanceLogId,
            WorkPlanId = obligation.WorkPlanId,
            WorkAssignmentId = obligation.WorkAssignmentId,
            WorkPlanSegmentId = segmentId ?? obligation.WorkPlanSegmentId,
            SchedulePurpose = obligation.ObligationType,
            ScheduleName = obligation.ScheduleName,
            SegmentName = segmentName,
            ClockType = clockType,
            LogicalEventType = logicalEventType,
            IsPrimaryClassification = isPrimary,
            MatchMethod = matchMethod,
            MatchReason = matchReason,
            ConfidenceState = confidence,
            PolicyVersion = AttendanceReconciliationPolicy.CurrentVersion,
            ScheduledStart = obligation.ScheduledStart,
            ScheduledEnd = obligation.ScheduledEnd,
            LateMinutes = lateMinutes,
            EarlyDepartureMinutes = earlyMinutes
        };
    }

    private static void FinalizeEventInterpretation(
        AttendanceEventInterpretationDto eventDto,
        List<AttendanceEventAssociationDto> associations,
        AttendanceReconciliationResult result)
    {
        // Enforce Official priority for IsPrimaryClassification whenever an Official association is present
        var officialAssoc = associations.FirstOrDefault(a => a.SchedulePurpose is "Official" or "OfficialBreak");
        if (officialAssoc is not null)
        {
            foreach (var a in associations)
                a.IsPrimaryClassification = ReferenceEquals(a, officialAssoc);
        }
        else if (associations.Count > 0 && !associations.Any(a => a.IsPrimaryClassification))
        {
            associations[0].IsPrimaryClassification = true;
        }

        var ordered = associations
            .OrderByDescending(a => a.IsPrimaryClassification)
            .ThenBy(a => a.SchedulePurpose == "Official" ? 0 : 1)
            .ToList();

        var primary = ordered[0];
        eventDto.Associations = ordered;
        eventDto.PrimaryPurpose = primary.SchedulePurpose;
        eventDto.PrimaryEventLabel = primary.LogicalEventType;
        eventDto.ConfidenceState = ordered.Any(a => a.ConfidenceState == "Conflict")
            ? "Conflict"
            : ordered.Count > 1
                ? "PolicyShared"
                : primary.ConfidenceState;
        eventDto.SummaryReason = string.Join(" | ", ordered.Select(a => a.MatchReason));

        result.ResolvedEventCount++;
        if (ordered.Count > 1)
            result.SharedEventCount++;
    }

    private static void MarkDuplicateSuppressed(
        AttendanceEventInterpretationDto eventDto,
        WorkPlan? workPlan,
        string method,
        string reason)
    {
        eventDto.IsDuplicateSuppressed = true;
        eventDto.PrimaryPurpose = "DuplicateIgnored";
        eventDto.PrimaryEventLabel = "Duplicate / Ignored";
        eventDto.ConfidenceState = "Deterministic";
        eventDto.SummaryReason = reason;
        eventDto.Associations.Add(new AttendanceEventAssociationDto
        {
            AttendanceLogId = eventDto.AttendanceLogId,
            WorkPlanId = workPlan?.WorkPlanId,
            SchedulePurpose = "DuplicateIgnored",
            ScheduleName = "Duplicate Suppression",
            SegmentName = "Duplicate Clock Tap",
            ClockType = AttendanceClockType.Unresolved,
            LogicalEventType = "DuplicateIgnored",
            IsPrimaryClassification = true,
            MatchMethod = method,
            MatchReason = reason,
            ConfidenceState = "Deterministic",
            PolicyVersion = AttendanceReconciliationPolicy.CurrentVersion
        });
    }

    private static void ApplyManualOverridesToEvent(
        AttendanceLog log,
        AttendanceEventInterpretationDto eventDto,
        List<AttendanceManualOverride> overrides,
        List<ObligationRuntime> officialObligations,
        List<ObligationRuntime> breakObligations,
        List<ObligationRuntime> dutyObligations,
        WorkPlan? workPlan)
    {
        var allObligations = officialObligations.Concat(breakObligations).Concat(dutyObligations).ToList();
        var associations = new List<AttendanceEventAssociationDto>();

        foreach (var ov in overrides)
        {
            var targetObligation = allObligations.FirstOrDefault(o =>
                (ov.WorkAssignmentId.HasValue && o.WorkAssignmentId == ov.WorkAssignmentId) ||
                string.Equals(o.ObligationType, ov.TargetSchedulePurpose, StringComparison.OrdinalIgnoreCase))
                ?? officialObligations.FirstOrDefault();

            var clockType = ov.TargetClockTypeId.HasValue
                ? (AttendanceClockType)ov.TargetClockTypeId.Value
                : AttendanceClockType.CheckIn;

            if (targetObligation is not null)
            {
                if (clockType == AttendanceClockType.CheckIn)
                {
                    targetObligation.StartLogId = log.AttendanceLogId;
                    targetObligation.ActualStart = log.Date;
                }
                else if (clockType == AttendanceClockType.CheckOut)
                {
                    targetObligation.EndLogId = log.AttendanceLogId;
                    targetObligation.ActualEnd = log.Date;
                }
                else if (clockType == AttendanceClockType.BreakOut)
                {
                    targetObligation.BreakOutLogId = log.AttendanceLogId;
                    targetObligation.BreakOutTime = log.Date;
                }
                else if (clockType == AttendanceClockType.BreakIn)
                {
                    targetObligation.BreakInLogId = log.AttendanceLogId;
                    targetObligation.BreakInTime = log.Date;
                }
            }

            bool isPrimary = !string.Equals(ov.OverrideAction, "AssignSecondary", StringComparison.OrdinalIgnoreCase) &&
                             associations.Count == 0;

            associations.Add(new AttendanceEventAssociationDto
            {
                AttendanceLogId = log.AttendanceLogId,
                WorkPlanId = ov.WorkPlanId ?? workPlan?.WorkPlanId,
                WorkAssignmentId = ov.WorkAssignmentId ?? targetObligation?.WorkAssignmentId,
                WorkPlanSegmentId = ov.WorkPlanSegmentId ?? targetObligation?.WorkPlanSegmentId,
                SchedulePurpose = ov.TargetSchedulePurpose,
                ScheduleName = targetObligation?.ScheduleName ?? ov.TargetSchedulePurpose,
                SegmentName = targetObligation?.CheckInSegmentName ?? ov.TargetSchedulePurpose,
                ClockType = clockType,
                LogicalEventType = $"{ov.TargetSchedulePurpose}{clockType}",
                IsPrimaryClassification = isPrimary,
                MatchMethod = "ManualOverride",
                MatchReason = $"Manual HR override: {ov.Reason}",
                ConfidenceState = "ManualOverride",
                PolicyVersion = AttendanceReconciliationPolicy.CurrentVersion,
                ScheduledStart = targetObligation?.ScheduledStart,
                ScheduledEnd = targetObligation?.ScheduledEnd
            });
        }

        eventDto.Associations = associations;
        eventDto.PrimaryPurpose = associations[0].SchedulePurpose;
        eventDto.PrimaryEventLabel = associations[0].LogicalEventType;
        eventDto.ConfidenceState = "ManualOverride";
        eventDto.SummaryReason = string.Join(" | ", associations.Select(a => a.MatchReason));
    }

    private static void EvaluateObligationsAndDurations(
        DateTime workDateTime,
        WorkPlan? workPlan,
        List<ObligationRuntime> officialObligations,
        List<ObligationRuntime> breakObligations,
        List<ObligationRuntime> dutyObligations,
        List<AttendanceManualOverride> manualOverrides,
        AttendanceReconciliationResult result)
    {
        var nowLocal = DateTime.UtcNow.AddHours(5);
        var actualIntervals = new List<(DateTime Start, DateTime End)>();
        var creditedOfficialIntervals = new List<(DateTime Start, DateTime End)>();
        var creditedDutyIntervals = new List<(DateTime Start, DateTime End)>();
        var plannedOfficialIntervals = officialObligations.Select(o => (o.ScheduledStart, o.ScheduledEnd)).ToList();
        var plannedDutyIntervals = dutyObligations.Select(o => (o.ScheduledStart, o.ScheduledEnd)).ToList();

        var coverage = WorkHourCoverage.Calculate(plannedOfficialIntervals, plannedDutyIntervals);
        result.DutyWithinOfficialMinutes = coverage.DutyWithinOfficialMinutes;
        result.DutyOutsideOfficialMinutes = coverage.DutyOutsideOfficialMinutes;

        // Evaluate Break Obligations first so Official knows break deductions
        int totalBreakMinutes = 0;
        int excessBreakMinutes = 0;
        int lateBreakReturnMinutes = 0;

        foreach (var brk in breakObligations)
        {
            var plannedBreakMins = Math.Max(0, (int)Math.Round((brk.ScheduledEnd - brk.ScheduledStart).TotalMinutes));
            int actualBreakMins = 0;

            if (brk.BreakOutTime.HasValue && brk.BreakInTime.HasValue && brk.BreakInTime.Value > brk.BreakOutTime.Value)
            {
                actualBreakMins = (int)Math.Round((brk.BreakInTime.Value - brk.BreakOutTime.Value).TotalMinutes);
                brk.State = AttendanceObligationState.Completed;
                brk.StatusReason = $"Break completed ({brk.BreakOutTime.Value:HH:mm}-{brk.BreakInTime.Value:HH:mm}).";
                if (brk.BreakInTime.Value > brk.ScheduledEnd)
                {
                    lateBreakReturnMinutes += (int)Math.Round((brk.BreakInTime.Value - brk.ScheduledEnd).TotalMinutes);
                }
                if (actualBreakMins > plannedBreakMins)
                {
                    excessBreakMinutes += actualBreakMins - plannedBreakMins;
                }
            }
            else if (brk.BreakOutTime.HasValue && !brk.BreakInTime.HasValue)
            {
                bool breakWindowElapsed = nowLocal > brk.ScheduledEnd.AddMinutes(brk.CheckOutGraceAfterMinutes) || workDateTime.Date < nowLocal.Date;
                brk.State = breakWindowElapsed ? AttendanceObligationState.MissingClockIn : AttendanceObligationState.OnBreak;
                brk.StatusReason = breakWindowElapsed
                    ? $"Break out recorded at {brk.BreakOutTime.Value:HH:mm}, but break return (Break In) is missing."
                    : $"Employee currently on break since {brk.BreakOutTime.Value:HH:mm}.";
                actualBreakMins = plannedBreakMins;

                if (breakWindowElapsed)
                {
                    result.Issues.Add(new AttendanceEvaluationIssueDto
                    {
                        WorkPlanId = workPlan?.WorkPlanId,
                        WorkPlanSegmentId = brk.WorkPlanSegmentId,
                        ExceptionType = AttendanceExceptionType.NoCheckIn,
                        Message = brk.StatusReason
                    });
                }
            }
            else
            {
                // Check if a duty overlapped the break window
                bool dutyCoveredBreak = dutyObligations.Any(d =>
                    d.ActualStart.HasValue && d.ActualEnd.HasValue &&
                    d.ActualStart.Value < brk.ScheduledEnd && d.ActualEnd.Value > brk.ScheduledStart);

                brk.State = dutyCoveredBreak ? AttendanceObligationState.Completed : AttendanceObligationState.NotStarted;
                brk.StatusReason = dutyCoveredBreak
                    ? "Break window overlapped by active duty assignment."
                    : "No explicit break punches recorded.";
            }

            totalBreakMinutes += actualBreakMins;

            result.Obligations.Add(new AttendanceObligationSummaryDto
            {
                WorkPlanId = brk.WorkPlanId,
                WorkAssignmentId = brk.WorkAssignmentId,
                WorkPlanSegmentId = brk.WorkPlanSegmentId,
                ObligationType = "OfficialBreak",
                ScheduleName = brk.ScheduleName,
                Priority = brk.Priority,
                ScheduledStart = brk.ScheduledStart,
                ScheduledEnd = brk.ScheduledEnd,
                GraceBeforeMinutes = brk.CheckInGraceBeforeMinutes,
                GraceAfterMinutes = brk.CheckOutGraceAfterMinutes,
                RequiresAttendance = brk.RequiresAttendance,
                RequiresCheckOut = brk.RequiresCheckOut,
                State = brk.State,
                ResolvedStartAttendanceLogId = brk.BreakOutLogId,
                ResolvedEndAttendanceLogId = brk.BreakInLogId,
                ActualStartDateTime = brk.BreakOutTime,
                ActualEndDateTime = brk.BreakInTime,
                BreakOutDateTime = brk.BreakOutTime,
                BreakInDateTime = brk.BreakInTime,
                BreakOutAttendanceLogId = brk.BreakOutLogId,
                BreakInAttendanceLogId = brk.BreakInLogId,
                PlannedMinutes = plannedBreakMins,
                ActualPresenceMinutes = actualBreakMins,
                CreditedWorkedMinutes = 0,
                LateMinutes = lateBreakReturnMinutes,
                StatusReason = brk.StatusReason
            });
        }

        result.OfficialBreakMinutes = totalBreakMinutes;
        result.ExcessBreakMinutes = excessBreakMinutes;
        result.LateBreakReturnMinutes = lateBreakReturnMinutes;

        // Evaluate Official Obligations (Priority 1)
        foreach (var off in officialObligations)
        {
            var plannedMins = Math.Max(0, (int)Math.Round((off.ScheduledEnd - off.ScheduledStart).TotalMinutes));
            result.PlannedOfficialMinutes += plannedMins;
            result.OfficialScheduledStart ??= off.ScheduledStart;
            result.OfficialScheduledEnd = off.ScheduledEnd;

            bool acknowledgedMissingOut = manualOverrides.Any(o =>
                string.Equals(o.OverrideAction, "AcknowledgeMissingEvent", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(o.TargetSchedulePurpose, "Official", StringComparison.OrdinalIgnoreCase));

            int actualMins = 0;
            int creditedMins = 0;
            int lateMins = 0;
            int earlyMins = 0;
            int overtimeMins = 0;

            if (off.ActualStart.HasValue)
            {
                lateMins = AttendanceTiming.CalculateLateMinutes(off.ScheduledStart, off.ActualStart.Value, 0);
            }

            if (off.ActualStart.HasValue && off.ActualEnd.HasValue && off.ActualEnd.Value > off.ActualStart.Value)
            {
                actualMins = (int)Math.Round((off.ActualEnd.Value - off.ActualStart.Value).TotalMinutes);
                actualIntervals.Add((off.ActualStart.Value, off.ActualEnd.Value));

                earlyMins = AttendanceTiming.CalculateEarlyDepartureMinutes(off.ScheduledEnd, off.ActualEnd.Value);

                // Credited official interval is clamped to [max(ScheduledStart, ActualStart), min(ScheduledEnd, ActualEnd)]
                // unless early arrival is within grace or overtime after ScheduledEnd is tracked separately
                var effectiveStart = off.ActualStart.Value <= off.ScheduledStart
                    ? off.ScheduledStart
                    : off.ActualStart.Value;
                var effectiveEnd = off.ActualEnd.Value >= off.ScheduledEnd
                    ? off.ScheduledEnd
                    : off.ActualEnd.Value;

                if (effectiveEnd > effectiveStart)
                {
                    creditedMins = Math.Max(0, (int)Math.Round((effectiveEnd - effectiveStart).TotalMinutes) - excessBreakMinutes);
                    creditedOfficialIntervals.Add((effectiveStart, effectiveEnd));
                }

                if (off.ActualEnd.Value > off.ScheduledEnd)
                {
                    overtimeMins = (int)Math.Round((off.ActualEnd.Value - off.ScheduledEnd).TotalMinutes);
                }

                off.State = AttendanceObligationState.Completed;
                off.StatusReason = $"Official attendance completed ({off.ActualStart.Value:HH:mm}-{off.ActualEnd.Value:HH:mm}).";
            }
            else if (off.ActualStart.HasValue && !off.ActualEnd.HasValue)
            {
                bool shiftEnded = nowLocal > off.ScheduledEnd.AddMinutes(off.CheckOutGraceAfterMinutes) || workDateTime.Date < nowLocal.Date;
                if (!off.RequiresCheckOut)
                {
                    creditedMins = Math.Max(0, plannedMins - lateMins);
                    actualMins = creditedMins;
                    off.State = AttendanceObligationState.Completed;
                    off.StatusReason = "Official check-in recorded; check-out not required by template.";
                    creditedOfficialIntervals.Add((off.ActualStart.Value, off.ScheduledEnd));
                }
                else if (acknowledgedMissingOut)
                {
                    off.State = AttendanceObligationState.PartiallyResolved;
                    off.StatusReason = "Missing official check-out acknowledged by HR override.";
                }
                else if (shiftEnded)
                {
                    off.State = AttendanceObligationState.MissingClockOut;
                    off.StatusReason = off.BreakOutTime.HasValue && !off.BreakInTime.HasValue
                        ? $"Official check-in at {off.ActualStart.Value:HH:mm} and break out at {off.BreakOutTime.Value:HH:mm}, but employee did not record break return or official check-out at {off.ScheduledEnd:HH:mm}."
                        : $"Official check-in recorded at {off.ActualStart.Value:HH:mm}, but official check-out at {off.ScheduledEnd:HH:mm} is missing.";

                    result.Issues.Add(new AttendanceEvaluationIssueDto
                    {
                        WorkPlanId = off.WorkPlanId,
                        WorkPlanSegmentId = off.CheckOutSegmentId ?? off.WorkPlanSegmentId,
                        ExceptionType = AttendanceExceptionType.NoCheckOut,
                        Message = off.StatusReason
                    });
                }
                else
                {
                    off.State = off.BreakOutTime.HasValue && !off.BreakInTime.HasValue
                        ? AttendanceObligationState.OnBreak
                        : AttendanceObligationState.ClockedIn;
                    off.StatusReason = off.State == AttendanceObligationState.OnBreak
                        ? $"Clocked in at {off.ActualStart.Value:HH:mm}; currently on break since {off.BreakOutTime!.Value:HH:mm}."
                        : $"Clocked in at {off.ActualStart.Value:HH:mm}; official shift in progress until {off.ScheduledEnd:HH:mm}.";
                }
            }
            else if (!off.ActualStart.HasValue && off.ActualEnd.HasValue)
            {
                off.State = AttendanceObligationState.MissingClockIn;
                off.StatusReason = $"Official check-out recorded at {off.ActualEnd.Value:HH:mm}, but official check-in at {off.ScheduledStart:HH:mm} is missing.";
                result.Issues.Add(new AttendanceEvaluationIssueDto
                {
                    WorkPlanId = off.WorkPlanId,
                    WorkPlanSegmentId = off.CheckInSegmentId ?? off.WorkPlanSegmentId,
                    ExceptionType = AttendanceExceptionType.NoCheckIn,
                    Message = off.StatusReason
                });
            }
            else
            {
                bool shiftStarted = nowLocal > off.ScheduledStart.AddMinutes(off.CheckInGraceAfterMinutes) || workDateTime.Date < nowLocal.Date;
                off.State = shiftStarted && off.RequiresAttendance
                    ? AttendanceObligationState.MissingClockIn
                    : AttendanceObligationState.NotStarted;
                off.StatusReason = shiftStarted && off.RequiresAttendance
                    ? $"No official attendance recorded for {off.ScheduleName} ({off.ScheduledStart:HH:mm}-{off.ScheduledEnd:HH:mm})."
                    : "Official schedule has not started.";

                if (shiftStarted && off.RequiresAttendance)
                {
                    result.Issues.Add(new AttendanceEvaluationIssueDto
                    {
                        WorkPlanId = off.WorkPlanId,
                        WorkPlanSegmentId = off.CheckInSegmentId ?? off.WorkPlanSegmentId,
                        ExceptionType = AttendanceExceptionType.MissingAttendance,
                        Message = off.StatusReason
                    });
                }
            }

            if (lateMins > 0)
            {
                result.Issues.Add(new AttendanceEvaluationIssueDto
                {
                    WorkPlanId = off.WorkPlanId,
                    WorkPlanSegmentId = off.CheckInSegmentId ?? off.WorkPlanSegmentId,
                    ExceptionType = AttendanceExceptionType.LateCheckIn,
                    Message = $"Official check-in was late by {lateMins} minute(s)."
                });
            }

            if (earlyMins > 0)
            {
                result.Issues.Add(new AttendanceEvaluationIssueDto
                {
                    WorkPlanId = off.WorkPlanId,
                    WorkPlanSegmentId = off.CheckOutSegmentId ?? off.WorkPlanSegmentId,
                    ExceptionType = AttendanceExceptionType.EarlyCheckOut,
                    Message = $"Official check-out was early by {earlyMins} minute(s)."
                });
            }

            result.OfficialState = off.State;
            result.OfficialCheckIn ??= off.ActualStart;
            result.OfficialCheckOut = off.ActualEnd ?? result.OfficialCheckOut;
            result.OfficialBreakOut ??= off.BreakOutTime;
            result.OfficialBreakIn ??= off.BreakInTime;
            result.OfficialCheckInLogId ??= off.StartLogId;
            result.OfficialCheckOutLogId = off.EndLogId ?? result.OfficialCheckOutLogId;
            result.OfficialBreakOutLogId ??= off.BreakOutLogId;
            result.OfficialBreakInLogId ??= off.BreakInLogId;
            result.ActualOfficialPresenceMinutes += actualMins;
            result.CreditedOfficialMinutes += creditedMins;
            result.OfficialLateMinutes += lateMins;
            result.OfficialEarlyDepartureMinutes += earlyMins;
            result.OvertimeCandidateMinutes += overtimeMins;

            result.Obligations.Insert(0, new AttendanceObligationSummaryDto
            {
                WorkPlanId = off.WorkPlanId,
                WorkAssignmentId = off.WorkAssignmentId,
                WorkPlanSegmentId = off.WorkPlanSegmentId,
                ObligationType = "Official",
                ScheduleName = off.ScheduleName,
                Priority = off.Priority,
                ScheduledStart = off.ScheduledStart,
                ScheduledEnd = off.ScheduledEnd,
                GraceBeforeMinutes = off.CheckInGraceBeforeMinutes,
                GraceAfterMinutes = off.CheckOutGraceAfterMinutes,
                RequiresAttendance = off.RequiresAttendance,
                RequiresCheckOut = off.RequiresCheckOut,
                State = off.State,
                ResolvedStartAttendanceLogId = off.StartLogId,
                ResolvedEndAttendanceLogId = off.EndLogId,
                ActualStartDateTime = off.ActualStart,
                ActualEndDateTime = off.ActualEnd,
                BreakOutDateTime = off.BreakOutTime,
                BreakInDateTime = off.BreakInTime,
                BreakOutAttendanceLogId = off.BreakOutLogId,
                BreakInAttendanceLogId = off.BreakInLogId,
                SharesStartEvent = off.SharesStartEvent,
                SharesEndEvent = off.SharesEndEvent,
                PlannedMinutes = plannedMins,
                ActualPresenceMinutes = actualMins,
                CreditedWorkedMinutes = creditedMins,
                BreakDeductedMinutes = excessBreakMinutes,
                LateMinutes = lateMins,
                EarlyDepartureMinutes = earlyMins,
                OvertimeCandidateMinutes = overtimeMins,
                StatusReason = off.StatusReason
            });
        }

        // Evaluate Duty Obligations Independently (Priority 10+)
        foreach (var duty in dutyObligations)
        {
            var plannedMins = Math.Max(0, (int)Math.Round((duty.ScheduledEnd - duty.ScheduledStart).TotalMinutes));
            result.PlannedDutyMinutes += plannedMins;

            var dutyCoverage = WorkHourCoverage.Calculate(plannedOfficialIntervals, [(duty.ScheduledStart, duty.ScheduledEnd)]);
            int actualMins = 0;
            int creditedMins = 0;
            int lateMins = 0;
            int earlyMins = 0;

            if (duty.ActualStart.HasValue)
            {
                lateMins = AttendanceTiming.CalculateLateMinutes(duty.ScheduledStart, duty.ActualStart.Value, duty.CheckInGraceAfterMinutes);
            }

            if (duty.ActualStart.HasValue && duty.ActualEnd.HasValue && duty.ActualEnd.Value > duty.ActualStart.Value)
            {
                actualMins = (int)Math.Round((duty.ActualEnd.Value - duty.ActualStart.Value).TotalMinutes);
                actualIntervals.Add((duty.ActualStart.Value, duty.ActualEnd.Value));

                earlyMins = AttendanceTiming.CalculateEarlyDepartureMinutes(duty.ScheduledEnd, duty.ActualEnd.Value);

                // CRITICAL RULE: Credited duty duration is clamped to [max(ScheduledStart, ActualStart), min(ScheduledEnd, ActualEnd)]
                // Do NOT extend scheduled duty to 12:30 simply because the physical clock event occurred at 12:30!
                var clampedStart = duty.ActualStart.Value < duty.ScheduledStart ? duty.ScheduledStart : duty.ActualStart.Value;
                var clampedEnd = duty.ActualEnd.Value > duty.ScheduledEnd ? duty.ScheduledEnd : duty.ActualEnd.Value;
                if (clampedEnd > clampedStart)
                {
                    creditedMins = (int)Math.Round((clampedEnd - clampedStart).TotalMinutes);
                    creditedDutyIntervals.Add((clampedStart, clampedEnd));
                }

                duty.State = AttendanceObligationState.Completed;
                duty.StatusReason = duty.ActualEnd.Value > duty.ScheduledEnd
                    ? $"Duty completed ({duty.ActualStart.Value:HH:mm}-{duty.ActualEnd.Value:HH:mm}); credited duration clamped to scheduled window {duty.ScheduledStart:HH:mm}-{duty.ScheduledEnd:HH:mm} ({creditedMins}m)."
                    : $"Duty completed ({duty.ActualStart.Value:HH:mm}-{duty.ActualEnd.Value:HH:mm}).";
            }
            else if (duty.ActualStart.HasValue && !duty.ActualEnd.HasValue)
            {
                bool dutyEnded = nowLocal > duty.ScheduledEnd.AddMinutes(duty.CheckOutGraceAfterMinutes) || workDateTime.Date < nowLocal.Date;
                if (!duty.RequiresCheckOut)
                {
                    creditedMins = Math.Max(0, plannedMins - lateMins);
                    actualMins = creditedMins;
                    duty.State = AttendanceObligationState.Completed;
                    duty.StatusReason = "Duty check-in recorded; check-out not required.";
                    creditedDutyIntervals.Add((duty.ScheduledStart, duty.ScheduledEnd));
                }
                else if (dutyEnded)
                {
                    duty.State = AttendanceObligationState.MissingClockOut;
                    duty.StatusReason = $"Duty check-in recorded at {duty.ActualStart.Value:HH:mm} for {duty.ScheduleName}, but duty check-out at {duty.ScheduledEnd:HH:mm} is missing.";
                    result.Issues.Add(new AttendanceEvaluationIssueDto
                    {
                        WorkPlanId = duty.WorkPlanId,
                        WorkPlanSegmentId = duty.CheckOutSegmentId ?? duty.WorkPlanSegmentId,
                        ExceptionType = AttendanceExceptionType.NoCheckOut,
                        Message = duty.StatusReason
                    });
                }
                else
                {
                    duty.State = AttendanceObligationState.ClockedIn;
                    duty.StatusReason = $"Duty in progress since {duty.ActualStart.Value:HH:mm}.";
                }
            }
            else if (!duty.ActualStart.HasValue && duty.ActualEnd.HasValue)
            {
                duty.State = AttendanceObligationState.MissingClockIn;
                duty.StatusReason = $"Duty check-out recorded at {duty.ActualEnd.Value:HH:mm} for {duty.ScheduleName}, but duty check-in at {duty.ScheduledStart:HH:mm} is missing.";
                result.Issues.Add(new AttendanceEvaluationIssueDto
                {
                    WorkPlanId = duty.WorkPlanId,
                    WorkPlanSegmentId = duty.CheckInSegmentId ?? duty.WorkPlanSegmentId,
                    ExceptionType = AttendanceExceptionType.NoCheckIn,
                    Message = duty.StatusReason
                });
            }
            else if (duty.State != AttendanceObligationState.Conflict)
            {
                bool dutyWindowPassed = nowLocal > duty.ScheduledEnd || workDateTime.Date < nowLocal.Date;
                duty.State = dutyWindowPassed && duty.RequiresAttendance
                    ? AttendanceObligationState.MissingClockIn
                    : AttendanceObligationState.NotStarted;
                duty.StatusReason = dutyWindowPassed && duty.RequiresAttendance
                    ? $"No clock events recorded for duty {duty.ScheduleName} ({duty.ScheduledStart:HH:mm}-{duty.ScheduledEnd:HH:mm})."
                    : "Duty has not started.";

                if (dutyWindowPassed && duty.RequiresAttendance)
                {
                    result.Issues.Add(new AttendanceEvaluationIssueDto
                    {
                        WorkPlanId = duty.WorkPlanId,
                        WorkPlanSegmentId = duty.CheckInSegmentId ?? duty.WorkPlanSegmentId,
                        ExceptionType = AttendanceExceptionType.MissingAttendance,
                        Message = duty.StatusReason
                    });
                }
            }

            result.ActualDutyPresenceMinutes += actualMins;
            result.CreditedDutyMinutes += creditedMins;

            result.Obligations.Add(new AttendanceObligationSummaryDto
            {
                WorkPlanId = duty.WorkPlanId,
                WorkAssignmentId = duty.WorkAssignmentId,
                WorkPlanSegmentId = duty.WorkPlanSegmentId,
                ObligationType = "Duty",
                ScheduleName = duty.ScheduleName,
                Priority = duty.Priority,
                ScheduledStart = duty.ScheduledStart,
                ScheduledEnd = duty.ScheduledEnd,
                GraceBeforeMinutes = duty.CheckInGraceBeforeMinutes,
                GraceAfterMinutes = duty.CheckOutGraceAfterMinutes,
                RequiresAttendance = duty.RequiresAttendance,
                RequiresCheckOut = duty.RequiresCheckOut,
                State = duty.State,
                ResolvedStartAttendanceLogId = duty.StartLogId,
                ResolvedEndAttendanceLogId = duty.EndLogId,
                ActualStartDateTime = duty.ActualStart,
                ActualEndDateTime = duty.ActualEnd,
                SharesStartEvent = duty.SharesStartEvent,
                SharesEndEvent = duty.SharesEndEvent,
                PlannedMinutes = plannedMins,
                ActualPresenceMinutes = actualMins,
                CreditedWorkedMinutes = creditedMins,
                LateMinutes = lateMins,
                EarlyDepartureMinutes = earlyMins,
                OverlapWithOfficialMinutes = dutyCoverage.DutyWithinOfficialMinutes,
                OutsideOfficialMinutes = dutyCoverage.DutyOutsideOfficialMinutes,
                StatusReason = duty.StatusReason
            });
        }

        // Compute non-double-counted interval unions
        var physicalCoverage = WorkHourCoverage.Calculate(actualIntervals, []);
        result.TotalPhysicalPresenceMinutes = Math.Max(0, physicalCoverage.OfficialMinutes - excessBreakMinutes);

        var creditedCoverage = WorkHourCoverage.Calculate(creditedOfficialIntervals, creditedDutyIntervals);
        result.TotalCreditedPayableMinutes = Math.Max(0, creditedCoverage.TotalDistinctScheduledMinutes - excessBreakMinutes);

        // Determine Daily Status from Official Priority + Duties
        if (officialObligations.Count > 0)
        {
            result.DailyStatus = result.OfficialState switch
            {
                AttendanceObligationState.Completed => AttendanceDailyStatus.Present,
                AttendanceObligationState.ClockedIn or AttendanceObligationState.OnBreak => AttendanceDailyStatus.InProgress,
                AttendanceObligationState.MissingClockOut or AttendanceObligationState.MissingClockIn or AttendanceObligationState.PartiallyResolved or AttendanceObligationState.Conflict => AttendanceDailyStatus.Incomplete,
                AttendanceObligationState.NotStarted when workDateTime.Date > nowLocal.Date => AttendanceDailyStatus.Future,
                _ => result.RawEventCount > 0 ? AttendanceDailyStatus.Incomplete : AttendanceDailyStatus.Absent
            };
            result.IsComplete = result.OfficialState == AttendanceObligationState.Completed &&
                                dutyObligations.All(d => !d.RequiresAttendance || d.State == AttendanceObligationState.Completed);
        }
        else if (dutyObligations.Count > 0)
        {
            bool allDutiesDone = dutyObligations.All(d => !d.RequiresAttendance || d.State == AttendanceObligationState.Completed);
            bool anyDutyStarted = dutyObligations.Any(d => d.ActualStart.HasValue || d.ActualEnd.HasValue);
            result.OfficialState = allDutiesDone ? AttendanceObligationState.Completed : anyDutyStarted ? AttendanceObligationState.PartiallyResolved : AttendanceObligationState.NotStarted;
            result.DailyStatus = allDutiesDone ? AttendanceDailyStatus.Present : anyDutyStarted ? AttendanceDailyStatus.Incomplete : AttendanceDailyStatus.Absent;
            result.IsComplete = allDutiesDone;
        }
        else
        {
            result.DailyStatus = workPlan is null ? AttendanceDailyStatus.NoActiveSchedule : AttendanceDailyStatus.RestDay;
            result.IsComplete = true;
        }

        result.SummaryMessage =
            $"Official: {result.OfficialState} ({result.CreditedOfficialMinutes}/{result.PlannedOfficialMinutes}m); " +
            $"Duties: {dutyObligations.Count} ({result.CreditedDutyMinutes}/{result.PlannedDutyMinutes}m); " +
            $"Shared events: {result.SharedEventCount}; Conflicts: {result.ConflictCount}.";
    }

    private static async Task PersistReconciliationAsync(
        HrmTeContext db,
        AttendanceReconciliationResult result,
        WorkPlan? workPlan,
        string triggerSource,
        int? initiatedByUserId,
        int? operationLogId,
        long? parentRunId,
        CancellationToken cancellationToken)
    {
        var workDateTime = result.WorkDate.ToDateTime(TimeOnly.MinValue);
        long? runId = parentRunId;

        int effectiveOrgId = result.OrganisationId > 0
            ? result.OrganisationId
            : workPlan?.OrganisationBusinessEntityId ?? 0;

        if (effectiveOrgId <= 0 && result.JobId > 0)
        {
            effectiveOrgId = await db.Jobs.AsNoTracking()
                .Where(j => j.JobId == result.JobId)
                .Select(j => j.OrganisationID)
                .FirstOrDefaultAsync(cancellationToken);
        }

        bool orgExists = effectiveOrgId > 0 &&
                         (!db.Database.IsSqlServer() ||
                          await db.Organisations.AsNoTracking().AnyAsync(o => o.BusinessEntityID == effectiveOrgId, cancellationToken));

        bool jobExists = result.JobId > 0 &&
                         (!db.Database.IsSqlServer() ||
                          await db.Jobs.AsNoTracking().AnyAsync(j => j.JobId == result.JobId, cancellationToken));

        int? validUserId = initiatedByUserId;
        if (validUserId.HasValue && db.Database.IsSqlServer())
        {
            bool userExists = await db.Users.AsNoTracking().AnyAsync(u => u.UserId == validUserId.Value, cancellationToken);
            if (!userExists) validUserId = null;
        }

        if (!runId.HasValue && orgExists)
        {
            var run = new AttendanceReconciliationRun
            {
                OrganisationId = effectiveOrgId,
                IndividualId = result.IndividualId > 0 ? result.IndividualId : null,
                JobId = jobExists ? result.JobId : null,
                FromWorkDate = workDateTime,
                ToWorkDate = workDateTime,
                TriggerSource = triggerSource,
                PolicyVersion = result.PolicyVersion,
                InitiatedByUserId = validUserId,
                OperationLogId = operationLogId,
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                TotalDaysEvaluated = 1,
                TotalEventsProcessed = result.RawEventCount,
                ConflictCount = result.ConflictCount,
                Status = "Completed",
                SummaryMessage = result.SummaryMessage
            };
            db.AttendanceReconciliationRuns.Add(run);
            await db.SaveChangesAsync(cancellationToken);
            runId = run.AttendanceReconciliationRunId;
        }

        result.ReconciliationRunId = runId;

        // Soft-invalidate previous obligation rows for this employee/job/date
        var prevObligations = await db.AttendanceObligations
            .Where(o => o.IndividualId == result.IndividualId &&
                        o.JobId == result.JobId &&
                        o.WorkDate == workDateTime &&
                        o.IsValid)
            .ToListAsync(cancellationToken);

        foreach (var prev in prevObligations)
            prev.IsValid = false;

        // Insert current obligations when valid organisation and job exist
        var obligationEntities = new List<(AttendanceObligationSummaryDto Dto, AttendanceObligation Entity)>();
        if (orgExists && jobExists)
        {
            foreach (var obDto in result.Obligations)
            {
                var entity = new AttendanceObligation
                {
                    OrganisationId = effectiveOrgId,
                    IndividualId = result.IndividualId,
                    JobId = result.JobId,
                    WorkDate = workDateTime,
                    WorkPlanId = obDto.WorkPlanId ?? workPlan?.WorkPlanId,
                    WorkAssignmentId = obDto.WorkAssignmentId,
                    WorkPlanSegmentId = obDto.WorkPlanSegmentId,
                    ObligationType = obDto.ObligationType,
                    ScheduleName = obDto.ScheduleName,
                    Priority = obDto.Priority,
                    ScheduledStart = obDto.ScheduledStart,
                    ScheduledEnd = obDto.ScheduledEnd,
                    GraceBeforeMinutes = obDto.GraceBeforeMinutes,
                    GraceAfterMinutes = obDto.GraceAfterMinutes,
                    RequiresAttendance = obDto.RequiresAttendance,
                    RequiresCheckOut = obDto.RequiresCheckOut,
                    ObligationState = obDto.State.ToString(),
                    ResolvedStartAttendanceLogId = obDto.ResolvedStartAttendanceLogId,
                    ResolvedEndAttendanceLogId = obDto.ResolvedEndAttendanceLogId,
                    ActualStartDateTime = obDto.ActualStartDateTime,
                    ActualEndDateTime = obDto.ActualEndDateTime,
                    PlannedMinutes = obDto.PlannedMinutes,
                    ActualPresenceMinutes = obDto.ActualPresenceMinutes,
                    CreditedWorkedMinutes = obDto.CreditedWorkedMinutes,
                    BreakDeductedMinutes = obDto.BreakDeductedMinutes,
                    LateMinutes = obDto.LateMinutes,
                    EarlyDepartureMinutes = obDto.EarlyDepartureMinutes,
                    OvertimeCandidateMinutes = obDto.OvertimeCandidateMinutes,
                    OverlapWithOfficialMinutes = obDto.OverlapWithOfficialMinutes,
                    OutsideOfficialMinutes = obDto.OutsideOfficialMinutes,
                    StatusReason = obDto.StatusReason,
                    PolicyVersion = result.PolicyVersion,
                    ReconciliationRunId = runId,
                    IsValid = true,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.AttendanceObligations.Add(entity);
                obligationEntities.Add((obDto, entity));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        foreach (var (dto, entity) in obligationEntities)
            dto.AttendanceObligationId = entity.AttendanceObligationId;

        // Sync AttendanceLogResolutions for all raw events in this evaluation
        var eventLogIds = result.Events.Select(e => e.AttendanceLogId).Distinct().ToList();
        if (eventLogIds.Count > 0)
        {
            var existingResolutions = await db.AttendanceLogResolutions
                .Where(r => eventLogIds.Contains(r.AttendanceLogId) && r.IsValid)
                .ToListAsync(cancellationToken);

            foreach (var oldRes in existingResolutions)
                oldRes.IsValid = false;

            await db.SaveChangesAsync(cancellationToken);

            foreach (var ev in result.Events)
            {
                bool primaryAssigned = false;
                var usedSegmentIds = new HashSet<int>();

                foreach (var assoc in ev.Associations.OrderByDescending(a => a.IsPrimaryClassification))
                {
                    var matchingObligation = result.Obligations.FirstOrDefault(o =>
                        (assoc.WorkAssignmentId.HasValue && o.WorkAssignmentId == assoc.WorkAssignmentId) ||
                        string.Equals(o.ObligationType, assoc.SchedulePurpose, StringComparison.OrdinalIgnoreCase));

                    var statusId = ev.IsDuplicateSuppressed
                        ? AttendanceResolutionStatusIds.DuplicatePunch
                        : assoc.ConfidenceState == "Conflict"
                            ? AttendanceResolutionStatusIds.Conflict
                            : assoc.ClockType != AttendanceClockType.Unresolved
                                ? AttendanceResolutionStatusIds.Resolved
                                : AttendanceResolutionStatusIds.NoSegment;

                    bool isPrimary = assoc.IsPrimaryClassification && !primaryAssigned;
                    if (isPrimary)
                        primaryAssigned = true;

                    int? segmentId = assoc.WorkPlanSegmentId;
                    if (segmentId.HasValue && !usedSegmentIds.Add(segmentId.Value))
                    {
                        segmentId = null;
                    }

                    var resEntity = new AttendanceLogResolution
                    {
                        AttendanceLogId = ev.AttendanceLogId,
                        WorkPlanId = assoc.WorkPlanId ?? workPlan?.WorkPlanId,
                        WorkAssignmentId = assoc.WorkAssignmentId,
                        WorkPlanSegmentId = segmentId,
                        JobId = result.JobId,
                        AttendanceResolutionStatusId = statusId,
                        AttendanceClockTypeId = assoc.ClockType == AttendanceClockType.Unresolved ? null : (int)assoc.ClockType,
                        ResolutionDate = ev.LogDateTime,
                        ResolutionMessage = assoc.MatchReason,
                        IsPrimaryClassification = isPrimary,
                        SchedulePurpose = assoc.SchedulePurpose,
                        LogicalEventType = assoc.LogicalEventType,
                        MatchMethod = assoc.MatchMethod,
                        MatchReason = assoc.MatchReason,
                        ConfidenceState = assoc.ConfidenceState,
                        PolicyVersion = assoc.PolicyVersion,
                        ReconciliationRunId = runId,
                        AttendanceObligationId = matchingObligation?.AttendanceObligationId,
                        OperationLogId = operationLogId,
                        IsValid = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    db.AttendanceLogResolutions.Add(resEntity);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
