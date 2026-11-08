// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-log")]
public sealed class AttendanceLogController(global::HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-my-team-attendance-today")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamAttendanceDto>>> GetMyTeamAttendanceToday()
    {
        return Ok(await service.GetMyTeamAttendanceTodayAsync());
    }

    [HttpGet("can-use-test-clock")]
    public async Task<ActionResult<global::System.Boolean>> CanUseTestClock()
    {
        return Ok(await service.CanUseTestClockAsync());
    }

    [HttpPost("add-clock-event")]
    public async Task<IActionResult> AddClockEvent([FromBody] AttendanceLogAddClockEventRequest request)
    {
        await service.AddClockEventAsync(request.individualId, request.checkTime);
        return NoContent();
    }

    public sealed class AttendanceLogAddClockEventRequest
    {
        public global::System.Int32 individualId { get; set; }
        public global::System.DateTime? checkTime { get; set; } = null;
    }

    [HttpGet("get-attendance-logs")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.AttendanceLogDto>>> GetAttendanceLogs([FromQuery] global::System.Int32 userId, [FromQuery] global::System.DateTime startDate, [FromQuery] global::System.DateTime endDate)
    {
        return Ok(await service.GetAttendanceLogsAsync(userId, startDate, endDate));
    }

    [HttpGet("get-user-leave-balances")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetUserLeaveBalances([FromQuery] global::System.String identifier)
    {
        return Ok(await service.GetUserLeaveBalancesAsync(identifier));
    }

    [HttpGet("get-uneven-logs")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.AttendanceLogDto>>> GetUnevenLogs([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetUnevenLogsAsync(individualId));
    }

    [HttpGet("get-leave-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.LeafDto>>> GetLeaveRequests([FromQuery] global::System.Int32 jobId)
    {
        return Ok(await service.GetLeaveRequestsAsync(jobId));
    }

}
