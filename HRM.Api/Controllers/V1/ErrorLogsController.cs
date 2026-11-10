using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/error-logs")]
[AllowMissingEmployee]
public sealed class ErrorLogsController(IErrorLogService errorLogService) : HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ErrorLogEntry>>> GetRecent(
        [FromQuery] int count = 100,
        [FromQuery] ErrorCriticalityLevel? minLevel = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool onlyDeleted = false)
    {
        var errors = await errorLogService.GetRecentErrorsAsync(count, minLevel, search, includeDeleted, onlyDeleted);
        return Ok(errors);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ErrorLogEntry>> GetById(Guid id)
    {
        var entry = await errorLogService.GetErrorByIdAsync(id);
        if (entry == null) return NotFound();
        return Ok(entry);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ErrorStatistics>> GetStatistics()
    {
        var stats = await errorLogService.GetStatisticsAsync();
        return Ok(stats);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<ActionResult<bool>> MarkResolved(
        Guid id,
        [FromBody] ErrorLogResolveRequest request)
    {
        var resolvedBy = !string.IsNullOrWhiteSpace(request?.ResolvedBy)
            ? request.ResolvedBy
            : (User?.Identity?.Name ?? "Developer");

        var success = await errorLogService.MarkResolvedAsync(id, resolvedBy, request?.Notes);
        if (!success) return NotFound();
        return Ok(true);
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<bool>> Reopen(Guid id)
    {
        var success = await errorLogService.ReopenAsync(id);
        if (!success) return NotFound();
        return Ok(true);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<bool>> SoftDelete(Guid id)
    {
        var deletedBy = User?.Identity?.Name ?? "Developer";
        var success = await errorLogService.SoftDeleteAsync(id, deletedBy);
        if (!success) return NotFound();
        return Ok(true);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<bool>> Restore(Guid id)
    {
        var success = await errorLogService.RestoreAsync(id);
        if (!success) return NotFound();
        return Ok(true);
    }

    [HttpDelete("resolved")]
    public async Task<ActionResult<int>> ClearResolved()
    {
        var deletedBy = User?.Identity?.Name ?? "Developer";
        var count = await errorLogService.SoftDeleteResolvedAsync(deletedBy);
        return Ok(count);
    }

    [HttpPost("simulate")]
    public async Task<ActionResult<ErrorLogEntry>> SimulateError([FromBody] SimulateErrorRequest request)
    {
        Exception ex;
        var errorType = request.ExceptionType?.ToLowerInvariant() ?? "nullreference";

        try
        {
            switch (errorType)
            {
                case "nullreference":
                    string? nullString = null;
                    _ = nullString!.Length; // Throws NullReferenceException
                    throw new Exception("Unreachable");

                case "argumentnull":
                    throw new ArgumentNullException("payrollRunId", "Payroll run identifier cannot be empty or default value.");

                case "sql":
                case "sqlkey":
                    throw new InvalidOperationException("Violation of UNIQUE KEY constraint 'UQ_PAY_PayrollRuns_Reference'. Cannot insert duplicate key in object 'dbo.PAY_PayrollRuns'. The duplicate key value is (PR-2026-11).");

                case "concurrency":
                    throw new InvalidOperationException("Database operation expected to affect 1 row(s) but actually affected 0 row(s); data may have been modified or deleted since entities were loaded. See http://go.microsoft.com/fwlink/?LinkId=527962 for information on understanding and handling optimistic concurrency exceptions.");

                case "http":
                    throw new HttpRequestException("Connection refused (127.0.0.1:5001) while attempting to contact backend identity service.");

                case "unauthorized":
                    throw new UnauthorizedAccessException("Current user does not have permission 'HRM.Payroll.BatchApprove' to perform this action.");

                default:
                    throw new InvalidOperationException(request.Message ?? "Simulated developer test exception for Error Logs diagnostic validation.");
            }
        }
        catch (Exception caught)
        {
            ex = caught;
        }

        var entry = await errorLogService.CaptureExceptionAsync(
            ex,
            request.Level ?? ErrorCriticalityLevel.Error,
            new ErrorContext
            {
                ApplicationSource = request.Source ?? "HRM.Api",
                Username = User?.Identity?.Name ?? "ahmed.developer",
                RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                PageLocation = request.PageLocation ?? "/developer/error-logs",
                HttpMethod = "POST",
                StatusCode = 500,
                CustomDataJson = "{\"IsSimulation\": true, \"SimulatedBy\": \"Developer Portal\"}"
            });

        return Ok(entry);
    }

    public sealed class ErrorLogResolveRequest
    {
        public string? ResolvedBy { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class SimulateErrorRequest
    {
        public string? ExceptionType { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public string? PageLocation { get; set; }
        public ErrorCriticalityLevel? Level { get; set; }
    }
}
