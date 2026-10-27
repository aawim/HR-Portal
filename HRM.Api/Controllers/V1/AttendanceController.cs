// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance")]
public sealed class AttendanceController(global::HRM.Services.Interfaces.IAttendanceService service) : HrmApiController
{
    [HttpGet("get-my-yearly-attendance")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.YearlyAttendanceDayDto>>> GetMyYearlyAttendance([FromQuery] global::System.Int32 year)
    {
        return Ok(await service.GetMyYearlyAttendanceAsync(year, HttpContext.RequestAborted));
    }

    [HttpGet("get-attendance-directory")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceDirectoryDto>> GetAttendanceDirectory()
    {
        return Ok(await service.GetAttendanceDirectoryAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-staff-schedule-attendance")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.StaffScheduleAttendanceDto>>> GetStaffScheduleAttendance([FromQuery] global::System.Int32 staffId, [FromQuery] global::System.DateTime from, [FromQuery] global::System.DateTime to)
    {
        return Ok(await service.GetStaffScheduleAttendanceAsync(staffId, from, to, HttpContext.RequestAborted));
    }

    [HttpGet("get-staff-attendance-range")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.YearlyAttendanceDayDto>>> GetStaffAttendanceRange([FromQuery] global::System.Int32 staffId, [FromQuery] global::System.DateTime from, [FromQuery] global::System.DateTime to)
    {
        return Ok(await service.GetStaffAttendanceRangeAsync(staffId, from, to, HttpContext.RequestAborted));
    }

    [HttpGet("get-staff-yearly-attendance")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.YearlyAttendanceDayDto>>> GetStaffYearlyAttendance([FromQuery] global::System.Int32 staffId, [FromQuery] global::System.Int32 year)
    {
        return Ok(await service.GetStaffYearlyAttendanceAsync(staffId, year, HttpContext.RequestAborted));
    }

    [HttpGet("get-staff-clock-events")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.StaffAttendanceClockEventDto>>> GetStaffClockEvents([FromQuery] global::System.Int32 staffId, [FromQuery] global::System.DateTime from, [FromQuery] global::System.DateTime to)
    {
        return Ok(await service.GetStaffClockEventsAsync(staffId, from, to, HttpContext.RequestAborted));
    }

    [HttpGet("get-my-weekly-attendance")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.AttendanceLogDto>>> GetMyWeeklyAttendance()
    {
        return Ok(await service.GetMyWeeklyAttendanceAsync());
    }

    [HttpGet("get-logs")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.AttendanceRawLogDto>>> GetLogs([FromQuery] global::System.Int32 individualId, [FromQuery] global::System.Int32 jobId, [FromQuery] global::System.DateTime checkTime)
    {
        return Ok(await service.GetLogsAsync(individualId, jobId, checkTime, HttpContext.RequestAborted));
    }

    [HttpGet("get-today-status")]
    public async Task<ActionResult<global::System.ValueTuple<global::HRM.DTOs.Attendance.AttendanceLogDto?, global::HRM.DTOs.Attendance.AttendanceLogDto?>>> GetTodayStatus([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetTodayStatusAsync(individualId));
    }

    [HttpGet("get-my-uneven-logs")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Attendance.AttendanceLogDto>>> GetMyUnevenLogs()
    {
        return Ok(await service.GetMyUnevenLogsAsync());
    }

    [HttpGet("get-my-weekly-worked-hours")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.WeeklyWorkedHoursDto>> GetMyWeeklyWorkedHours()
    {
        return Ok(await service.GetMyWeeklyWorkedHoursAsync());
    }

    [HttpGet("get-evaluation")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceDailyEvaluationDto>> GetEvaluation([FromQuery] global::System.Int32 individualId, [FromQuery] global::System.Int32 jobId, [FromQuery] global::System.Int32 organisationBusinessEntityId, [FromQuery] global::System.DateTime workDate)
    {
        return Ok(await service.GetEvaluationAsync(individualId, jobId, organisationBusinessEntityId, workDate, HttpContext.RequestAborted));
    }

}
