using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Attendance.AttendancePlan;
using HRM.Services.Attendance.Evaluation;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace HRM.Services
{
    public partial class AttendanceService : IAttendanceService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IAttendanceLogDataLoader _loader;
        private readonly IUserAccessService _userAccessService;
        //private readonly IAttendancePlanService _attendancePlanService;
        private readonly IAttendanceEvaluationService _attendanceEvaluationService;
        private readonly ILogger<AttendanceService> _logger;


        public AttendanceService(IDbContextFactory<HrmTeContext> factory,
            IAttendanceEvaluationService attendanceEvaluationService
,           IAttendanceLogDataLoader loader, 
            IUserAccessService userAccessService, 
            IAttendancePlanService attendancePlanService, 
            ILogger<AttendanceService> logger)
        {
            _dbFactory = factory;
            _loader = loader;
            _userAccessService = userAccessService;
            //_attendancePlanService = attendancePlanService;
            _logger = logger;
            _attendanceEvaluationService = attendanceEvaluationService;

        }

        public async Task<(AttendanceLogDto? checkIn, AttendanceLogDto? checkOut)> GetTodayStatusAsync(int individualId)
        {
            var today = DateTime.UtcNow.AddHours(5).Date;

            var checkIn = await _loader.GetEarliestCheckInAsync(individualId, today);
            var checkOut = await _loader.GetLatestCheckOutAsync(individualId, today);

            return (checkIn, checkOut);
        }


     

        public async Task<List<AttendanceLogDto>> GetMyUnevenLogsAsync()
        {
            var individualId = await _userAccessService.GetCurrentIndividualIdAsync();

            return await GetUnevenLogsAsync(individualId);
        }

        

        public async Task<List<AttendanceLogDto>> GetUnevenLogsAsync(int individualId)
        {
 
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var today = DateTime.UtcNow.AddHours(5).Date;

            // ---------------------------------------------------------
            // Current salary period
            // 15th -> 16th of following month inclusive
            // ---------------------------------------------------------
            DateTime periodStart;

            if (today.Day >= 15)
            {
                periodStart =
                    new DateTime(
                        today.Year,
                        today.Month,
                        15);
            }
            else
            {
                periodStart =
                    new DateTime(
                        today.Year,
                        today.Month,
                        15)
                    .AddMonths(-1);
            }

            var periodEndExclusive =
                periodStart
                    .AddMonths(1)
                    .AddDays(2);

            // ---------------------------------------------------------
            // Load logs
            // ---------------------------------------------------------
            var logs = await db.AttendanceLogs
                .AsNoTracking()
                .Include(x => x.AttendanceLogResolutions)
                .Where(x =>
                    x.IndividualId == individualId &&
                    x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
                    x.Date >= periodStart &&
                    x.Date < periodEndExclusive)
                .OrderBy(x => x.Date)
                .ToListAsync();

            // ---------------------------------------------------------
            // Load work plans
            // ---------------------------------------------------------
            var plans = await db.WorkPlans
                .AsNoTracking()
                .Include(x => x.WorkPlanSegments)
                .Where(x =>
                    x.IndividualId == individualId &&
                    x.IsValid &&
                    x.WorkDate >= periodStart &&
                    x.WorkDate < periodEndExclusive)
                .ToListAsync();

            var unevenLogIds = new HashSet<int>();

            // ---------------------------------------------------------
            // Process one WorkPlan/day at a time
            // ---------------------------------------------------------
            foreach (var plan in plans)
            {
                var dayLogs = logs
                    .Where(x =>
                        x.Date.Date == plan.WorkDate.Date)
                    .OrderBy(x => x.Date)
                    .ToList();

                var requiredSegments = plan.WorkPlanSegments
                    .Where(x =>
                        x.RequiresAttendance)
                    .OrderBy(x => x.StartDateTime)
                    .ToList();

                if (!requiredSegments.Any())
                {
                    continue;
                }

                // -----------------------------------------------------
                // Find the final required attendance boundary.
                //
                // Anything AFTER this has been satisfied should not
                // automatically become uneven.
                // -----------------------------------------------------
                var finalSegment =
                    requiredSegments.Last();

                var finalResolution = dayLogs
                    .SelectMany(x =>
                        x.AttendanceLogResolutions)
                    .Where(x =>
                        x.WorkPlanSegmentId ==
                            finalSegment.WorkPlanSegmentId)
                    .OrderByDescending(x =>
                        x.AttendanceLogResolutionId)
                    .FirstOrDefault();

                DateTime? finalClockTime = null;

                if (finalResolution != null)
                {
                    finalClockTime = dayLogs
                        .Where(x =>
                            x.AttendanceLogId ==
                            finalResolution.AttendanceLogId)
                        .Select(x => (DateTime?)x.Date)
                        .FirstOrDefault();
                }

                // -----------------------------------------------------
                // Check every required segment
                // -----------------------------------------------------
                foreach (var segment in requiredSegments)
                {
                    var resolvedLog = dayLogs
                        .Where(log =>
                            log.AttendanceLogResolutions.Where(r => r.IsValid).Any(r =>
                                r.WorkPlanSegmentId ==
                                    segment.WorkPlanSegmentId))
                        .OrderBy(log => log.Date)
                        .FirstOrDefault();

                    // Expected clock exists.
                    if (resolvedLog != null)
                    {
                        continue;
                    }

                    // -------------------------------------------------
                    // No resolved log for required segment.
                    //
                    // Look for unmatched clocks around this required
                    // attendance window.
                    // -------------------------------------------------
                    var candidate = dayLogs
                        .Where(log =>
                        {
                            // Already belongs to another valid segment.
                            var hasResolution =
                                log.AttendanceLogResolutions.Where(r => r.IsValid).Any(r =>
                                    r.WorkPlanSegmentId.HasValue);

                            if (hasResolution)
                            {
                                return false;
                            }

                            // Anything after successful final checkout
                            // isn't an uneven clock for this work plan.
                            if (finalClockTime.HasValue &&
                                log.Date > finalClockTime.Value)
                            {
                                return false;
                            }

                            return true;
                        })
                        .OrderBy(log =>
                            Math.Abs(
                                (log.Date -
                                 segment.StartDateTime)
                                .TotalMinutes))
                        .FirstOrDefault();

                    if (candidate != null)
                    {
                        unevenLogIds.Add(
                            candidate.AttendanceLogId);
                    }
                }
            }

            // ---------------------------------------------------------
            // Convert only uneven logs to DTO
            // ---------------------------------------------------------
            return logs
                .Where(x =>
                    unevenLogIds.Contains(
                        x.AttendanceLogId))
                .Select(x => new AttendanceLogDto
                {
                    AttendanceLogID =
                        x.AttendanceLogId,

                    AttendanceDeviceID =
                        x.AttendanceDeviceId,

                    IndividualID =
                        x.IndividualId,

                    OrganisationID =
                        x.OrganisationId,

                    OrganisationStructureID =
                        x.OrganisationStructureId,

                    InOutModeID =
                        x.InOutModeId,

                    Year =
                        x.Year,

                    Month =
                        x.Month,

                    Day =
                        x.Day,

                    Hour =
                        x.Hour,

                    Minute =
                        x.Minute,

                    Second =
                        x.Second,

                    Date =
                        x.Date,

                    AttendanceLogModeID =
                        x.AttendanceLogModeId,

                    AttendanceLogStateID =
                        x.AttendanceLogStateId,

                    OperationLogID =
                        x.OperationLogId,

                    RelatedAttendanceLogID =
                        x.RelatedAttendanceLogId,

                    ActualInOutMode =
                        x.ActualInOutMode
                })
                .OrderBy(x => x.Date)
                .ToList();
        }


        public async Task<List<AttendanceLogDto>> GetMyWeeklyAttendanceAsync()
        {
            var context =
                await _userAccessService.RequireContextAsync();

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            // Today + previous 6 days = 7 days total
            var startDate =
                DateTime.UtcNow.AddHours(5).Date.AddDays(-6);

            var endDate =
                DateTime.UtcNow.AddHours(5).Date.AddDays(1);

            // ---------------------------------------------------------
            // Load raw logs together with their resolutions
            // ---------------------------------------------------------
            var logs = await db.AttendanceLogs
                .AsNoTracking()
                .Include(x => x.AttendanceLogResolutions)
                .Where(x =>
                    x.IndividualId == context.IndividualId &&
                    x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
                    x.Date >= startDate &&
                    x.Date < endDate)
                .OrderBy(x => x.Date)
                .ToListAsync();

            // ---------------------------------------------------------
            // Map entities -> DTOs
            // ---------------------------------------------------------
            var result = logs
                .Select(log =>
                {
                    // Get the latest resolution for this clock event.
                    //
                    // This is important because later a clock event may
                    // be reprocessed and receive a newer resolution.
                    var resolution = log.AttendanceLogResolutions.Where(x => x.IsValid).OrderByDescending(x => x.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved).ThenBy(x => x.AttendanceLogResolutionId).Take(1)
                        .OrderByDescending(x =>
                            x.AttendanceLogResolutionId)
                        .FirstOrDefault();

                    return new AttendanceLogDto
                    {
                        AttendanceLogID =
                            log.AttendanceLogId,

                        AttendanceDeviceID =
                            log.AttendanceDeviceId,

                        IndividualID =
                            log.IndividualId,

                        OrganisationID =
                            log.OrganisationId,

                        OrganisationStructureID =
                            log.OrganisationStructureId,

                        // Keep original physical/device value.
                        // Do NOT use this for CheckIn/CheckOut.
                        InOutModeID =
                            log.InOutModeId,

                        Year =
                            log.Year,

                        Month =
                            log.Month,

                        Day =
                            log.Day,

                        Hour =
                            log.Hour,

                        Minute =
                            log.Minute,

                        Second =
                            log.Second,

                        Date =
                            log.Date,

                        AttendanceLogModeID =
                            log.AttendanceLogModeId,

                        AttendanceLogStateID =
                            log.AttendanceLogStateId,

                        OperationLogID =
                            log.OperationLogId,

                        RelatedAttendanceLogID =
                            log.RelatedAttendanceLogId,

                        ActualInOutMode =
                            log.ActualInOutMode,

                        // ---------------------------------------------
                        // WorkPlanning / Resolution
                        // ---------------------------------------------

                        AttendanceLogResolutionID =
                            resolution?.AttendanceLogResolutionId,

                        WorkPlanID =
                            resolution?.WorkPlanId,

                        WorkAssignmentID =
                            resolution?.WorkAssignmentId,

                        WorkAssignmentSegmentID =
                            resolution?.WorkAssignmentSegmentId,

                        WorkPlanSegmentID =
                            resolution?.WorkPlanSegmentId,

                        JobID =
                            resolution?.JobId,

                        ResolvedClockType =
                            resolution == null
                                ? null
                                : (AttendanceClockType?)
                                    resolution.AttendanceClockTypeId
                    };
                })
                .ToList();

            return result;
        }

        public async Task<List<AttendanceRawLogDto>>GetLogsAsync(
             int individualId,
             int jobId,
             DateTime checkTime,
             CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var date =
                checkTime.Date;

            var nextDate =
                date.AddDays(1);

            var organisation = await db.Jobs.Where(j => j.JobId == jobId && j.IndividualID == individualId).Select(j => (int?)j.OrganisationID).SingleOrDefaultAsync(cancellationToken);
            if (!organisation.HasValue) return [];
            var rawLogEntities = await db.AttendanceLogs.AsNoTracking()
                .Where(log =>
                    log.IndividualId == individualId &&
                    log.OrganisationId == organisation.Value &&
                    log.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
                    log.Date >= date &&
                    log.Date < nextDate)
                .OrderBy(log => log.Date)
                .ThenBy(log => log.AttendanceLogId)
                .ToListAsync(cancellationToken);

            var logIds = rawLogEntities.Select(l => l.AttendanceLogId).ToList();
            var activeResolutions = logIds.Count == 0
                ? []
                : await (
                    from res in db.AttendanceLogResolutions.AsNoTracking()
                    where res.IsValid && logIds.Contains(res.AttendanceLogId)
                    join seg in db.WorkPlanSegments.AsNoTracking()
                        on res.WorkPlanSegmentId equals seg.WorkPlanSegmentId into segGroup
                    from seg in segGroup.DefaultIfEmpty()
                    join assign in db.WorkAssignments.AsNoTracking()
                        on res.WorkAssignmentId equals assign.WorkAssignmentId into assignGroup
                    from assign in assignGroup.DefaultIfEmpty()
                    select new
                    {
                        Resolution = res,
                        Segment = seg,
                        Assignment = assign
                    })
                    .ToListAsync(cancellationToken);

            var resolutionsByLog = activeResolutions
                .GroupBy(x => x.Resolution.AttendanceLogId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.Resolution.IsPrimaryClassification)
                          .ThenBy(x => (x.Resolution.SchedulePurpose ?? x.Segment?.SchedulePurpose ?? x.Assignment?.SchedulePurpose) == "Official" ? 0 : 1)
                          .ThenBy(x => x.Resolution.AttendanceLogResolutionId)
                          .ToList());

            var logs = rawLogEntities.Select(log =>
            {
                var orderedRes = resolutionsByLog.GetValueOrDefault(log.AttendanceLogId) ?? [];
                var primary = orderedRes.FirstOrDefault();
                var res = primary?.Resolution;
                var seg = primary?.Segment;
                var assign = primary?.Assignment;
                var purpose = res?.SchedulePurpose ?? seg?.SchedulePurpose ?? assign?.SchedulePurpose;
                var clockType = res is { AttendanceClockTypeId: not null }
                    ? (AttendanceClockType)res.AttendanceClockTypeId.Value
                    : AttendanceClockType.Unresolved;

                var associations = orderedRes.Select(item =>
                {
                    var itemPurpose = item.Resolution.SchedulePurpose ?? item.Segment?.SchedulePurpose ?? item.Assignment?.SchedulePurpose ?? "Official";
                    var itemClock = item.Resolution.AttendanceClockTypeId.HasValue
                        ? (AttendanceClockType)item.Resolution.AttendanceClockTypeId.Value
                        : AttendanceClockType.Unresolved;
                    return new AttendanceEventAssociationDto
                    {
                        AttendanceLogResolutionId = item.Resolution.AttendanceLogResolutionId,
                        AttendanceLogId = log.AttendanceLogId,
                        WorkPlanId = item.Resolution.WorkPlanId,
                        WorkAssignmentId = item.Resolution.WorkAssignmentId,
                        WorkPlanSegmentId = item.Resolution.WorkPlanSegmentId,
                        SchedulePurpose = itemPurpose,
                        ScheduleName = item.Assignment?.Name ?? item.Segment?.Name ?? itemPurpose,
                        SegmentName = item.Segment?.Name ?? itemPurpose,
                        ClockType = itemClock,
                        LogicalEventType = item.Resolution.LogicalEventType ?? $"{itemPurpose}{itemClock}",
                        IsPrimaryClassification = item.Resolution.IsPrimaryClassification,
                        MatchMethod = item.Resolution.MatchMethod ?? "Deterministic",
                        MatchReason = item.Resolution.MatchReason ?? item.Resolution.ResolutionMessage ?? string.Empty,
                        ConfidenceState = item.Resolution.ConfidenceState ?? "Deterministic",
                        PolicyVersion = item.Resolution.PolicyVersion ?? "v2.0-OfficialPriority",
                        ScheduledStart = item.Assignment?.StartDateTime ?? item.Segment?.StartDateTime,
                        ScheduledEnd = item.Assignment?.EndDateTime ?? item.Segment?.EndDateTime
                    };
                }).ToList();

                return new AttendanceRawLogDto
                {
                    AttendanceLogId = log.AttendanceLogId,
                    InOutModeId = log.InOutModeId,
                    WorkAssignmentId = res?.WorkAssignmentId,
                    SchedulePurpose = purpose,
                    ScheduleName = assign?.Name,
                    ScheduledStart = assign?.StartDateTime,
                    ScheduledEnd = assign?.EndDateTime,
                    IndividualId = log.IndividualId,
                    LogDateTime = log.Date,
                    AttendanceLogResolutionId = res?.AttendanceLogResolutionId,
                    WorkPlanId = res?.WorkPlanId,
                    WorkPlanSegmentId = res?.WorkPlanSegmentId,
                    SegmentName = seg?.Name,
                    ClockType = clockType,
                    ResolutionStatusId = res?.AttendanceResolutionStatusId,
                    ResolutionMessage = res?.ResolutionMessage,
                    ResolutionDate = res?.ResolutionDate,
                    IsPrimaryClassification = res?.IsPrimaryClassification ?? true,
                    LogicalEventType = res?.LogicalEventType,
                    MatchMethod = res?.MatchMethod,
                    MatchReason = res?.MatchReason ?? res?.ResolutionMessage,
                    ConfidenceState = res?.ConfidenceState ?? "Deterministic",
                    PolicyVersion = res?.PolicyVersion ?? "v2.0-OfficialPriority",
                    IsSharedAcrossSchedules = associations.Count > 1,
                    SharedScheduleSummary = associations.Count > 1
                        ? $"Shared physical event (#{log.AttendanceLogId}): " + string.Join(" + ", associations.Select(a => $"{a.SchedulePurpose} {a.ClockType}"))
                        : null,
                    Associations = associations
                };
            }).ToList();

            await HRM.Helpers.DutyClockEventPresentation.ApplyAsync(db, logs, individualId, jobId, organisation.Value, date, cancellationToken);
            return logs;


        }


        public async Task<AttendanceDailyEvaluationDto> GetEvaluationAsync(
            int individualId,
            int jobId,
            int organisationBusinessEntityId,
            DateTime workDate,
            CancellationToken cancellationToken = default)
        {
            return await _attendanceEvaluationService.EvaluateAsync(
                individualId,
                jobId,
                workDate,
                cancellationToken);
        }
        public async Task<WeeklyWorkedHoursDto> GetMyWeeklyWorkedHoursAsync()
        {
            var actor = await _userAccessService.RequireContextAsync();
            var org = await _userAccessService.GetCurrentOrganisationIdAsync();
            if (org <= 0 || !await _userAccessService.HasOrganisationAccessAsync(org))
                throw new UnauthorizedAccessException("Select an organisation you can access.");
            await using var db = await _dbFactory.CreateDbContextAsync();
            var result = await HRM.Helpers.WeeklyWorkingHours.LoadAsync(db, actor.IndividualId, org, DateTime.UtcNow.AddHours(5).Date);
            var previous = await HRM.Helpers.WeeklyWorkingHours.LoadAsync(db, actor.IndividualId, org, result.WeekStart.AddDays(-1));
            result.PreviousWeekHours = previous.CurrentWeekHours;
            return result;
        }

        public async Task<List<YearlyAttendanceDayDto>> GetMyYearlyAttendanceAsync(int year, CancellationToken cancellationToken = default)
        {
            var actor = await _userAccessService.RequireContextAsync();
            return await GetYearlyAttendanceAsync(actor.IndividualId, year, null, cancellationToken);
        }

        private async Task<int> RequireStaffAttendanceAccessAsync(int staffId, CancellationToken ct)
        {
            var actor = await _userAccessService.RequireContextAsync();
            var org = await _userAccessService.GetCurrentOrganisationIdAsync();
            if (staffId <= 0 || org <= 0 || !await _userAccessService.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select an organisation you can access.");
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var jobs = await db.Jobs.Where(x => x.IndividualID == staffId && x.OrganisationID == org).Select(x => x.JobId).ToListAsync(ct);
            if (jobs.Count == 0) throw new UnauthorizedAccessException("The selected employee is not in the current organisation.");
            await HRM.Helpers.StaffAttributeAccess.RequireViewAsync(db, _userAccessService, staffId, org, ct);
            if (actor.IndividualId == staffId || await CanViewAllStaffAttendanceAsync(actor)) return org;
            foreach (var job in jobs)
                if ((await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db, job)).Contains(actor.IndividualId)) return org;
            throw new UnauthorizedAccessException("You cannot view this employee's attendance.");
        }

        public async Task<List<YearlyAttendanceDayDto>> GetStaffYearlyAttendanceAsync(int staffId, int year, CancellationToken cancellationToken = default)
        {
            var org = await RequireStaffAttendanceAccessAsync(staffId, cancellationToken);
            var days = await GetYearlyAttendanceAsync(staffId, year, org, cancellationToken);
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var employment = await db.Jobs.AsNoTracking().Where(x => x.IndividualID == staffId && x.OrganisationID == org && x.JobStateId == 4)
                .Select(x => new { x.JoinedDate, x.TerminatedDate }).ToListAsync(cancellationToken);
            return days.Where(x => x.HasAttendanceLogs || x.IsOnLeave || employment.Any(j => j.JoinedDate.Date <= x.Date && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value.Date >= x.Date))).ToList();
        }

        public async Task<List<StaffAttendanceClockEventDto>> GetStaffClockEventsAsync(int staffId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var org = await RequireStaffAttendanceAccessAsync(staffId, cancellationToken);
            if (from.Date > to.Date || (to.Date - from.Date).Days > 366) throw new InvalidOperationException("Select a date range of up to one year.");
            var end = to.Date.AddDays(1);
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var logs = await db.AttendanceLogs.AsNoTracking()
                .Include(x => x.AttendanceLogResolutions).ThenInclude(x => x.WorkAssignment)
                .Include(x => x.AttendanceLogResolutions).ThenInclude(x => x.WorkPlan).ThenInclude(x => x!.WorkPlanSegments)
                .Where(x => x.IndividualId == staffId && x.OrganisationId == org && x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid && x.Date >= from.Date && x.Date < end)
                .OrderByDescending(x => x.Date).Take(1000).ToListAsync(cancellationToken);
            return logs.Select(x =>
            {
                var validResolutions = x.AttendanceLogResolutions
                    .Where(r => r.IsValid)
                    .OrderByDescending(r => r.IsPrimaryClassification)
                    .ThenByDescending(r => !string.Equals(r.SchedulePurpose, "Duty", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(r => r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved)
                    .ThenBy(r => r.AttendanceLogResolutionId)
                    .ToList();
                var resolution = validResolutions.FirstOrDefault();
                var code = resolution?.AttendanceClockTypeId ?? (validResolutions.Count == 0 ? x.InOutModeId : null);
                var label = resolution?.LogicalEventType switch
                {
                    "OfficialCheckIn" => "Official Check in",
                    "OfficialCheckOut" => "Official Check out",
                    "OfficialBreakOut" => "Official Break out",
                    "OfficialBreakIn" => "Official Break in",
                    "DutyCheckIn" => "Duty Check in",
                    "DutyCheckOut" => "Duty Check out",
                    "DuplicateIgnored" => "Duplicate (Ignored)",
                    "UnmatchedException" => "Unresolved / Exception",
                    _ => code switch
                    {
                        1 => "Check in",
                        2 => "Check out",
                        3 => "Break out",
                        4 => "Break in",
                        5 => "Ignored",
                        _ => "Unresolved"
                    }
                };
                string FormatSchedule(HRM.Models.AttendanceLogResolution? r)
                {
                    if (r == null) return "—";
                    var seg = r.WorkPlanSegmentId.HasValue
                        ? r.WorkPlan?.WorkPlanSegments.FirstOrDefault(s => s.WorkPlanSegmentId == r.WorkPlanSegmentId.Value)
                        : null;
                    if (r.WorkAssignment != null)
                        return $"{r.WorkAssignment.SchedulePurpose} · {r.WorkAssignment.Name}";
                    if (seg != null)
                        return $"{seg.SchedulePurpose} · {seg.Name}";
                    if (!string.IsNullOrWhiteSpace(r.SchedulePurpose))
                        return r.SchedulePurpose;
                    return "—";
                }
                var primarySchedule = FormatSchedule(resolution);
                var isShared = validResolutions.Count > 1;
                var sharedSummary = isShared
                    ? string.Join(" + ", validResolutions.Select(r => $"{FormatSchedule(r)} ({r.LogicalEventType ?? "Matched"})"))
                    : null;
                return new StaffAttendanceClockEventDto(
                    x.Date,
                    label,
                    primarySchedule,
                    IsSharedAcrossSchedules: isShared,
                    SharedScheduleSummary: sharedSummary,
                    LogicalEventType: resolution?.LogicalEventType,
                    MatchMethod: resolution?.MatchMethod,
                    MatchReason: resolution?.MatchReason,
                    ConfidenceState: resolution?.ConfidenceState,
                    AttendanceLogId: x.AttendanceLogId);
            }).ToList();
        }

        private async Task<List<YearlyAttendanceDayDto>> GetYearlyAttendanceAsync(int individualId,
            int year,
            int? organisationId,
            CancellationToken cancellationToken = default, DateTime? rangeFrom = null, DateTime? rangeTo = null)
        {
            // ---------------------------------------------------------
            // Validate year
            // ---------------------------------------------------------
            if (year < 2000 || year > DateTime.UtcNow.AddHours(5).Year)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Invalid attendance year.");
            }


            // ---------------------------------------------------------
            // Current user
            // ---------------------------------------------------------


            // ---------------------------------------------------------
            // Database
            // ---------------------------------------------------------
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);


            // ---------------------------------------------------------
            // Year boundaries
            //
            // Inclusive start
            // Exclusive end
            // ---------------------------------------------------------
            var yearStart =
                new DateTime(
                    year,
                    1,
                    1);

            var yearEndExclusive =
                yearStart.AddYears(1);
            if (rangeFrom.HasValue) yearStart = rangeFrom.Value.Date;
            if (rangeTo.HasValue) yearEndExclusive = rangeTo.Value.Date.AddDays(1);


            // ---------------------------------------------------------
            // Do not include future attendance
            // ---------------------------------------------------------
            var queryEnd =
                year == DateTime.UtcNow.AddHours(5).Year
                    ? DateTime.UtcNow.AddHours(5)
                    : yearEndExclusive;
            if (queryEnd > yearEndExclusive) queryEnd = yearEndExclusive;


            // ---------------------------------------------------------
            // Load attendance logs
            // ---------------------------------------------------------
            var logs = await db.AttendanceLogs
                .AsNoTracking()
                .Include(x =>
                    x.AttendanceLogResolutions)
                .Where(x =>
                    x.IndividualId ==
                        individualId && (!organisationId.HasValue || x.OrganisationId == organisationId) &&

                    x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&

                    x.Date >= yearStart &&

                    x.Date < queryEnd)
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);


            // ---------------------------------------------------------
            // Load approved leave across all of the employee's jobs.
            // ---------------------------------------------------------
            var approvedLeaves = await db.Leaves
                .AsNoTracking()
                .Where(x =>
                    x.Job.IndividualID == individualId && (!organisationId.HasValue || x.Job.OrganisationID == organisationId) &&
                    x.LeaveStateId == SharedConfig.LeaveStates.APPROVED &&
                    x.FromDate < yearEndExclusive &&
                    x.ToDate >= yearStart)
                .Select(x => new { x.FromDate, x.ToDate, Type = x.LeaveType.Name })
                .ToListAsync(cancellationToken);

            var eligibility = await HRM.Helpers.AttendanceEligibilityCalendar.LoadAsync(db, individualId,
                yearStart, yearEndExclusive.AddDays(-1), organisationId, ct: cancellationToken);

            var holidayExceptions = await db.OrganisationCalendarExceptions.AsNoTracking()
                .Where(x => x.CalendarImpactType == "MarkAsNonWorkingDay" && x.IsActive &&
                            (!organisationId.HasValue || x.OrganisationBusinessEntityId == organisationId.Value) &&
                            x.EffectiveDate < yearEndExclusive && x.EffectiveEndDate >= yearStart)
                .ToListAsync(cancellationToken);
            var holidayEvents = await db.OrganisationEvents.AsNoTracking()
                .Where(x => (x.CalendarImpactType == "MarkAsNonWorkingDay" || x.Category == "Holiday") &&
                            x.Status == "Published" && x.IsValid &&
                            (!organisationId.HasValue || x.OrganisationBusinessEntityId == organisationId.Value) &&
                            x.StartDateTime < yearEndExclusive && x.EndDateTime >= yearStart)
                .ToListAsync(cancellationToken);

            var scheduleFacts = await HRM.Helpers.AttendanceScheduleFacts.LoadAsync(db, individualId,
                yearStart, queryEnd.Date, organisationId, cancellationToken, logs);
            var schedulesByDay = scheduleFacts.GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.ToList());
            // Older resolutions can identify a segment without an assignment.
            var segmentIds = logs.SelectMany(l => l.AttendanceLogResolutions)
                .Where(r => r.WorkPlanSegmentId.HasValue).Select(r => r.WorkPlanSegmentId!.Value).Distinct().ToList();
            var dutySegmentIds = (await db.WorkPlanSegments.AsNoTracking()
                .Where(s => segmentIds.Contains(s.WorkPlanSegmentId) && s.SchedulePurpose == "Duty")
                .Select(s => s.WorkPlanSegmentId).ToListAsync(cancellationToken)).ToHashSet();
            var dutyAssignmentIds = scheduleFacts.Where(s => s.Purpose == "Duty").Select(s => s.WorkAssignmentId).ToHashSet();

            // Group physical clock events by calendar date for the overview.
            var logsByDate = logs
                .GroupBy(x => x.Date.Date)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .OrderBy(log => log.Date)
                        .ToList());


            // ---------------------------------------------------------
            // Determine last date to generate
            // ---------------------------------------------------------
            var lastDate =
                year == DateTime.UtcNow.AddHours(5).Year
                    ? DateTime.UtcNow.AddHours(5).Date
                    : new DateTime(
                        year,
                        12,
                        31);
            if (lastDate >= yearEndExclusive) lastDate = yearEndExclusive.AddDays(-1);


            var result =
                new List<YearlyAttendanceDayDto>();


            // ---------------------------------------------------------
            // Generate one DTO per calendar day
            // ---------------------------------------------------------
            for (var date = yearStart.Date;
                 date <= lastDate.Date;
                 date = date.AddDays(1))
            {
                logsByDate.TryGetValue(
                    date,
                    out var dayLogs);

                dayLogs ??= [];


                // -----------------------------------------------------
                // Get effective resolution for every physical log
                // -----------------------------------------------------
                bool IsDutyResolution(HRM.Models.AttendanceLogResolution x) =>
                    string.Equals(x.SchedulePurpose, "Duty", StringComparison.OrdinalIgnoreCase) ||
                    (x.WorkPlanSegmentId.HasValue && dutySegmentIds.Contains(x.WorkPlanSegmentId.Value)) ||
                    (x.WorkAssignmentId.HasValue && dutyAssignmentIds.Contains(x.WorkAssignmentId.Value));

                var events = dayLogs
                    .Select(log =>
                    {
                        var resolution =
                            log.AttendanceLogResolutions
                                .Where(x => x.IsValid)
                                .OrderByDescending(x => x.IsPrimaryClassification)
                                .ThenByDescending(x => !IsDutyResolution(x))
                                .ThenByDescending(x => x.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved)
                                .ThenByDescending(x => x.AttendanceLogResolutionId)
                                .FirstOrDefault();

                        return new
                        {
                            Log = log,
                            Resolution = resolution
                        };
                    })
                    .Where(x => x.Resolution == null || !IsDutyResolution(x.Resolution))
                    .OrderBy(x => x.Log.Date)
                    .ToList();


                // -----------------------------------------------------
                // Resolved Check Ins
                // -----------------------------------------------------
                var checkIns = events
                    .Where(x =>
                        x.Resolution != null &&

                        x.Resolution
                            .AttendanceClockTypeId ==
                        (int)AttendanceClockType.CheckIn)
                    .OrderBy(x =>
                        x.Log.Date)
                    .ToList();


                // -----------------------------------------------------
                // Resolved Check Outs
                // -----------------------------------------------------
                var checkOuts = events
                    .Where(x =>
                        x.Resolution != null &&

                        x.Resolution
                            .AttendanceClockTypeId ==
                        (int)AttendanceClockType.CheckOut)
                    .OrderBy(x =>
                        x.Log.Date)
                    .ToList();


                // -----------------------------------------------------
                // Ignored events
                // -----------------------------------------------------
                var ignoredEvents = events
                    .Where(x =>
                        x.Resolution != null &&

                        x.Resolution
                            .AttendanceClockTypeId ==
                        (int)AttendanceClockType.Ignored)
                    .ToList();


                // -----------------------------------------------------
                // Useful resolved events
                // -----------------------------------------------------
                var resolvedEvents = events
                    .Where(x =>
                        x.Resolution != null &&
                        (
                            x.Resolution
                                .AttendanceClockTypeId ==
                            (int)AttendanceClockType.CheckIn
                            ||
                            x.Resolution
                                .AttendanceClockTypeId ==
                            (int)AttendanceClockType.CheckOut
                            ||
                            x.Resolution
                                .AttendanceClockTypeId == (int)AttendanceClockType.BreakOut
                            ||
                            x.Resolution
                                .AttendanceClockTypeId == (int)AttendanceClockType.BreakIn
                        ))
                    .ToList();


                // -----------------------------------------------------
                // Unresolved events
                //
                // No resolution OR resolution has not produced a
                // meaningful attendance clock type.
                //
                // Ignored events are not considered unresolved.
                // -----------------------------------------------------
                var unresolvedEvents = events
                    .Where(x =>
                        x.Resolution == null ||
                        (
                            x.Resolution
                                .AttendanceClockTypeId !=
                            (int)AttendanceClockType.CheckIn
                            &&
                            x.Resolution
                                .AttendanceClockTypeId !=
                            (int)AttendanceClockType.CheckOut
                            &&
                            x.Resolution
                                .AttendanceClockTypeId != (int)AttendanceClockType.BreakOut
                            &&
                            x.Resolution
                                .AttendanceClockTypeId != (int)AttendanceClockType.BreakIn
                            &&
                            x.Resolution
                                .AttendanceClockTypeId !=
                            (int)AttendanceClockType.Ignored
                        ))
                    .ToList();


                // -----------------------------------------------------
                // First Check In
                //
                // When several punches match the same boundary, the
                // one closest to the scheduled start is the effective
                // arrival (for example 07:30 rather than 06:20 for
                // an 08:00 shift).
                // -----------------------------------------------------
                var attendanceSchedules = schedulesByDay.GetValueOrDefault(date) ?? [];
                var attendanceOfficial = attendanceSchedules
                    .Where(s => s.RequiresAttendance && s.Purpose == "Official")
                    .ToList();
                var scheduledAttendance = attendanceOfficial
                    .OrderBy(s => s.ScheduledStart)
                    .FirstOrDefault();
                var firstCheckIn =
                    HRM.Helpers.AttendanceTiming.Closest(
                        checkIns,
                        x => x.Log.Date,
                        scheduledAttendance?.ScheduledStart);


                // -----------------------------------------------------
                // Last Check Out
                // -----------------------------------------------------
                var lastCheckOut =
                    checkOuts.LastOrDefault();


                // -----------------------------------------------------
                // Worked hours
                //
                // Temporary/simple calculation:
                //
                // First CheckIn -> Last CheckOut
                //
                // We will later replace this with WorkPlan/segment
                // calculation so breaks and overnight work are handled
                // properly.
                // -----------------------------------------------------
                decimal workedHours = 0;

                if (eligibility[date].HasApprovedPosition && eligibility[date].HasActiveSchedule && firstCheckIn != null &&
                    lastCheckOut != null &&
                    lastCheckOut.Log.Date >
                    firstCheckIn.Log.Date)
                {
                    var worked =
                        lastCheckOut.Log.Date -
                        firstCheckIn.Log.Date;

                    workedHours =
                        Math.Round(
                            (decimal)worked.TotalHours,
                            2);
                }


                // -----------------------------------------------------
                // Attendance state
                // -----------------------------------------------------

                // Approved leave takes precedence over attendance states,
                // while physical clock details remain available on the DTO.
                var isOnLeave = approvedLeaves.Any(x =>
                    x.FromDate.Date <= date && x.ToDate.Date >= date);
                var official = attendanceOfficial;
                var scheduled = scheduledAttendance;
                var actualStart = scheduled?.CheckIn ?? (firstCheckIn?.Resolution?.WorkAssignmentId == null ? firstCheckIn?.Log.Date : null);
                var lateUntil = actualStart ?? (date == DateTime.UtcNow.AddHours(5).Date ? DateTime.UtcNow.AddHours(5) : (DateTime?)null);
                var isLate = !isOnLeave && eligibility[date].CanBeAbsent && scheduled != null &&
                    lateUntil.HasValue && HRM.Helpers.AttendanceTiming.LateMinutes(lateUntil.Value, scheduled.ScheduledStart) > 0;
                var lateMinutes = isLate ? HRM.Helpers.AttendanceTiming.LateMinutes(lateUntil!.Value, scheduled!.ScheduledStart) : 0;

                // Presence is determined by successfully resolved CheckIn.
                var isPresent =
                    !isOnLeave && eligibility[date].HasApprovedPosition && eligibility[date].HasActiveSchedule && firstCheckIn != null;
                var requiredSchedules = official;
                var recordedLateness = requiredSchedules.Where(s => s.CheckIn.HasValue)
                    .Select(s => HRM.Helpers.AttendanceTiming.LateMinutes(s.CheckIn!.Value, s.ScheduledStart)).Where(m => m > 0).ToList();
                if (isPresent && recordedLateness.Count > 0)
                {
                    isLate = true;
                    lateMinutes = recordedLateness.Sum();
                }
                else if (isPresent && !actualStart.HasValue) { isLate = false; lateMinutes = 0; }
                var completed = requiredSchedules.Count > 0 && requiredSchedules.All(s => s.CheckIn.HasValue && s.CheckOut.HasValue &&
                    s.CheckOut >= s.CheckIn && s.CheckOut >= s.ScheduledEnd && HRM.Helpers.AttendanceTiming.LateMinutes(s.CheckIn.Value, s.ScheduledStart) == 0);
                // Legacy unassigned clock pairs still need a known scheduled start
                // before they can be described as perfectly attended.
                if (!completed && requiredSchedules.Count == 1 && actualStart.HasValue && scheduled != null &&
                    HRM.Helpers.AttendanceTiming.LateMinutes(actualStart.Value, scheduled.ScheduledStart) == 0 && lastCheckOut != null && lastCheckOut.Log.Date >= actualStart && lastCheckOut.Log.Date >= scheduled.ScheduledEnd &&
                    firstCheckIn?.Resolution?.WorkAssignmentId == null)
                    completed = true;


                // There were physical clock events but none of them
                // resulted in useful attendance.
                var isUnresolved =
                    !isOnLeave && eligibility[date].HasApprovedPosition && eligibility[date].HasActiveSchedule &&
                    dayLogs.Count > 0 &&
                    !isPresent &&
                    resolvedEvents.Count == 0 &&
                    unresolvedEvents.Count > 0;


                // Only official working days can be absent. Official shift patterns
                // override the normal Sunday–Thursday working week; supplemental duty does not.
                var isHoliday = holidayExceptions.Any(x => x.EffectiveDate.Date <= date && x.EffectiveEndDate.Date >= date &&
                    (x.TargetScope == "Organisation" || (x.TargetScope == "Individual" && x.TargetEntityId == individualId)))
                    || holidayEvents.Any(x => x.StartDateTime.Date <= date && x.EndDateTime.Date >= date);

                var isRestDay = eligibility[date].HasActiveSchedule && !eligibility[date].IsScheduledDay;
                var isAbsent =
                    eligibility[date].CanBeAbsent && !isRestDay && !isOnLeave && !isHoliday &&
                    !isPresent &&
                    !isUnresolved;


                var dutySchedules = attendanceSchedules
                    .Where(s => s.Purpose == "Duty" && s.CheckIn.HasValue && s.CheckOut.HasValue && s.CheckOut.Value > s.CheckIn.Value)
                    .ToList();
                decimal dutyWorkedHours = 0m;
                foreach (var ds in dutySchedules)
                {
                    var effIn = ds.CheckIn!.Value < ds.ScheduledStart ? ds.ScheduledStart : ds.CheckIn.Value;
                    var effOut = ds.CheckOut!.Value > ds.ScheduledEnd ? ds.ScheduledEnd : ds.CheckOut.Value;
                    if (effOut > effIn)
                    {
                        dutyWorkedHours += Math.Round((decimal)(effOut - effIn).TotalHours, 2);
                    }
                }

                var breakOuts = events
                    .Where(x => x.Resolution?.AttendanceClockTypeId == (int)AttendanceClockType.BreakOut || x.Resolution?.LogicalEventType == "OfficialBreakOut")
                    .OrderBy(x => x.Log.Date)
                    .Select(x => x.Log.Date)
                    .ToList();
                var breakIns = events
                    .Where(x => x.Resolution?.AttendanceClockTypeId == (int)AttendanceClockType.BreakIn || x.Resolution?.LogicalEventType == "OfficialBreakIn")
                    .OrderBy(x => x.Log.Date)
                    .Select(x => x.Log.Date)
                    .ToList();
                int officialBreakMinutes = 0;
                for (int bi = 0; bi < Math.Min(breakOuts.Count, breakIns.Count); bi++)
                {
                    if (breakIns[bi] > breakOuts[bi])
                    {
                        officialBreakMinutes += (int)Math.Round((breakIns[bi] - breakOuts[bi]).TotalMinutes);
                    }
                }

                var sharedEventCount = dayLogs.Count(l => l.AttendanceLogResolutions.Count(r => r.IsValid) > 1);
                var conflictCount = dayLogs.Count(l => l.AttendanceLogResolutions.Any(r =>
                    r.IsValid &&
                    (r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Conflict ||
                     string.Equals(r.ConfidenceState, "Conflict", StringComparison.OrdinalIgnoreCase))));

                // -----------------------------------------------------
                // Add day
                // -----------------------------------------------------
                var scheduledEndMax = requiredSchedules.Select(s => (DateTime?)s.ScheduledEnd).Max();
                var nowTime = DateTime.UtcNow.AddHours(5);
                var isMissingClock = !isAbsent && !isHoliday && !isRestDay && !isOnLeave &&
                    ((firstCheckIn != null && lastCheckOut == null && (date < nowTime.Date || nowTime >= (scheduledEndMax ?? date.AddHours(14))))
                     || (firstCheckIn == null && lastCheckOut != null));

                result.Add(
                    new YearlyAttendanceDayDto
                    {
                        HasActiveSchedule = eligibility[date].HasActiveSchedule,
                        IsLate = isLate,
                        LateMinutes = lateMinutes,
                        ScheduledStart = scheduled?.ScheduledStart,
                        ScheduledEnd = scheduledEndMax,
                        IsPerfectlyAttended = isPresent && !isLate && completed,
                        HasApprovedPosition = eligibility[date].HasApprovedPosition,
                        RequiresAttendance = eligibility[date].CanBeAbsent,
                        EligibilityMessage = eligibility[date].Message,
                        LeaveTypeName = isOnLeave ? string.Join(", ", approvedLeaves.Where(x => x.FromDate.Date <= date && x.ToDate.Date >= date).Select(x => x.Type).Distinct()) : null,
                        Date =
                            date,

                        HasAttendanceLogs =
                            dayLogs.Count > 0,

                        ClockEventCount =
                            dayLogs.Count,

                        ResolvedEventCount =
                            resolvedEvents.Count,

                        UnresolvedEventCount =
                            unresolvedEvents.Count,

                        FirstCheckIn =
                            firstCheckIn?.Log.Date,

                        LastCheckOut =
                            lastCheckOut?.Log.Date,

                        WorkedHours =
                            workedHours,

                        DutyWorkedHours =
                            dutyWorkedHours,

                        OfficialBreakMinutes =
                            officialBreakMinutes,

                        SharedEventCount =
                            sharedEventCount,

                        ConflictCount =
                            conflictCount,

                        IsPresent =
                            isPresent,

                        IsAbsent =
                            isAbsent,

                        IsUnresolved =
                            isUnresolved,

                        IsRestDay =
                            eligibility[date].HasActiveSchedule && eligibility[date].HasApprovedPosition && isRestDay && !isOnLeave && !isPresent && !isUnresolved,

                        IsOnLeave =
                            isOnLeave,

                        IsHoliday =
                            isHoliday,

                        IsMissingClock =
                            isMissingClock
                    });
            }


            return result;
        }



    }
}
