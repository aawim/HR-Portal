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


        public AttendanceService(IDbContextFactory<HrmTeContext> factory, IAttendanceLogDataLoader loader, IUserAccessService userAccessService, IAttendancePlanService attendancePlanService)
        {
            _dbFactory = factory;
            _loader = loader;
            _userAccessService = userAccessService;
            _attendancePlanService = attendancePlanService;
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
                currentWeekStart.AddDays(7);

            // ---------------------------------------------------------
            // Previous week
            // ---------------------------------------------------------
            var previousWeekStart =
                currentWeekStart.AddDays(-7);

            var previousWeekEnd =
                currentWeekStart;

            // We only need two weeks of logs.
            var logs = await db.AttendanceLogs
                .AsNoTracking()
                .Include(x => x.AttendanceLogResolutions)
                .Where(x =>
                    x.IndividualId == userContext.IndividualId &&
                    x.Date >= previousWeekStart &&
                    x.Date < currentWeekEnd)
                .OrderBy(x => x.Date)
                .ToListAsync();

            var currentWeekHours =
                CalculateWorkedHours(
                    logs,
                    currentWeekStart,
                    currentWeekEnd);

            var previousWeekHours =
                CalculateWorkedHours(
                    logs,
                    previousWeekStart,
                    previousWeekEnd);

            return new WeeklyWorkedHoursDto
            {
                CurrentWeekHours =
                    Math.Round(currentWeekHours, 2),

                PreviousWeekHours =
                    Math.Round(previousWeekHours, 2)
            };
        }



        private static decimal CalculateWorkedHours(
    List<AttendanceLog> logs,
    DateTime fromDate,
    DateTime toDate)
        {
            var periodLogs = logs
                .Where(x =>
                    x.Date >= fromDate &&
                    x.Date < toDate)
                .OrderBy(x => x.Date)
                .ToList();

            decimal totalHours = 0;

            // ---------------------------------------------------------
            // Process each day independently
            // ---------------------------------------------------------
            var days = periodLogs
                .GroupBy(x => x.Date.Date);

            foreach (var day in days)
            {
                var resolvedLogs = day
                    .Select(log =>
                    {
                        // Latest resolution wins
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
                    .Where(x => x.Resolution != null)
                    .OrderBy(x => x.Log.Date)
                    .ToList();

                // -----------------------------------------------------
                // Find resolved CheckIn
                // -----------------------------------------------------
                var checkIn = resolvedLogs
                    .FirstOrDefault(x =>
                        x.Resolution!.AttendanceClockTypeId ==
                        (int)AttendanceClockType.CheckIn);

                // -----------------------------------------------------
                // Find resolved CheckOut
                // -----------------------------------------------------
                var checkOut = resolvedLogs
                    .LastOrDefault(x =>
                        x.Resolution!.AttendanceClockTypeId ==
                        (int)AttendanceClockType.CheckOut);

                if (checkIn == null ||
                    checkOut == null)
                {
                    // Incomplete day.
                    // Don't invent worked hours.
                    continue;
                }

                if (checkOut.Log.Date <= checkIn.Log.Date)
                {
                    continue;
                }

                var worked =
                    checkOut.Log.Date -
                    checkIn.Log.Date;

                totalHours +=
                    (decimal)worked.TotalHours;
            }

            return totalHours;
        }

    }
}
