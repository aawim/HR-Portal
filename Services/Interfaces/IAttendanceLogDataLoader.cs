using HRM.DTOs;
using HRM.DTOs.Attendance;

namespace HRM.Services.Interfaces
{
    public interface IAttendanceLogDataLoader
    {
        Task<AttendanceLogDto?> GetEarliestCheckInAsync(int individualId, DateTime date);
        Task<AttendanceLogDto?> GetLatestCheckOutAsync(int individualId, DateTime date);
        Task<AttendanceLogDto?> GetLogsAsync(int individualId, DateTime from, DateTime to);


        // To be implimented
        //Task<List<AttendanceLogDto>> GetMyUnevenLogsAsync(int individualId, DateTime from, DateTime to);




    }
}
