using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace HRM.Api.Infrastructure;

public sealed class ApiSessionFilter(IUserAccessService access) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails { Status = 401, Title = "Please sign in again." });
            return;
        }
        // MVC requests in the temporary Blazor host need their own scoped authentication state.
        // The principal is already verified by cookie authentication; circuit scopes are untouched.
        if (context.HttpContext.RequestServices.GetService<AuthenticationStateProvider>() is IHostEnvironmentAuthenticationStateProvider hostState)
            hostState.SetAuthenticationState(Task.FromResult(new AuthenticationState(context.HttpContext.User)));
        var action = context.ActionDescriptor as ControllerActionDescriptor;
        var allowsMissingEmployee = context.Controller.GetType().IsDefined(typeof(AllowMissingEmployeeAttribute), true) ||
            action?.MethodInfo.IsDefined(typeof(AllowMissingEmployeeAttribute), true) == true;
        if (!allowsMissingEmployee && await access.GetContextAsync() is not { UserId: > 0 })
        {
            context.Result = new UnauthorizedObjectResult(new ProblemDetails { Status = 401, Title = "Your employee session is unavailable. Please sign in again." });
            return;
        }
        var requirement = action?.MethodInfo.GetCustomAttribute<ApiPermissionAttribute>() ?? context.Controller.GetType().GetCustomAttribute<ApiPermissionAttribute>();
        if (requirement is not null)
        {
            var actor = await access.GetContextAsync();
            var allowed = actor?.IsSuperAdministrator == true || await access.HasPermissionAsync("Admin");
            foreach (var permission in requirement.Keys) allowed |= await access.HasPermissionAsync(permission);
            if (!allowed)
            {
                context.Result = new ObjectResult(new ProblemDetails { Status = 403, Title = "You do not have permission to access this resource." }) { StatusCode = 403 };
                return;
            }
        }
        context.HttpContext.RequestAborted.ThrowIfCancellationRequested();
        await next();
    }
}
