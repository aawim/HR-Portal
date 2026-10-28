using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Infrastructure;

[ApiController]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(ApiExceptionFilter))]
[ServiceFilter(typeof(ApiSessionFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class HrmApiController : ControllerBase;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowMissingEmployeeAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiPermissionAttribute(params string[] keys) : Attribute
{
    public IReadOnlyList<string> Keys { get; } = keys;
}
