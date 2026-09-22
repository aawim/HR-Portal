using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Attendance.AttendancePlan;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace HRM.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IAttendanceLogDataLoader _loader;
        private readonly IUserAccessService _userAccessService;
        private readonly IAttendancePlanService _attendancePlanService;

        private readonly ILogger<AttendanceService> _logger;


        public AttendanceService(IDbContextFactory<HrmTeContext> factory, IAttendanceLogDataLoader loader, IUserAccessService userAccessService, IAttendancePlanService attendancePlanService, ILogger<AttendanceService> logger)
        {
            _dbFactory = factory;
            _loader = loader;
            _userAccessService = userAccessService;
            _attendancePlanService = attendancePlanService;
            _logger = logger;

        }

        public async Task<(AttendanceLogDto? checkIn, AttendanceLogDto? checkOut)> GetTodayStatusAsync(int individualId)
        {
            var today = DateTime.Today;

            var checkIn = await _loader.GetEarliestCheckInAsync(individualId, today);
            var checkOut = await _loader.GetLatestCheckOutAsync(individualId, today);

            return (checkIn, checkOut);
        }

        public Task AddAttendanceCheckAsync(int individualId, DateTime checkTime, int checkType)
        {
            using var context = _dbFactory.CreateDbContext();

            throw new NotImplementedException();
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



        public async Task<WeeklyWorkedHoursDto> GetMyWeeklyWorkedHoursAsync()
        {
            var userContext =
                await _userAccessService.RequireContextAsync();

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var today = DateTime.Today;

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
                x.Date >= previousWeekStart &&
                x.Date < currentWeekEndExclusive)
            .OrderBy(x => x.Date)
            .ToListAsync();




            foreach (var log in logs)
            {
                var resolution = log.AttendanceLogResolutions
                    .OrderByDescending(x =>
                        x.AttendanceLogResolutionId)
                    .FirstOrDefault();
 
            }


            var currentWeekHours =
                CalculateWorkedHours(
                    logs,
                    currentWeekStart,
                    DateTime.Now);

            var previousWeekHours =
                CalculateWorkedHours(
                    logs,
                    previousWeekStart,
                    previousWeekEndExclusive);





            return new WeeklyWorkedHoursDto
            {
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




        public async Task<List<YearlyAttendanceDayDto>>
        GetMyYearlyAttendanceAsync(
            int year,
            CancellationToken cancellationToken = default)
        {
            // ---------------------------------------------------------
            // Validate year
            // ---------------------------------------------------------
            if (year < 2000 || year > DateTime.Today.Year)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Invalid attendance year.");
            }


            // ---------------------------------------------------------
            // Current user
            // ---------------------------------------------------------
            var userContext =
                await _userAccessService.RequireContextAsync();


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


            // ---------------------------------------------------------
            // Do not include future attendance
            // ---------------------------------------------------------
            var queryEnd =
                year == DateTime.Today.Year
                    ? DateTime.Now
                    : yearEndExclusive;


            // ---------------------------------------------------------
            // Load attendance logs
            // ---------------------------------------------------------
            var logs = await db.AttendanceLogs
                .AsNoTracking()
                .Include(x =>
                    x.AttendanceLogResolutions)
                .Where(x =>
                    x.IndividualId ==
                        userContext.IndividualId &&

                    x.Date >= yearStart &&

                    x.Date < queryEnd)
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);


            // ---------------------------------------------------------
            // Group logs by calendar date
            //
            // NOTE:
            // For the yearly overview, calendar-date grouping is fine.
            //
            // Worked-hour calculations may later need WorkPlan grouping
            // for overnight assignments.
            // ---------------------------------------------------------
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
                year == DateTime.Today.Year
                    ? DateTime.Today
                    : new DateTime(
                        year,
                        12,
                        31);


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

                if (firstCheckIn != null &&
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

                // Presence is determined by successfully resolved
                // CheckIn.
                var isPresent =
                    firstCheckIn != null;


                // There were physical clock events but none of them
                // resulted in useful attendance.
                var isUnresolved =
                    dayLogs.Count > 0 &&
                    !isPresent &&
                    resolvedEvents.Count == 0 &&
                    unresolvedEvents.Count > 0;


                /*
                 * TEMPORARY ABSENCE RULE
                 *
                 * At this stage we do not yet know whether the employee
                 * was actually scheduled to work.
                 *
                 * Eventually absence MUST come from WorkPlan:
                 *
                 * Has required attendance WorkPlan
                 *        +
                 * no valid CheckIn
                 *        =
                 * absent
                 *
                 * For now this preserves the behaviour of the existing
                 * attendance summary.
                 */
                var isAbsent =
                    !isPresent &&
                    !isUnresolved;


                // -----------------------------------------------------
                // Add day
                // -----------------------------------------------------
                result.Add(
                    new YearlyAttendanceDayDto
                    {
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

                        // These will be implemented from the
                        // appropriate modules later.
                        IsRestDay =
                            false,

                        IsOnLeave =
                            false,

                        IsHoliday =
                            false
                    });
            }


            return result;
        }



    }
}
