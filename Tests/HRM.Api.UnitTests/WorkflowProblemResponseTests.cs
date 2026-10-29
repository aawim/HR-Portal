using HRM.Api.Infrastructure;
using HRM.Application.Transport;
using HRM.DTOs.ApprovalFlow;
using HRM.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class WorkflowProblemResponseTests
{
    [Fact]public void Configuration_failure_returns_a_known_code_and_no_internal_workflow_details()
    {
        var http=new DefaultHttpContext();
        var context=new ExceptionContext(new ActionContext(http,new RouteData(),new ActionDescriptor()),new List<IFilterMetadata>())
        {Exception=new WorkflowConfigurationException(new WorkflowRequestContextDto(),"Sensitive IDs and SQL details")};
        new ApiExceptionFilter(NullLogger<ApiExceptionFilter>.Instance).OnException(context);
        var result=Assert.IsType<ObjectResult>(context.Result);var problem=Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(409,result.StatusCode);Assert.Equal(ApiProblemCodes.WorkflowUnavailableMessage,problem.Title);Assert.Equal(ApiProblemCodes.WorkflowUnavailable,http.Response.Headers[ApiProblemCodes.Header].ToString());Assert.True(context.ExceptionHandled);
    }
}
