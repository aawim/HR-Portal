using Microsoft.AspNetCore.Components;
namespace HRM.Api.Authentication;
/// <summary>Supplies the configured public application URL for existing audit services.</summary>
public sealed class ApiNavigationManager : NavigationManager
{
    public ApiNavigationManager(IConfiguration configuration, IHttpContextAccessor accessor)
    {
        var address = configuration["Application:PublicOrigin"];
        if (!System.Uri.TryCreate(address, UriKind.Absolute, out var origin) || origin.Scheme != "https" || !string.IsNullOrEmpty(origin.UserInfo))
            throw new InvalidOperationException("Application:PublicOrigin must be an HTTPS origin.");
        var root = origin.GetLeftPart(UriPartial.Authority) + "/";
        var path = accessor.HttpContext?.Request.Path.Value?.TrimStart('/') ?? "";
        Initialize(root, new Uri(new Uri(root), path).AbsoluteUri);
    }
    protected override void NavigateToCore(string uri, bool forceLoad) => throw new UnauthorizedAccessException("Your employee session is unavailable. Please sign in again.");
}

