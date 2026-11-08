using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.DTOs.Attendance;
using System.Threading;
namespace HRM.Services.Contracts;
public abstract class AttendanceLogServiceContracts
{

  
        public  class AttendanceAddViewModel
        {
             public int JobID { get; set; }

            public int InOutModeID { get; set; }
 
            public DateTime DateForLog { get; set; }
 
            public DateTime Time { get; set; }
 
            public string Comments { get; set; } = string.Empty;
 
            public int RelatedAttendanceLogID { get; set; }


        }

public interface IOperations
{
Task<List<TeamAttendanceDto>> GetMyTeamAttendanceTodayAsync()
;
Task<bool> CanUseTestClockAsync() ;
Task AddClockEventAsync(
            int individualId,
            DateTime? checkTime = null)
;
[Obsolete("Use AddClockEventAsync so official and duty events share the same clock processing.")]
Task AddAttendanceCheckAsync(int individualId, int mode) ;
Task<List<global::HRM.DTOs.Contracts.AttendanceLogDto>> GetAttendanceLogsAsync(int userId, DateTime startDate, DateTime endDate)
;

Task<List<global::HRM.DTOs.Leave.JobLeaveTypeDto>> GetUserLeaveBalancesAsync(string identifier)
;
Task<List<global::HRM.DTOs.Contracts.AttendanceLogDto>> GetUnevenLogsAsync(int individualId)
;
Task<List<global::HRM.DTOs.Contracts.LeafDto>> GetLeaveRequestsAsync(int jobId)
;
}
}
