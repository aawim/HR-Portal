
using HRM.Models;
 
using HRM.DTOs.Attendance;

namespace HRM.Services.Interfaces
{
    public interface IAttendanceService
    {


       Task <List<AttendanceLogDto>> GetMyWeeklyAttendanceAsync();

      
        Task AddAttendanceCheckAsync(int individualId, DateTime checkTime, int checkType);

        Task<(AttendanceLogDto? checkIn, AttendanceLogDto? checkOut)> GetTodayStatusAsync(int individualId);

        Task<List<AttendanceLogDto>> GetMyUnevenLogsAsync();


    }
}

