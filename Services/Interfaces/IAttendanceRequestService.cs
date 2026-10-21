using HRM.DTOs;
using HRM.DTOs.Attendance;

namespace HRM.Services.Interfaces;

public interface IAttendanceRequestService
{
    Task<AttendanceRequestDateInfoDto> GetDateInfoAsync(DateOnly date, int? originalLogId = null);
    Task<ServiceResult> SubmitAsync(AttendanceRequestApplicationDto model);
}
