using Microsoft.AspNetCore.Components.Authorization;
namespace HRM.UI.Authentication;

public sealed class CircuitBackendHandler(AuthenticationStateProvider authentication, BackendSessionStore sessions) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await sessions.Require((await authentication.GetAuthenticationStateAsync()).User).SendAsync(request, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            // Let the operation client perform one full navigation that rebuilds an opaque
            // server-side backend session after a UI restart or an expired circuit handle.
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}
