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
            var today = DateTime.Today;

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

            var today = DateTime.Today;

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
                            log.AttendanceLogResolutions.Any(r =>
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
                                log.AttendanceLogResolutions.Any(r =>
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
                DateTime.Today.AddDays(-6);

            var endDate =
                DateTime.Today.AddDays(1);

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
                    var resolution = log.AttendanceLogResolutions
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

            var logs =
        await (
            from log in db.AttendanceLogs.AsNoTracking()

            where
                log.IndividualId == individualId &&
                log.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
                log.Date >= date &&
                log.Date < nextDate

            join resolution in
                db.AttendanceLogResolutions.AsNoTracking()
                .Where(x => x.IsValid)

                on log.AttendanceLogId
                equals resolution.AttendanceLogId
                into resolutionGroup

            from resolution in
                resolutionGroup.DefaultIfEmpty()

            join segment in
                db.WorkPlanSegments.AsNoTracking()

                on resolution.WorkPlanSegmentId
                equals segment.WorkPlanSegmentId
                into segmentGroup

            from segment in
                segmentGroup.DefaultIfEmpty()

            orderby log.Date

            select new AttendanceRawLogDto
            {
                AttendanceLogId =
                    log.AttendanceLogId,

                IndividualId =
                    log.IndividualId,

                LogDateTime =
                    log.Date,

                AttendanceLogResolutionId =
                    resolution != null
                        ? resolution.AttendanceLogResolutionId
                        : null,

                WorkPlanId =
                        resolution != null
                            ? resolution.WorkPlanId
                            : null,

                WorkPlanSegmentId =
                        resolution != null
                            ? resolution.WorkPlanSegmentId
                            : null,

                SegmentName =
                    segment != null
                        ? segment.Name
                        : null,

                ClockType =
                    resolution != null &&
                    resolution.AttendanceClockTypeId.HasValue

                        ? (AttendanceClockType)
                            resolution.AttendanceClockTypeId.Value

                        : AttendanceClockType.Unresolved,

                ResolutionStatusId =
                    resolution != null
                        ? resolution.AttendanceResolutionStatusId
                        : null,

                ResolutionMessage =
                    resolution != null
                        ? resolution.ResolutionMessage
                        : null,

                ResolutionDate =
                    resolution != null
                        ? resolution.ResolutionDate
                        : null
            })
            .ToListAsync(cancellationToken);

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
            var userContext =
                await _userAccessService.RequireContextAsync();

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var today = DateTime.UtcNow.AddHours(5).Date;
            var organisation = await _userAccessService.GetCurrentOrganisationIdAsync();

            // ---------------------------------------------------------
            // Current week: Sunday -> Saturday
            // ---------------------------------------------------------
            var currentWeekStart =
                today.AddDays(-(int)today.DayOfWeek);

            var currentWeekEnd =
                currentWeekStart.AddDays(6);

            // Exclusive boundary for database queries
            var currentWeekEndExclusive =
                currentWeekStart.AddDays(7);

            // ---------------------------------------------------------
            // Previous week: Sunday -> Saturday
            // ---------------------------------------------------------
            var previousWeekStart =
                currentWeekStart.AddDays(-7);

            var previousWeekEnd =
                currentWeekStart.AddDays(-1);

            // Exclusive boundary
            var previousWeekEndExclusive =
                currentWeekStart;


            var logs = await db.AttendanceLogs
            .AsNoTracking()
            .Include(x => x.AttendanceLogResolutions)
            .Where(x =>
                x.IndividualId == userContext.IndividualId &&
                x.OrganisationId == organisation &&
                x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
                x.Date >= previousWeekStart &&
                x.Date < currentWeekEndExclusive)
            .OrderBy(x => x.Date)
            .ToListAsync();




            var eligibility = await HRM.Helpers.AttendanceEligibilityCalendar.LoadAsync(db, userContext.IndividualId,
                previousWeekStart, currentWeekEnd, organisation);
            logs = logs.Where(x => eligibility[x.Date.Date].HasApprovedPosition && eligibility[x.Date.Date].HasActiveSchedule).ToList();
            var currentWeekHours =
                CalculateWorkedHours(
                    logs,
                    currentWeekStart,
                    DateTime.UtcNow.AddHours(5));

            var previousWeekHours =
                CalculateWorkedHours(
                    logs,
                    previousWeekStart,
                    previousWeekEndExclusive);





            var makeup = await HRM.Helpers.WeeklyAttendanceMakeup.LoadAsync(db, userContext.IndividualId, organisation, currentWeekStart, currentWeekEndExclusive);
            return new WeeklyWorkedHoursDto
            {
                WeekStart = currentWeekStart,
                WeekEnd = currentWeekEnd,
                ApprovedEarlyCheckoutShortfallHours = makeup.ShortfallHours,
                MakeupCompletedHours = makeup.CompletedHours,
                MakeupRemainingHours = makeup.RemainingHours,
                CurrentWeekHours =
                    Math.Round(currentWeekHours, 2),

                PreviousWeekHours =
                    Math.Round(previousWeekHours, 2)
            };
        }


        private decimal CalculateWorkedHours(
            List<AttendanceLog> logs,
            DateTime fromDate,
            DateTime toDate)
        {
            decimal totalMinutes = 0;

            // ---------------------------------------------------------
            // Get all resolved events in the requested period
            // ---------------------------------------------------------
            var resolvedEvents = logs
                .Where(x =>
                    x.Date >= fromDate &&
                    x.Date <= toDate)
                .SelectMany(log =>
                {
                    var resolution = log.AttendanceLogResolutions
                        .OrderByDescending(x =>
                            x.AttendanceLogResolutionId)
                        .Take(1);

                    return resolution.Select(r => new
                    {
                        Log = log,
                        Resolution = r
                    });
                })
                .Where(x =>
                    x.Resolution.WorkPlanId > 0)
                .OrderBy(x => x.Log.Date)
                .ToList();

            // ---------------------------------------------------------
            // Group by WorkPlan instead of calendar date
            // ---------------------------------------------------------
            var workPlans = resolvedEvents
                .GroupBy(x => x.Resolution.WorkPlanId)
                .ToList();

            foreach (var workPlan in workPlans)
            {
                var events = workPlan
                    .OrderBy(x => x.Log.Date)
                    .ToList();

                // -----------------------------------------------------
                // First valid CheckIn
                // -----------------------------------------------------
                var checkIn = events
                    .FirstOrDefault(x =>
                        x.Resolution.AttendanceClockTypeId ==
                        (int)AttendanceClockType.CheckIn);

                if (checkIn == null)
                {
                    continue;
                }

                // -----------------------------------------------------
                // Last CheckOut AFTER CheckIn
                // -----------------------------------------------------
                var checkOut = events
                    .Where(x =>
                        x.Resolution.AttendanceClockTypeId ==
                            (int)AttendanceClockType.CheckOut &&
                        x.Log.Date > checkIn.Log.Date)
                    .OrderByDescending(x => x.Log.Date)
                    .FirstOrDefault();

                if (checkOut == null)
                {
                    continue;
                }

                // -----------------------------------------------------
                // Calculate duration
                // -----------------------------------------------------
                var workedMinutes =
                    (checkOut.Log.Date - checkIn.Log.Date)
                    .TotalMinutes;

                if (workedMinutes <= 0)
                {
                    continue;
                }

                totalMinutes +=
                    (decimal)workedMinutes;
            }

            return totalMinutes / 60m;
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
            var logs = await db.AttendanceLogs.AsNoTracking().Include(x => x.AttendanceLogResolutions).ThenInclude(x => x.WorkAssignment)
                .Where(x => x.IndividualId == staffId && x.OrganisationId == org && x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid && x.Date >= from.Date && x.Date < end)
                .OrderByDescending(x => x.Date).Take(1000).ToListAsync(cancellationToken);
            return logs.Select(x => {
                var resolution = x.AttendanceLogResolutions.Where(r => r.IsValid).OrderByDescending(r => r.AttendanceLogResolutionId).FirstOrDefault();
                var code = resolution?.AttendanceClockTypeId ?? (x.AttendanceLogResolutions.Count == 0 ? x.InOutModeId : null);
                var label = code switch { 1 => "Check in", 2 => "Check out", 3 => "Break out", 4 => "Break in", _ => "Unresolved" };
                return new StaffAttendanceClockEventDto(x.Date, label, resolution?.WorkAssignment == null ? "—" : $"{resolution.WorkAssignment.SchedulePurpose} · {resolution.WorkAssignment.Name}");
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
            var scheduleFacts = await HRM.Helpers.AttendanceScheduleFacts.LoadAsync(db, individualId,
                yearStart, queryEnd.Date, organisationId, cancellationToken, logs);
            var schedulesByDay = scheduleFacts.GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.ToList());

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
                var events = dayLogs
                    .Select(log =>
                    {
                        var resolution =
                            log.AttendanceLogResolutions
                                .OrderByDescending(x =>
                                    x.AttendanceLogResolutionId)
                                .FirstOrDefault();

                        return new
                        {
                            Log = log,
                            Resolution = resolution
                        };
                    })
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
                                .AttendanceClockTypeId !=
                            (int)AttendanceClockType.Ignored
                        ))
                    .ToList();


                // -----------------------------------------------------
                // First Check In
                // -----------------------------------------------------
                var firstCheckIn =
                    checkIns.FirstOrDefault();


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
                var daySchedules = schedulesByDay.GetValueOrDefault(date) ?? [];
                var official = daySchedules.Where(s => s.RequiresAttendance && s.Purpose == "Official").ToList();
                var scheduled = (official.Count > 0 ? official : daySchedules.Where(s => s.RequiresAttendance))
                    .OrderBy(s => s.ScheduledStart).FirstOrDefault();
                var actualStart = scheduled?.CheckIn ?? (firstCheckIn?.Resolution?.WorkAssignmentId == null ? firstCheckIn?.Log.Date : null);
                var lateUntil = actualStart ?? (date == DateTime.UtcNow.AddHours(5).Date ? DateTime.UtcNow.AddHours(5) : (DateTime?)null);
                var isLate = !isOnLeave && eligibility[date].CanBeAbsent && scheduled != null &&
                    lateUntil.HasValue && lateUntil.Value > scheduled.ScheduledStart;
                var lateMinutes = isLate ? (int)Math.Ceiling((lateUntil!.Value - scheduled!.ScheduledStart).TotalMinutes) : 0;

                // Presence is determined by successfully resolved CheckIn.
                var isPresent =
                    !isOnLeave && eligibility[date].HasApprovedPosition && eligibility[date].HasActiveSchedule && firstCheckIn != null;
                var requiredSchedules = daySchedules.Where(s => s.RequiresAttendance).ToList();
                var recordedLateness = requiredSchedules.Where(s => s.CheckIn.HasValue && s.CheckIn.Value > s.ScheduledStart)
                    .Select(s => (int)Math.Ceiling((s.CheckIn!.Value - s.ScheduledStart).TotalMinutes)).ToList();
                if (isPresent && recordedLateness.Count > 0)
                {
                    isLate = true;
                    lateMinutes = recordedLateness.Sum();
                }
                else if (isPresent && !actualStart.HasValue) { isLate = false; lateMinutes = 0; }
                var completed = requiredSchedules.Count > 0 && requiredSchedules.All(s => s.CheckIn.HasValue && s.CheckOut.HasValue &&
                    s.CheckOut >= s.CheckIn && s.CheckOut >= s.ScheduledEnd && s.CheckIn <= s.ScheduledStart);
                // Legacy unassigned clock pairs still need a known scheduled start
                // before they can be described as perfectly attended.
                if (!completed && requiredSchedules.Count == 1 && actualStart.HasValue && scheduled != null &&
                    actualStart <= scheduled.ScheduledStart && lastCheckOut != null && lastCheckOut.Log.Date >= actualStart && lastCheckOut.Log.Date >= scheduled.ScheduledEnd &&
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


                // Only working days can be absent. Explicit duty and shift patterns
                // override the normal Sunday–Thursday working week.
                var isRestDay = eligibility[date].HasActiveSchedule && !eligibility[date].IsScheduledDay;
                var isAbsent =
                    eligibility[date].CanBeAbsent && !isRestDay && !isOnLeave &&
                    !isPresent &&
                    !isUnresolved;


                // -----------------------------------------------------
                // Add day
                // -----------------------------------------------------
                result.Add(
                    new YearlyAttendanceDayDto
                    {
                        HasActiveSchedule = eligibility[date].HasActiveSchedule,
                        IsLate = isLate,
                        LateMinutes = lateMinutes,
                        ScheduledStart = scheduled?.ScheduledStart,
                        ScheduledEnd = requiredSchedules.Select(s => (DateTime?)s.ScheduledEnd).Max(),
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
                            false
                    });
            }


            return result;
        }



    }
}
