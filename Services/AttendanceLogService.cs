using Microsoft.EntityFrameworkCore;
using HRM.Models; // Adjust this if your models are in a different folder
using HRM.DTOs; // For SharedConfig.OperationLogActionTypes
using HRM.Services.Interfaces;
using HRM.DTOs.Attendance;
namespace HRM.Services
{
    public class AttendanceLogService
    {
        // Now using your actual database context
        private readonly HrmTeContext _context;
        private readonly IOperationLogService _operationLogService;
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _access;

        public AttendanceLogService(HrmTeContext context, IOperationLogService operationLogService, IDbContextFactory<HrmTeContext> dbFactory, IUserAccessService access)
        {
            _context = context;
            _operationLogService = operationLogService;
            _dbFactory = dbFactory;
            _access = access;
        }
       public async Task<List<TeamAttendanceDto>> GetMyTeamAttendanceTodayAsync()
        {
            var context = await _access.RequireContextAsync();
            using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateTime.Today;

            // =====================================================
            // 1. Get teams where current user is supervisor
            // =====================================================

            var teamIds = await db.TeamStaffs
                .Where(x =>
                    x.StaffId == context.IndividualId &&
                    x.IsSuperVisor &&
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

            var result = teamStaff
                .Select(staff =>
                {

                    var logs = attendanceLogs
                        .Where(x => x.IndividualId == staff.StaffID)
                        .OrderBy(x => x.Date)
                        .ToList();



                    AttendanceStatus status;


                    if (!logs.Any())
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

       
        public async Task AddAttendanceCheckAsync(int individualId, int mode)
        {
          
           var now = DateTime.Now;
            // Fetch job context
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.IndividualID == individualId);
            if (job == null) throw new Exception("Job profile not found.");
            var newLog = new AttendanceLog
            {
                IndividualId = individualId,
                OrganisationId = job.OrganisationID,
                InOutModeId = mode,
                Date = now,
                Year = now.Year,
                Month = now.Month,
                Day = now.Day,
                Hour = now.Hour,
                Minute = now.Minute,
                Second = now.Second,
                Uidstamp = $"{individualId}{mode:D2}{now:yyyyMMddHHmmss}",
                AttendanceLogStateId = 1,
                AttendanceLogModeId = mode,
                OrganisationStructureId = job.OrganisationStructureId,
                OperationLogId = 1 //_operationLogService.CreateAndSaveLogAsync("New Attendance Log remotely", SharedConfig.OperationLogActionTypes.ATTENDANCE_LOG_CREATE),
            };
            _context.ChangeTracker.Clear();
            await _context.AttendanceLogs.AddAsync(newLog);
            await _context.SaveChangesAsync();
        }
 
        /// <summary>
        /// Retrieves attendance logs for a specific user within a date range.
        /// </summary>
        public async Task<List<AttendanceLog>> GetAttendanceLogsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            return await _context.AttendanceLogs
                // 1. Filter by the user (Update 'IndividualId' to match your actual Foreign Key property name)
                .Where(log => log.IndividualId == userId)

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
        public async Task<bool> UpdateUserAsync(User updatedUser)
        {
            try
            {
                // Tells EF Core that this user object has been modified and needs to be saved
                _context.Users.Update(updatedUser);
                var changes = await _context.SaveChangesAsync();
                // Return true if at least one row was affected
                return changes > 0;
            }
            catch (Exception ex)
            {
                // In a real production app, log the 'ex.Message' here
                return false;
            }
        }

        /// <summary>
        /// Fetches valid leave balances by traversing from User -> Jobs -> JobLeaveTypes
        /// </summary>
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