using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IAttendanceLogDataLoader _loader;
        private readonly IUserAccessService _userAccessService;
 

        public AttendanceService(IDbContextFactory<HrmTeContext> factory, IAttendanceLogDataLoader loader, IUserAccessService userAccessService )
        {
            _dbFactory = factory;
            _loader = loader;
            _userAccessService = userAccessService;
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



        //public async Task<List<AttendanceLog>> GetWeeklyAttendanceAsync()
        //{
        //    // 1. Get the user's IndividualID directly from their active Job
        //    var username = await _userContext.GetUsernameAsync();

        //    if (string.IsNullOrWhiteSpace(username))
        //        return null;


        //    await using var context = await _dbFactory.CreateDbContextAsync();

        //    individualId = await context.Users
        //        .Where(u => u.Username == username)
        //        .SelectMany(u => u.Jobs)
        //        .Select(j => j.IndividualID)
        //        .FirstOrDefaultAsync();

        //    if (individualId == 0)
        //    {
        //        return new List<AttendanceLog>();
        //    }

        //    // 2. Define the 7-day window
        //    var startOfWeek = DateTime.Today.AddDays(-7);

        //    // 3. Fetch the logs for that specific IndividualID
        //    return await context.AttendanceLogs
        //        .Where(a => a.IndividualId == individualId && a.Date >= startOfWeek)
        //        .OrderBy(a => a.Date)
        //        .ToListAsync();
        //}


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
    }
}
