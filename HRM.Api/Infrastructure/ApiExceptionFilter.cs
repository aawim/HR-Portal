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
            HRM.WorkPlanning.WorkPlanBusyException => 503,
            UnauthorizedAccessException => 403,
            ArgumentException => 400,
            KeyNotFoundException => 404,
            InvalidOperationException or DbUpdateException => 409,
            _ => 500
        };
        var title = status switch
        {
            503 => "The server is temporarily busy. Please refresh shortly.",
            403 => "You do not have permission to access this information or perform this action.",
            400 => "Check the request fields and try again.",
            404 => "The requested record is unavailable.",
            409 => context.Exception switch
            {
                InvalidOperationException inv => inv.Message,
                DbUpdateException dbEx => dbEx.InnerException?.Message ?? dbEx.Message,
                _ => "This action could not be completed. Refresh and try again, or contact your organisation's HR team."
            },
            _ => "This action could not be completed right now. Please try again."
        };
        if(context.Exception is HRM.Services.WorkflowConfigurationException)
        {
            context.HttpContext.Response.Headers[HRM.Application.Transport.ApiProblemCodes.Header]=HRM.Application.Transport.ApiProblemCodes.WorkflowUnavailable;
            title=HRM.Application.Transport.ApiProblemCodes.WorkflowUnavailableMessage;
        }
        // This is the single exception boundary for every HrmApiController endpoint.
        // Log at Error so the registered HRM error-log provider persists the exception;
        // the client still receives only the safe ProblemDetails response below.
        logger.LogError(context.Exception,
            "API request failed. Method {Method}; path {Path}; status {Status}; trace {TraceId}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            status,
            context.HttpContext.TraceIdentifier);
        if(status==503)context.HttpContext.Response.Headers.RetryAfter="1";
        context.Result = new ObjectResult(new ProblemDetails { Status = status, Title = title,
            Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier } }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
