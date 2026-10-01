
using HRM.Models;
 
using HRM.DTOs.Attendance;

namespace HRM.Services.Interfaces
{
    public interface IAttendanceService
    {

        Task<List<YearlyAttendanceDayDto>>GetMyYearlyAttendanceAsync(int year, CancellationToken cancellationToken = default);
        Task <List<AttendanceLogDto>> GetMyWeeklyAttendanceAsync();

      
        Task AddAttendanceCheckAsync(int individualId, DateTime checkTime, int checkType);

        Task<List<AttendanceRawLogDto>> GetLogsAsync(
            int individualId,
            int jobId,
            DateTime checkTime,
            CancellationToken cancellationToken = default);

        Task<(AttendanceLogDto? checkIn, AttendanceLogDto? checkOut)> GetTodayStatusAsync(int individualId);

        Task<List<AttendanceLogDto>> GetMyUnevenLogsAsync();

        Task<WeeklyWorkedHoursDto>GetMyWeeklyWorkedHoursAsync();

        Task<AttendanceDailyEvaluationDto> GetEvaluationAsync(
            int individualId,
            int jobId,
            int organisationBusinessEntityId,
            DateTime workDate,
            CancellationToken cancellationToken = default);

    }
}

