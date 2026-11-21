using System.Security.Claims;
using Intranet.Application.Security;
using Microsoft.AspNetCore.Http;
namespace Intranet.Infrastructure.Security;
public class HttpOrganisationContext : IOrganisationContext
{
    private readonly IHttpContextAccessor _accessor;
    public HttpOrganisationContext(IHttpContextAccessor accessor) => _accessor = accessor;
    private ClaimsPrincipal? User => _accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public long OrganisationBusinessEntityID => long.TryParse(User?.FindFirst("org")?.Value, out var v) ? v : 0;
    public long IndividualID => long.TryParse(User?.FindFirst("sub")?.Value ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var v) ? v : 0;
    public bool HasPermission(string permission) => User?.HasClaim("permission", permission) == true || User?.IsInRole(permission) == true;
}