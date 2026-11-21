using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRM.Api.Infrastructure;

/// <summary>
/// Automatically records an Activity Log for every successful state-changing API request
/// (POST, PUT, PATCH, DELETE) across DHR unless the service already recorded an explicit
/// activity for this request or the endpoint is a non-business/noisy endpoint.
/// Never reads or logs request payloads, credentials, tokens, or salary values.
/// </summary>
public sealed class GlobalActivityLoggingFilter(
    IActivityLogService? activityLogs = null,
    ILogger<GlobalActivityLoggingFilter>? logger = null) : IAsyncActionFilter
{
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

        if (activityLogs is null)
            return;

        var http = context.HttpContext;
        var method = http.Request.Method;
        if (!HttpMethods.IsPost(method) &&
            !HttpMethods.IsPut(method) &&
            !HttpMethods.IsPatch(method) &&
            !HttpMethods.IsDelete(method))
        {
            return;
        }

        // Only record successful business operations (2xx, no unhandled exception)
        if (executed.Exception is not null && !executed.ExceptionHandled)
            return;

        var statusCode = http.Response.StatusCode;
        if (statusCode < 200 || statusCode >= 300)
            return;

        // Avoid duplicate activity entries when a service already recorded a richer explicit activity
        if (http.Items.ContainsKey(HrmActivityContextAccessor.ExplicitActivityRecordedItemKey))
            return;

        // Only record authenticated user actions (skip anonymous probes/edge callbacks)
        if (http.User?.Identity?.IsAuthenticated != true)
            return;

        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controllerName = descriptor?.ControllerName ?? "Api";
        var actionName = descriptor?.ActionName ?? method;

        if (ExcludedControllers.Contains(controllerName))
            return;

        var path = http.Request.Path.Value ?? string.Empty;
        if (path.Contains("heartbeat", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("antiforgery", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("recent-search", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("mark-read", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("typing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var (category, module) = Classify(controllerName, path);
            var targetId = ResolveTargetId(context);
            var verbAction = MapAction(method, actionName);

            await activityLogs.RecordAsync(new ActivityLogRequest
            {
                Category = category,
                ActivityType = $"{module}.{actionName}",
                Module = module,
                Action = verbAction,
                Description = $"{HumanizeAction(verbAction, actionName)} in {module}" + (string.IsNullOrEmpty(targetId) ? "." : $" (#{targetId})."),
                TargetEntityType = controllerName,
                TargetEntityId = targetId,
                TargetDisplayName = string.IsNullOrEmpty(targetId) ? module : $"{controllerName} #{targetId}",
                Source = "HRM.Api"
            }, http.RequestAborted);
        }
        catch (Exception ex)
        {
            // Never fail a completed business request because of automatic activity logging.
            logger?.LogDebug(ex, "GlobalActivityLoggingFilter skipped recording for {Controller}.{Action}.", controllerName, actionName);
        }
    }

    private static string? ResolveTargetId(ActionExecutingContext context)
    {
        foreach (var key in new[] { "id", "jobId", "staffId", "individualId", "organisationId", "teamId", "requestId", "runId", "documentId", "transferId" })
        {
            if (context.RouteData.Values.TryGetValue(key, out var routeVal) && routeVal is not null)
            {
                var str = routeVal.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                    return str;
            }
        }
        return null;
    }

    private static string MapAction(string method, string actionName)
    {
        if (actionName.Contains("Approve", StringComparison.OrdinalIgnoreCase)) return "APPROVE";
        if (actionName.Contains("Reject", StringComparison.OrdinalIgnoreCase)) return "REJECT";
        if (actionName.Contains("Cancel", StringComparison.OrdinalIgnoreCase)) return "CANCEL";
        if (actionName.Contains("Delete", StringComparison.OrdinalIgnoreCase) || HttpMethods.IsDelete(method)) return "DELETE";
        if (actionName.Contains("Create", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Add", StringComparison.OrdinalIgnoreCase)) return "CREATE";
        if (actionName.Contains("Submit", StringComparison.OrdinalIgnoreCase)) return "SUBMIT";
        if (actionName.Contains("Publish", StringComparison.OrdinalIgnoreCase)) return "PUBLISH";
        if (actionName.Contains("Save", StringComparison.OrdinalIgnoreCase) || actionName.Contains("Update", StringComparison.OrdinalIgnoreCase) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method)) return "UPDATE";
        return method.ToUpperInvariant();
    }

    private static string HumanizeAction(string verbAction, string actionName) => verbAction switch
    {
        "APPROVE" => $"Approved ({actionName})",
        "REJECT" => $"Rejected ({actionName})",
        "CANCEL" => $"Cancelled ({actionName})",
        "DELETE" => $"Deleted ({actionName})",
        "CREATE" => $"Created ({actionName})",
        "SUBMIT" => $"Submitted ({actionName})",
        "PUBLISH" => $"Published ({actionName})",
        "UPDATE" => $"Updated ({actionName})",
        _ => $"Performed {actionName}"
    };

    private static (ActivityCategory Category, string Module) Classify(string controllerName, string path)
    {
        var key = $"{controllerName} {path}".ToLowerInvariant();
        if (key.Contains("payroll") || key.Contains("payslip") || key.Contains("compensation") || key.Contains("loan"))
            return (ActivityCategory.Payroll, "Payroll");
        if (key.Contains("leave"))
            return (ActivityCategory.Leave, "Leave");
        if (key.Contains("attendance") || key.Contains("clock") || key.Contains("overtime") || key.Contains("schedule") || key.Contains("roster") || key.Contains("workplan") || key.Contains("duty"))
            return (ActivityCategory.Attendance, "Attendance");
        if (key.Contains("recruit") || key.Contains("vacancy") || key.Contains("applicant") || key.Contains("interview") || key.Contains("offer"))
            return (ActivityCategory.Recruitment, "Recruitment");
        if (key.Contains("transfer") || key.Contains("separation") || key.Contains("onboarding"))
            return (ActivityCategory.Transfers, "Transfers");
        if (key.Contains("asset"))
            return (ActivityCategory.Assets, "Assets");
        if (key.Contains("project") || key.Contains("task") || key.Contains("todo"))
            return (ActivityCategory.Projects, "Projects");
        if (key.Contains("team") || key.Contains("group"))
            return (ActivityCategory.Teams, "Teams");
        if (key.Contains("permission") || key.Contains("role") || key.Contains("admin") || key.Contains("setting") || key.Contains("user"))
            return (ActivityCategory.Administration, "Administration");
        if (key.Contains("organisation") || key.Contains("organization") || key.Contains("calendar") || key.Contains("budget"))
            return (ActivityCategory.Organisation, "Organisation");
        if (key.Contains("workflow") || key.Contains("approval") || key.Contains("request"))
            return (ActivityCategory.Workflow, "Workflow");
        if (key.Contains("communication") || key.Contains("announcement") || key.Contains("document") || key.Contains("certificate"))
            return (ActivityCategory.Communication, "Communication");
        if (key.Contains("staff") || key.Contains("employee") || key.Contains("individual") || key.Contains("position") || key.Contains("job"))
            return (ActivityCategory.EmployeeManagement, "Employees");
        return (ActivityCategory.Administration, controllerName);
    }
}
