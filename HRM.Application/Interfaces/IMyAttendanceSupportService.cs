using HRM.DTOs.Attendance;
namespace HRM.Services.Interfaces;
public interface IMyAttendanceSupportService
{
    Task<MyAttendanceSupport> LoadAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
