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
            using var context = _dbFactory.CreateDbContext();

            // Define the salary period dates (15th and 16th of current month)
            int year = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            //DateTime start = new DateTime(year, month, 15);
            //DateTime end = new DateTime(year, month, 16);

            DateTime start = new DateTime(year, 4, 15);
            DateTime end = new DateTime(year, 5, 16);

            // Fetch all logs for this specific period
            var logs = await context.AttendanceLogs
                .AsNoTracking()
                .Where(a =>
                    a.IndividualId == individualId &&
                    a.Date >= start &&
                    a.Date <= end)
                .Select(a => new AttendanceLogDto
                {
                    AttendanceLogID = a.AttendanceLogId,
                    AttendanceDeviceID = a.AttendanceDeviceId,
                    IndividualID = a.IndividualId,
                    OrganisationID = a.OrganisationId,
                    OrganisationStructureID = a.OrganisationStructureId,
                    InOutModeID = a.InOutModeId,
                    Year = a.Year,
                    Month = a.Month,
                    Day = a.Day,
                    Hour = a.Hour,
                    Minute = a.Minute,
                    Second = a.Second,
                    Date = a.Date,
                    AttendanceLogModeID = a.AttendanceLogModeId,
                    AttendanceLogStateID = a.AttendanceLogStateId,
                    OperationLogID = a.OperationLogId,
                    RelatedAttendanceLogID = a.RelatedAttendanceLogId,
                    ActualInOutMode = a.ActualInOutMode
                })
                .ToListAsync();

            var uneven = new List<AttendanceLogDto>();

            // Logic: Group by date to check for daily anomalies
            var dailyGroups = logs.GroupBy(l => l.Date.Date);

            foreach (var group in dailyGroups)
            {
                var checkIn = group.FirstOrDefault(l => l.InOutModeID == 1);
                var checkOut = group.FirstOrDefault(l => l.InOutModeID == 2);

                // Condition 1: Late check-in (later than 08:00:00)
                if (checkIn != null && (checkIn.Hour > 8 || (checkIn.Hour == 8 && checkIn.Minute > 0)))
                {
                    uneven.Add(checkIn);
                }

                // Condition 2: Missing check-in OR missing check-out
                if (checkIn == null || checkOut == null)
                {
                    uneven.AddRange(group);
                }
            }

            return uneven;
        }
        public async Task<List<AttendanceLogDto>> GetMyWeeklyAttendanceAsync()
        {
            var context = await _userAccessService.RequireContextAsync();

            await using var db = await _dbFactory.CreateDbContextAsync();

            var startOfWeek = DateTime.Today.AddDays(-7);

            return await db.AttendanceLogs
                .AsNoTracking()
                .Where(a =>
                    a.IndividualId == context.IndividualId &&
                    a.Date >= startOfWeek)
                .OrderBy(a => a.Date)
                .Select(a => new AttendanceLogDto
                {
                    AttendanceLogID = a.AttendanceLogId,

                    IndividualID = a.IndividualId,

                    OrganisationID = a.OrganisationId,

                    OrganisationStructureID =
                        a.OrganisationStructureId,

                    InOutModeID =
                        a.InOutModeId,

                    Year = a.Year,

                    Month = a.Month,

                    Day = a.Day,

                    Hour = a.Hour,

                    Minute = a.Minute,

                    Second = a.Second,

                    Date = a.Date,

                    AttendanceLogModeID =
                        a.AttendanceLogModeId,

                    AttendanceLogStateID =
                        a.AttendanceLogStateId,

                    OperationLogID =
                        a.OperationLogId,

                    RelatedAttendanceLogID =
                        a.RelatedAttendanceLogId,

                    ActualInOutMode =
                        a.ActualInOutMode
                })
                .ToListAsync();
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

   







    }
}
