using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HRM.Application.Transport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HRM.UI.Authentication;

public sealed class BackendAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IHttpClientFactory clients, BackendSessionStore sessions, BackendOrigins origins)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Backend";
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Cookie")) return AuthenticateResult.NoResult();
        var session = new BackendSession(clients.CreateClient("backend"), origins.Api);
        var retained = false;
        try
        {
            session.ImportCookies(Request.Headers.Cookie.ToString());
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/session/identity");
            using var response = await session.SendAsync(request, Context.RequestAborted);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return AuthenticateResult.NoResult();
            response.EnsureSuccessStatusCode();
            var identity = await response.Content.ReadFromJsonAsync<ApiIdentityDto>(ApiOperationKeys.Json, Context.RequestAborted);
            if (identity is not { IsAuthenticated: true } || string.IsNullOrWhiteSpace(identity.Username)) return AuthenticateResult.NoResult();
            var handle = sessions.Add(session, Request.Headers.Cookie.ToString()); retained = true;
            var claims = new[] { new Claim("idnumber", identity.Username), new Claim(ClaimTypes.NameIdentifier, identity.Username),
                new Claim("full_name", identity.DisplayName ?? identity.Username), new Claim(BackendSessionStore.ClaimType, handle) };
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName, "full_name", ClaimTypes.Role)), SchemeName));
        }
        catch (HttpRequestException ex) { Logger.LogWarning(ex, "The authentication backend is unavailable"); return AuthenticateResult.Fail("Authentication is temporarily unavailable."); }
        finally { if (!retained) session.Dispose(); }
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Online/Auth/login"); return Task.CompletedTask;
    }
}

public sealed record BackendOrigins(Uri Api, Uri Public)
{
    public static BackendOrigins FromConfiguration(IConfiguration config)
    {
        static Uri Origin(string? value, string name)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException($"{name} must be a fixed HTTPS origin without a path or credentials.");
            return uri;
        }
        return new(Origin(config["Backend:Origin"], "Backend:Origin"), Origin(config["Application:PublicOrigin"], "Application:PublicOrigin"));
    }
}
