using HRM.Api.Infrastructure;
using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Models;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/audit-logs")]
public sealed class AuditLogsController(
    IAuditLogQueryService queryService,
    IAuditContextAccessor contextAccessor,
    IAuditLogService auditWriter) : HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<AuditLogPageResult>> Search(
        [FromQuery] int? organisationId = null,
        [FromQuery] int? divisionId = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? module = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] bool includeDetails = false,
        [FromQuery] string sortBy = "OccurredAtUtc",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        var filter = new AuditLogFilter
        {
            OrganisationId = organisationId,
            DivisionId = divisionId,
            UserId = userId,
            Action = action,
            Outcome = outcome,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            CorrelationId = correlationId,
            SearchText = search,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            IncludeDetails = includeDetails,
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
            await auditWriter.RecordSecurityEventAsync(
                "AUDIT_ACCESS_DENIED", AuditOutcomes.Denied, "AuditLogs", "AuditLog",
                organisationId?.ToString() ?? "SEARCH", ex.Message, cancellationToken: ct);
            return Forbid();
        }
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AuditLogRecordDto>> GetById(long id, CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            var record = await queryService.GetByIdAsync(id, actor, ct);
            return record is null ? NotFound() : Ok(record);
        }
        catch (UnauthorizedAccessException ex)
        {
            await auditWriter.RecordSecurityEventAsync(
                "AUDIT_ACCESS_DENIED", AuditOutcomes.Denied, "AuditLogs", "AuditLog",
                id.ToString(), ex.Message, cancellationToken: ct);
            return Forbid();
        }
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<AuditLogPageResult>> GetEntityHistory(
        string entityType,
        string entityId,
        [FromQuery] int? organisationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            var result = await queryService.GetEntityHistoryAsync(
                entityType,
                entityId,
                new AuditLogFilter { OrganisationId = organisationId, Page = page, PageSize = pageSize, IncludeDetails = true },
                actor,
                ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            await auditWriter.RecordSecurityEventAsync(
                "AUDIT_ACCESS_DENIED", AuditOutcomes.Denied, "AuditLogs", entityType,
                entityId, ex.Message, cancellationToken: ct);
            return Forbid();
        }
    }

    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<AuditLogPageResult>> GetUserHistory(
        int userId,
        [FromQuery] int? organisationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            var result = await queryService.GetUserHistoryAsync(
                userId,
                new AuditLogFilter { OrganisationId = organisationId, Page = page, PageSize = pageSize },
                actor,
                ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            await auditWriter.RecordSecurityEventAsync(
                "AUDIT_ACCESS_DENIED", AuditOutcomes.Denied, "AuditLogs", "User",
                userId.ToString(), ex.Message, cancellationToken: ct);
            return Forbid();
        }
    }

    [HttpGet("organisation/{organisationId:int}")]
    public async Task<ActionResult<AuditLogPageResult>> GetOrganisationHistory(
        int organisationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            var result = await queryService.GetOrganisationHistoryAsync(
                organisationId,
                new AuditLogFilter { OrganisationId = organisationId, Page = page, PageSize = pageSize },
                actor,
                ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            await auditWriter.RecordSecurityEventAsync(
                "AUDIT_ACCESS_DENIED", AuditOutcomes.Denied, "AuditLogs", "Organisation",
                organisationId.ToString(), ex.Message, cancellationToken: ct);
            return Forbid();
        }
    }

    [HttpGet("correlation/{correlationId}")]
    public async Task<ActionResult<IReadOnlyList<AuditLogRecordDto>>> GetByCorrelationId(
        string correlationId,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        try
        {
            return Ok(await queryService.GetByCorrelationIdAsync(correlationId, actor, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            await auditWriter.RecordSecurityEventAsync(
                "AUDIT_ACCESS_DENIED", AuditOutcomes.Denied, "AuditLogs", "Correlation",
                correlationId, ex.Message, cancellationToken: ct);
            return Forbid();
        }
    }

    [HttpGet("export")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(HrmRateLimitPolicies.Export)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] int? organisationId = null,
        [FromQuery] int? divisionId = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? module = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var actor = await contextAccessor.GetCurrentContextAsync(ct);
        var filter = new AuditLogFilter
        {
            OrganisationId = organisationId,
            DivisionId = divisionId,
            UserId = userId,
            Action = action,
            Outcome = outcome,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
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
            await auditWriter.RecordSecurityEventAsync(
                "AUDIT_EXPORT_DENIED", AuditOutcomes.Denied, "AuditLogs", "AuditLog",
                organisationId?.ToString() ?? "EXPORT", ex.Message, cancellationToken: ct);
            return Forbid();
        }
    }
}
