using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/activity-logs")]
public sealed class ActivityLogsController(
    IActivityLogQueryService queryService,
    IActivityContextAccessor contextAccessor) : HrmApiController
{
    [HttpGet("my")]
    public async Task<ActionResult<ActivityLogPageResult>> GetMyRecent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            return Ok(await queryService.GetMyRecentAsync(new ActivityLogFilter { Page = page, PageSize = pageSize }, actor, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ActivityLogPageResult>> Search(
        [FromQuery] int? organisationId = null,
        [FromQuery] int? divisionId = null,
        [FromQuery] int? teamId = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? activityType = null,
        [FromQuery] string? module = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetEntityType = null,
        [FromQuery] string? targetEntityId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string sortBy = "OccurredAtUtc",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        var filter = new ActivityLogFilter
        {
            OrganisationId = organisationId,
            DivisionId = divisionId,
            TeamId = teamId,
            UserId = userId,
            Category = category,
            ActivityType = activityType,
            Module = module,
            Action = action,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            CorrelationId = correlationId,
            SearchText = search,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize
        };

        try
        {
            return Ok(await queryService.SearchAsync(filter, actor, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("{activityLogId:long}")]
    public async Task<ActionResult<ActivityLogDto>> GetById(
        long activityLogId,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            var entry = await queryService.GetByIdAsync(activityLogId, actor, ct);
            if (entry is null)
                return NotFound();
            return Ok(entry);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("employee/{userId:int}")]
    public async Task<ActionResult<ActivityLogPageResult>> GetEmployeeTimeline(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            return Ok(await queryService.GetEmployeeTimelineAsync(userId, new ActivityLogFilter { Page = page, PageSize = pageSize }, actor, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("organisation/{organisationId:int}")]
    public async Task<ActionResult<ActivityLogPageResult>> GetOrganisationActivities(
        int organisationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            return Ok(await queryService.GetOrganisationActivitiesAsync(organisationId, new ActivityLogFilter { Page = page, PageSize = pageSize }, actor, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("team/{teamId:int}")]
    public async Task<ActionResult<ActivityLogPageResult>> GetTeamActivities(
        int teamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            return Ok(await queryService.GetTeamActivitiesAsync(teamId, new ActivityLogFilter { Page = page, PageSize = pageSize }, actor, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<ActivityLogPageResult>> GetEntityActivities(
        string entityType,
        string entityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            return Ok(await queryService.GetEntityActivitiesAsync(entityType, entityId, new ActivityLogFilter { Page = page, PageSize = pageSize }, actor, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("export")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(HrmRateLimitPolicies.Export)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] int? organisationId = null,
        [FromQuery] int? divisionId = null,
        [FromQuery] int? teamId = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? activityType = null,
        [FromQuery] string? module = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetEntityType = null,
        [FromQuery] string? targetEntityId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        var filter = new ActivityLogFilter
        {
            OrganisationId = organisationId,
            DivisionId = divisionId,
            TeamId = teamId,
            UserId = userId,
            Category = category,
            ActivityType = activityType,
            Module = module,
            Action = action,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            CorrelationId = correlationId,
            SearchText = search,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };

        try
        {
            var export = await queryService.ExportCsvAsync(filter, actor, ct);
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
