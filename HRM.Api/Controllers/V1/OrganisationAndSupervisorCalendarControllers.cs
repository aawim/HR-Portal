using HRM.Api.Infrastructure;
using HRM.DTOs.Calendar;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-calendar")]
public sealed class OrganisationCalendarController(global::HRM.Services.Calendar.Contracts.OrganisationCalendarServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("resolve-effective-workday")]
    public async Task<ActionResult<EffectiveWorkdayResolutionDto>> ResolveEffectiveWorkday([FromQuery] DateTime date, [FromQuery] int? individualId = null)
        => Ok(await service.ResolveEffectiveWorkdayAsync(date, individualId));

    [HttpGet("month-summary")]
    public async Task<ActionResult<List<OrganisationCalendarDaySummaryDto>>> GetOrganisationCalendarMonth([FromQuery] DateTime month)
        => Ok(await service.GetOrganisationCalendarMonthAsync(month));

    [HttpGet("day-detail")]
    public async Task<ActionResult<OrganisationCalendarDaySummaryDto>> GetCalendarDayDetail([FromQuery] DateTime date)
        => Ok(await service.GetCalendarDayDetailAsync(date));
}

[Route("api/v1/supervisor-calendar")]
public sealed class SupervisorCalendarController(global::HRM.Services.Calendar.Contracts.SupervisorCalendarServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("teams")]
    public async Task<ActionResult<List<SupervisorTeamOptionDto>>> GetSupervisedTeams()
        => Ok(await service.GetSupervisedTeamsAsync());

    [HttpGet("schedules")]
    public async Task<ActionResult<List<SupervisorCalendarScheduleItemDto>>> GetTeamSchedules([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? teamId = null, [FromQuery] int? staffId = null)
        => Ok(await service.GetTeamSchedulesAsync(from, to, teamId, staffId));

    [HttpGet("tasks")]
    public async Task<ActionResult<List<SupervisorCalendarTaskDto>>> GetTasks([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? teamId = null)
        => Ok(await service.GetTasksAsync(from, to, teamId));

    [HttpPost("save-task")]
    public async Task<ActionResult<int>> SaveTask([FromBody] SaveTaskRequest request)
        => Ok(await service.SaveTaskAsync(request.task));

    [HttpPost("update-task-status")]
    public async Task<IActionResult> UpdateTaskStatus([FromBody] UpdateTaskStatusRequest request)
    {
        await service.UpdateTaskStatusAsync(request.taskId, request.status);
        return NoContent();
    }

    [HttpPost("delete-task")]
    public async Task<IActionResult> DeleteTask([FromBody] DeleteTaskRequest request)
    {
        await service.DeleteTaskAsync(request.taskId);
        return NoContent();
    }

    public sealed class SaveTaskRequest { public SupervisorCalendarTaskDto task { get; set; } = new(); }
    public sealed class UpdateTaskStatusRequest { public int taskId { get; set; } public string status { get; set; } = ""; }
    public sealed class DeleteTaskRequest { public int taskId { get; set; } }
}

[Route("api/v1/calendar-impact")]
public sealed class CalendarImpactController(global::HRM.Services.Calendar.Contracts.CalendarImpactServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("assessments")]
    public async Task<ActionResult<List<LeaveImpactAssessmentDto>>> GetAssessmentsForException([FromQuery] int exceptionId)
        => Ok(await service.GetAssessmentsForExceptionAsync(exceptionId));

    [HttpPost("assess")]
    public async Task<IActionResult> AssessImpactForException([FromBody] AssessRequest request)
    {
        await service.AssessImpactForExceptionAsync(request.exceptionId);
        return NoContent();
    }

    [HttpPost("act")]
    public async Task<IActionResult> ActOnAssessment([FromBody] ActRequest request)
    {
        await service.ActOnAssessmentAsync(request.assessmentId, request.action);
        return NoContent();
    }

    public sealed class AssessRequest { public int exceptionId { get; set; } }
    public sealed class ActRequest { public int assessmentId { get; set; } public string action { get; set; } = ""; }
}
