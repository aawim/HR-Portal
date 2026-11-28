using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Infrastructure;

/// <summary>
/// Automatically records an Operation Log entry in dbo.OperationLogs for every successful
/// state-changing API request (POST, PUT, PATCH, DELETE) across the entire system whenever
/// the service did not already create an explicit OperationLog during the request.
/// Never fails a completed business request if operation logging encounters an issue.
/// </summary>
public sealed class GlobalOperationLoggingFilter(
    IDbContextFactory<HrmTeContext> dbFactory,
    IOperationLogService operationLogs,
    ILogger<GlobalOperationLoggingFilter>? logger = null) : IAsyncActionFilter
{
    private static int _cachedDefaultActionId;

    private static readonly HashSet<string> ExcludedControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Logout",
        "SessionTokens",
        "ErrorLogs",
        "AuditLogs",
        "ActivityLogs",
        "AttendanceConnector",
        "EdgeAttendanceHeartbeat",
        "EdgeAttendanceIngestion",
        "StaffChat",
        "StaffGroupChat",
        "WorkspaceRecentSearch",
        "RequestNotification"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        var http = context.HttpContext;
        var method = http.Request.Method;
        if (!HttpMethods.IsPost(method) &&
            !HttpMethods.IsPut(method) &&
            !HttpMethods.IsPatch(method) &&
            !HttpMethods.IsDelete(method))
        {
            return;
        }

        if (executed.Exception is not null && !executed.ExceptionHandled)
            return;

        var statusCode = http.Response.StatusCode;
        if (statusCode < 200 || statusCode >= 300)
            return;

        // Avoid duplicate OperationLog entries when the service already created one explicitly
        if (http.Items.ContainsKey(OperationLogService.ExplicitOperationRecordedItemKey))
            return;

        if (http.User?.Identity?.IsAuthenticated != true)
            return;

        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controllerName = descriptor?.ControllerName ?? "Api";
        var actionName = descriptor?.ActionName ?? method;

        if (ExcludedControllers.Contains(controllerName))
            return;

        var path = http.Request.Path.Value ?? string.Empty;
        if (path.Contains("/query/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("heartbeat", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("antiforgery", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("recent-search", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("mark-read", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("typing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(http.RequestAborted);
            var actionId = _cachedDefaultActionId;
            if (actionId <= 0)
            {
                actionId = await db.OperationLogActionTypes.AsNoTracking()
                    .OrderBy(a => a.OperationLogActionTypes)
                    .Select(a => a.OperationLogActionTypes)
                    .FirstOrDefaultAsync(http.RequestAborted);
                if (actionId > 0) _cachedDefaultActionId = actionId;
            }
            if (actionId <= 0)
                return;

            var remarks = $"{method} {path} ({controllerName}.{actionName})";
            await operationLogs.CreateAsync(db, actionId, remarks);
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "GlobalOperationLoggingFilter skipped recording for {Controller}.{Action}.", controllerName, actionName);
        }
    }
}
