using Microsoft.EntityFrameworkCore;
using HRM.Models; // Adjust this if your models are in a different folder
using HRM.DTOs; // For SharedConfig.OperationLogActionTypes
using HRM.Services.Interfaces;
using HRM.DTOs.Attendance;
using System.Threading;
using HRM.Services.Attendance.Abstraction;
namespace HRM.Services
{
    public class AttendanceLogService
    {
        // Now using your actual database context
        private readonly HrmTeContext _context;
        private readonly IOperationLogService _operationLogService;
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _access;
        private readonly IAttendanceLogProcessor _attendanceLogProcessor;
        private readonly UserSettingsService? _accounts;
        public AttendanceLogService(HrmTeContext context,
            IAttendanceLogProcessor attendanceLogProcessor,
            IOperationLogService operationLogService, 
            IDbContextFactory<HrmTeContext> dbFactory, IUserAccessService access, UserSettingsService? accounts = null)
        {
            _context = context;
            _operationLogService = operationLogService;
            _dbFactory = dbFactory;
            _access = access;
            _attendanceLogProcessor = attendanceLogProcessor;
            _accounts = accounts;
        }
       public async Task<List<TeamAttendanceDto>> GetMyTeamAttendanceTodayAsync()
        {
            var context = await _access.RequireContextAsync();
            using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateTime.Today;
            var organisationId = await _access.GetCurrentOrganisationIdAsync();

            // =====================================================
            // 1. Get teams where current user is supervisor
            // =====================================================

            var teamIds = await db.TeamStaffs
                .Where(x =>
                    x.StaffId == context.IndividualId &&
                    x.IsSuperVisor && x.Team.OrganisationId == organisationId &&
                    x.IsValid)
                .Select(x => x.TeamId)
                .Distinct()
                .ToListAsync();


            if (!teamIds.Any())
                return new List<TeamAttendanceDto>();


            // =====================================================
            // 2. Get all staff from those teams
            // =====================================================

            var teamStaff = await
            (
                from ts in db.TeamStaffs

                join t in db.Teams
                    on ts.TeamId equals t.TeamId

                join i in db.Individuals
                    on ts.StaffId equals i.BusinessEntityId


                where teamIds.Contains(ts.TeamId)
                      && ts.IsValid
                      && t.IsValid == true


                select new
                {
                    TeamID = t.TeamId,

                    TeamName = t.Name,

                    StaffID = ts.StaffId,

                    Name =
                        (i.FirstNameEnglish ?? "") + " " +
                        (i.MiddleNameEnglish ?? "") + " " +
                        (i.LastNameEnglish ?? "")
                }

            )
            .ToListAsync();



            // Remove duplicate staff if they belong to multiple teams
            teamStaff = teamStaff
                .GroupBy(x => x.StaffID)
                .Select(x => x.First())
                .ToList();



            if (!teamStaff.Any())
                return new List<TeamAttendanceDto>();



            // =====================================================
            // 3. Load today's attendance
            // =====================================================

            var staffIds = teamStaff
                .Select(x => x.StaffID)
                .ToList();


            var attendanceLogs = await db.AttendanceLogs

                .Where(x =>
                    staffIds.Contains(x.IndividualId)
                    && x.OrganisationId == organisationId && x.AttendanceLogStateId == 1
                    &&
                    x.Date >= today
                    &&
                    x.Date < today.AddDays(1)
                )

                .OrderBy(x => x.Date)

                .ToListAsync();




            // =====================================================
            // 4. Build result
            // =====================================================

            var eligibility = new Dictionary<int, HRM.Helpers.AttendanceEligibilityDay>();
            foreach (var person in staffIds)
                eligibility[person] = (await HRM.Helpers.AttendanceEligibilityCalendar.LoadAsync(db, person, today, today, organisationId))[today];
            var leavePeople = await db.Leaves.Where(l => staffIds.Contains(l.Job.IndividualID) && l.Job.OrganisationID == organisationId &&
                l.LeaveStateId == 4 && l.FromDate <= today && l.ToDate >= today).Select(l => l.Job.IndividualID).Distinct().ToListAsync();
            var result = teamStaff
                .Select(staff =>
                {

                    var logs = attendanceLogs
                        .Where(x => x.IndividualId == staff.StaffID)
                        .OrderBy(x => x.Date)
                        .ToList();



                    AttendanceStatus status;


                    if (leavePeople.Contains(staff.StaffID)) status = AttendanceStatus.Leave;
                    else if (!eligibility[staff.StaffID].HasActiveSchedule) status = AttendanceStatus.NoActiveSchedule;
                    else if (!eligibility[staff.StaffID].HasApprovedPosition) status = AttendanceStatus.BeforePositionApproval;
                    else if (!eligibility[staff.StaffID].CanBeAbsent) status = AttendanceStatus.RestDay;
                    else if (!logs.Any())
                    {
                        status = AttendanceStatus.Absent;
                    }

                    else if (logs.Count == 1)
                    {
                        status = AttendanceStatus.MissingCheckout;
                    }

                    else
                    {
                        // Change this according to your organisation time
                        var officeStart = new TimeSpan(8, 30, 0);


                        if (logs.First().Date.TimeOfDay > officeStart)
                        {
                            status = AttendanceStatus.Late;
                        }
                        else
                        {
                            status = AttendanceStatus.Present;
                        }
                    }



                    return new TeamAttendanceDto
                    {
                        StaffID = staff.StaffID,
                        StatusMessage = eligibility[staff.StaffID].Message,

                        TeamID = staff.TeamID,

                        TeamName = staff.TeamName,

                        Name = staff.Name
                            .Replace("  ", " ")
                            .Trim(),


                        CheckIn = logs
                            .FirstOrDefault()
                            ?.Date,


                        CheckOut = logs
                            .LastOrDefault()
                            ?.Date,


                        Status = status
                    };

                })
                .OrderBy(x => x.Name)
                .ToList();



            return result;
        }
        public async Task AddClockEventAsync(
            int individualId,
            DateTime? checkTime = null)
        {
            var clockTime =
                checkTime ?? DateTime.Now;

            var job =
                await _context.Jobs
                    .AsNoTracking()
                    .Where(x =>
                        x.IndividualID == individualId &&
                        x.TerminatedDate == null)
                    .OrderByDescending(x => x.JobId)
                    .FirstOrDefaultAsync();

            if (job == null)
            {
                throw new InvalidOperationException(
                    "Active job profile not found.");
            }

            const int automaticInOutModeId = 3;
            const int attendanceLogModeId = 2;
            const int attendanceLogStateId = 1;

            var log =
                new AttendanceLog
                {
                    IndividualId =
                        individualId,

                    OrganisationId =
                        job.OrganisationID,

                    OrganisationStructureId =
                        job.OrganisationStructureId,

                    InOutModeId =
                        automaticInOutModeId,

                    Date =
                        clockTime,

                    Year =
                        clockTime.Year,

                    Month =
                        clockTime.Month,

                    Day =
                        clockTime.Day,

                    Hour =
                        clockTime.Hour,

                    Minute =
                        clockTime.Minute,

                    Second =
                        clockTime.Second,

                    Uidstamp = $"{individualId}-{clockTime:yyyyMMddHHmmssfff}",

                    AttendanceLogStateId = attendanceLogStateId,

                    AttendanceLogModeId = attendanceLogModeId,

                    OperationLogId = 1
                };
            _context.ChangeTracker.Clear();
            await _context.AttendanceLogs.AddAsync(log);
            await _context.SaveChangesAsync();


            var processingResult = await _attendanceLogProcessor.ProcessAsync(log.AttendanceLogId);


        }
 


        [Obsolete("Use AddClockEventAsync so official and duty events share the same clock processing.")]
        public Task AddAttendanceCheckAsync(int individualId, int mode) => AddClockEventAsync(individualId);
        public async Task<List<AttendanceLog>> GetAttendanceLogsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.AttendanceLogs
                .Include(x => x.AttendanceLogResolutions)
                // 1. Filter by the user (Update 'IndividualId' to match your actual Foreign Key property name)
                .Where(log => log.IndividualId == userId && log.AttendanceLogStateId == 1)

                // 2. Filter by the date range. 
                // We use .Date to strip out the time component and ensure accurate inclusive boundaries.
                .Where(log => log.Date.Date >= startDate.Date && log.Date.Date <= endDate.Date)

                // 3. Order chronologically so the frontend processes the "LastOrDefault" correctly
                .OrderBy(log => log.Date)

                // 4. Execute the query asynchronously
                .ToListAsync();
        }

        /// <summary>
        /// Updates the user's basic profile information in the database.
        /// </summary>

        [Obsolete("Use UserSettingsService.SaveAccountAsync.")]
        public async Task<bool> UpdateUserAsync(User updatedUser)
        {
            if (_accounts == null) throw new InvalidOperationException("Use Manage Account to update accounts.");
            return (await _accounts.SaveLegacyUserAsync(updatedUser)).Success;
        }
        public async Task<List<JobLeaveType>> GetUserLeaveBalancesAsync(string identifier)
        {
            return await _context.Users
                .Where(u => u.Username == identifier) // Find the user
                .SelectMany(u => u.Jobs) // Jump into their jobs
                .Where(j => j.JobStateId == 4) // Optional: Uncomment this to only check ACTIVE jobs
                .SelectMany(j => j.JobLeaveTypes) // Jump into the leave types for those jobs
                .Where(jlt => jlt.IsValid == true) // Only get valid balances
                .Where(jlt => jlt.IsLeaveInfoUpdated == true) // Only get valid balances
                .Include(jlt => jlt.LeaveType) // Bring in the Leave Name (Annual, Sick, etc.)
                .ToListAsync();
        }

   
        public async Task<List<AttendanceLog>> GetUnevenLogsAsync(int individualId)
        {
            // Define the salary period dates (15th and 16th of current month)
            int year = DateTime.Today.Year;
            int month = DateTime.Today.Month;
            //DateTime start = new DateTime(year, month, 15);
            //DateTime end = new DateTime(year, month, 16);

            DateTime start = new DateTime(year, 4, 15);
            DateTime end = new DateTime(year, 5, 16);

            // Fetch all logs for this specific period
            var logs = await _context.AttendanceLogs
                .Where(a => a.IndividualId == individualId && a.Date >= start && a.Date <= end)
                .ToListAsync();

            var uneven = new List<AttendanceLog>();

            // Logic: Group by date to check for daily anomalies
            var dailyGroups = logs.GroupBy(l => l.Date.Date);

            foreach (var group in dailyGroups)
            {
                var checkIn = group.FirstOrDefault(l => l.InOutModeId == 1);
                var checkOut = group.FirstOrDefault(l => l.InOutModeId == 2);

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


        public async Task<List<Leaf>> GetLeaveRequestsAsync(int jobId)
        {
            return await _context.Leaves
             .Include(l => l.LeaveType)  // Fetches the Type name
             .Include(l => l.LeaveState) // Fetches the Status name
             .Where(l => l.JobId == jobId)
             .Where(s => s.LeaveStateId != 4)
             .OrderByDescending(l => l.FromDate)
             .ToListAsync();
        }

  
        public  class AttendanceAddViewModel
        {
             public int JobID { get; set; }

            public int InOutModeID { get; set; }
 
            public DateTime DateForLog { get; set; }
 
            public DateTime Time { get; set; }
 
            public string Comments { get; set; }
 
            public int RelatedAttendanceLogID { get; set; }


        }
    }
}
