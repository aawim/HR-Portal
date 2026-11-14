using System.Diagnostics;
using System.Security.Claims;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HRM.ErrorLogs.Middleware;

/// <summary>
/// ASP.NET Core middleware that catches unhandled HTTP request exceptions, extracts rich request metadata,
/// pinpoints source line numbers, and records prescriptive fix recommendations.
/// </summary>
public sealed class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorLoggingMiddleware> _logger;
    private readonly string _applicationSource;

    public ErrorLoggingMiddleware(
        RequestDelegate _next,
        ILogger<ErrorLoggingMiddleware> logger,
        string applicationSource = "HRM.Api")
    {
        this._next = _next;
        this._logger = logger;
        this._applicationSource = applicationSource;
    }

    public async Task InvokeAsync(HttpContext context, IErrorLogService errorLogService)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client intentionally closed connection or navigated away before request finished.
            // This is a normal HTTP cancellation lifecycle event, not an application fault.
            _logger.LogDebug("HTTP {Method} {Path} was canceled by client disconnection.", context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorLogService);
            throw; // Re-throw so standard exception pipelines / error pages can handle presentation
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, IErrorLogService errorLogService)
    {
        try
        {
            var req = context.Request;
            var user = context.User;

            var username = user?.Identity?.Name ??
                           user?.FindFirst(ClaimTypes.Name)?.Value ??
                           user?.FindFirst("preferred_username")?.Value ??
                           user?.FindFirst(ClaimTypes.Email)?.Value ??
                           (user?.Identity?.IsAuthenticated == true ? "AuthenticatedUser" : "Anonymous");

            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         user?.FindFirst("sub")?.Value ??
                         user?.FindFirst("oid")?.Value;

            var remoteIp = GetClientIpAddress(context);
            var pageLocation = $"{req.Path}{req.QueryString}";
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var userAgent = req.Headers["User-Agent"].ToString();

            var errorContext = new ErrorContext
            {
                Username = username,
                UserId = userId,
                RemoteIp = remoteIp,
                PageLocation = pageLocation,
                HttpMethod = req.Method,
                StatusCode = context.Response?.StatusCode is > 0 and < 600 ? context.Response.StatusCode : 500,
                UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
                TraceId = traceId,
                ApplicationSource = _applicationSource
            };

            await errorLogService.CaptureExceptionAsync(ex, ErrorCriticalityLevel.Error, errorContext);
        }
        catch (Exception loggingEx)
        {
            _logger.LogError(loggingEx, "Failed to record unhandled exception into ErrorLogService.");
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Check standard reverse proxy headers
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.ToString().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }
}
