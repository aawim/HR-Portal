// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-plan")]
public sealed class AttendancePlanController(global::HRM.Services.Attendance.AttendancePlan.IAttendancePlanService service) : HrmApiController
{
    [HttpGet("get-plan")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceWorkPlanDto?>> GetPlan([FromQuery] global::System.Int32 individualId, [FromQuery] global::System.Int32 jobId, [FromQuery] global::System.DateTime workDate)
    {
        return Ok(await service.GetPlanAsync(individualId, jobId, workDate, HttpContext.RequestAborted));
    }

    [HttpPost("get-or-generate")]
    public async Task<ActionResult<global::HRM.DTOs.Attendance.AttendanceWorkPlanDto?>> GetOrGenerate([FromBody] AttendancePlanGetOrGenerateRequest request)
    {
        return Ok(await service.GetOrGenerateAsync(request.individualId, request.jobId, request.organisationBusinessEntityId, request.attendanceTime, HttpContext.RequestAborted));
    }

    public sealed class AttendancePlanGetOrGenerateRequest
    {
        public global::System.Int32 individualId { get; set; }
        public global::System.Int32 jobId { get; set; }
        public global::System.Int32 organisationBusinessEntityId { get; set; }
        public global::System.DateTime attendanceTime { get; set; }
    }

}
