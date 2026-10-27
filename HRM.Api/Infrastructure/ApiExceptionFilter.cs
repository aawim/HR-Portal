using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Infrastructure;

public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested) return;
        var status = context.Exception switch
        {
            UnauthorizedAccessException => 403,
            ArgumentException => 400,
            KeyNotFoundException => 404,
            InvalidOperationException or DbUpdateException => 409,
            _ => 500
        };
        var title = status switch
        {
            403 => "You do not have permission to access this information or perform this action.",
            400 => "Check the request fields and try again.",
            404 => "The requested record is unavailable.",
            409 => "This action could not be completed. Refresh and try again, or contact your organisation's HR team.",
            _ => "This action could not be completed right now. Please try again."
        };
        logger.LogWarning(context.Exception, "API request failed with status {Status}; trace {TraceId}", status, context.HttpContext.TraceIdentifier);
        context.Result = new ObjectResult(new ProblemDetails { Status = status, Title = title,
            Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier } }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
