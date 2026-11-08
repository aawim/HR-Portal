using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
namespace HRM.Api.Authentication;
public sealed class ApiAuthenticationStateProvider(IHttpContextAccessor accessor) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(
        accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity())));
}
