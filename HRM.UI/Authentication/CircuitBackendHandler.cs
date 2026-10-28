using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
namespace HRM.UI.Authentication;

public sealed class CircuitBackendHandler(AuthenticationStateProvider authentication, BackendSessionStore sessions,
    IHttpClientFactory clients, BackendOrigins origins, IDataProtectionProvider protection) : HttpMessageHandler
{
    public const string RecoveryHeader = "X-HRM-Session-Recovery";
    private readonly IDataProtector recoveryProtector = protection.CreateProtector("HRM.UI.BackendSessionRecovery.v1");
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        BackendSession session;
        try
        {
            session = sessions.Require(user);
        }
        catch (UnauthorizedAccessException)
        {
            try
            {
                var protectedCookies = user.FindFirst(BackendSessionStore.RecoveryClaimType)?.Value
                    ?? throw new UnauthorizedAccessException();
                var cookies = recoveryProtector.Unprotect(protectedCookies);
                session = sessions.Restore(user, cookies, () =>
                {
                    var restored = new BackendSession(clients.CreateClient("backend"), origins.Api);
                    restored.ImportCookies(cookies);
                    return restored;
                });
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
            {
                return Unauthorized(user.Identity?.IsAuthenticated == true);
            }
        }
        try
        {
            var response = await session.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && user.Identity?.IsAuthenticated == true)
                response.Headers.TryAddWithoutValidation(RecoveryHeader, "required");
            return response;
        }
        catch (UnauthorizedAccessException) { return Unauthorized(user.Identity?.IsAuthenticated == true); }
    }

    private static HttpResponseMessage Unauthorized(bool recover)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        if (recover) response.Headers.TryAddWithoutValidation(RecoveryHeader, "required");
        return response;
    }
}
