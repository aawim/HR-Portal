using Microsoft.AspNetCore.Components.Authorization;
namespace HRM.UI.Authentication;

public sealed class CircuitBackendHandler(AuthenticationStateProvider authentication, BackendSessionStore sessions) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => await sessions.Require((await authentication.GetAuthenticationStateAsync()).User).SendAsync(request, cancellationToken);
}
