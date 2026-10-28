using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRM.Api.Authentication;
using HRM.Application.Transport;
using HRM.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class DeploymentSecurityTests
{
    [Fact]
    public void Credential_exposure_check_matches_fingerprints_without_storing_plaintext()
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("test-only-credential")));
        Assert.True(CredentialExposure.Matches("test-only-credential", [fingerprint]));
        Assert.False(CredentialExposure.Matches("different-credential", [fingerprint]));
        Assert.False(CredentialExposure.Matches(null, [fingerprint]));
    }
    [Theory]
    [InlineData(false, "8.0.16", "8.0.33", false)]
    [InlineData(false, "8.0.33", "8.0.16", false)]
    [InlineData(false, "8.0.33", "8.0.33", true)]
    [InlineData(true, "8.0.16", "8.0.16", true)]
    public void Production_rejects_unpatched_runtime_and_web_framework(bool development, string runtime, string aspnet, bool accepted)
    {
        if (accepted) RuntimeRequirements.Validate(development, new(runtime), new(aspnet));
        else Assert.Throws<InvalidOperationException>(() => RuntimeRequirements.Validate(development, new(runtime), new(aspnet)));
    }
    private static IConfiguration Config(Dictionary<string, string?>? changes = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Application:PublicOrigin"] = "https://staff.example.org/",
            ["Authentication:OpenIdConnect:Authority"] = "https://identity.example.org/efaas",
            ["Authentication:OpenIdConnect:ClientId"] = "test-client",
            ["Authentication:OpenIdConnect:ClientSecret"] = "test-only-not-a-real-secret",
            ["AllowedHosts"] = "staff.example.org;api.example.org"
        };
        foreach (var pair in changes ?? []) values[pair.Key] = pair.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "HRM.Api";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "hrm-unit-app");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://localhost:62839/")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("http://staff.example.org/")]
    [InlineData("https://user:password@staff.example.org/")]
    [InlineData("https://staff.example.org/subpath")]
    public void Production_rejects_missing_local_or_unsafe_public_origins(string? value)
        => Assert.Throws<InvalidOperationException>(() => DeploymentOrigin.Require(value, "origin", production: true));

    [Theory]
    [InlineData("Authentication:OpenIdConnect:ClientSecret", "")]
    [InlineData("Authentication:OpenIdConnect:ClientId", "")]
    [InlineData("Authentication:OpenIdConnect:Authority", "http://identity.example.org/efaas")]
    public void Production_authentication_requires_explicit_efaas_configuration(string field, string value)
        => Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddEfaasAuthentication(Config(new() { [field] = value }), new EnvironmentStub("Production")));

    [Fact]
    public void Production_requires_restricted_hosts_and_persistent_keys_outside_application()
    {
        var services = new ServiceCollection(); var environment = new EnvironmentStub("Production");
        Assert.Throws<InvalidOperationException>(() => services.AddProtectedHostKeys(Config(), environment, "HRM.Api"));
        Assert.Throws<InvalidOperationException>(() => services.AddProtectedHostKeys(Config(new() { ["AllowedHosts"] = "*" }), environment, "HRM.Api"));
        Assert.Throws<InvalidOperationException>(() => services.AddProtectedHostKeys(Config(new() { ["DataProtection:KeysPath"] = Path.Combine(environment.ContentRootPath, "wwwroot", "keys") }), environment, "HRM.Api"));
    }

    [Fact]
    public void Oidc_requires_code_pkce_and_signed_issuer_audience_and_lifetime_validation()
    {
        using var provider = Services();
        var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("OpenIdConnect");
        Assert.Equal("code", oidc.ResponseType); Assert.True(oidc.UsePkce); Assert.True(oidc.RequireHttpsMetadata);
        Assert.True(oidc.TokenValidationParameters.ValidateIssuer); Assert.True(oidc.TokenValidationParameters.ValidateAudience);
        Assert.True(oidc.TokenValidationParameters.ValidateLifetime); Assert.True(oidc.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(oidc.TokenValidationParameters.RequireSignedTokens);
        var cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
        Assert.IsType<ProtectedTicketStore>(cookie.SessionStore); Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.Equal(AuthenticationSessionDefaults.CookieName, cookie.Cookie.Name); Assert.Null(cookie.Cookie.Domain);
    }

    [Fact]
    public async Task Oidc_callbacks_use_configuration_even_when_request_host_is_forged()
    {
        using var provider = Services();
        var options = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get("OpenIdConnect");
        var http = new DefaultHttpContext(); http.Request.Scheme = "https"; http.Request.Host = new HostString("attacker.example");
        var context = new RedirectContext(http, new AuthenticationScheme("OpenIdConnect", null, typeof(OpenIdConnectHandler)), options, new AuthenticationProperties());
        context.ProtocolMessage = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage();
        await options.Events.OnRedirectToIdentityProvider(context);
        Assert.Equal("https://staff.example.org/Online/Auth/Callback", context.ProtocolMessage.RedirectUri);
        await options.Events.OnRedirectToIdentityProviderForSignOut(context);
        Assert.Equal("https://staff.example.org/signout-callback-oidc", context.ProtocolMessage.PostLogoutRedirectUri);
    }

    [Theory]
    [InlineData("127.0.0.1", "https", "staff.example.org")]
    [InlineData("198.51.100.10", "http", "api.example.org")]
    public async Task Forwarded_headers_are_honoured_only_from_known_proxies(string address, string expectedScheme, string expectedHost)
    {
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        { services.AddTrustedProxyHeaders(Config(), new Uri("https://staff.example.org/")); }).Configure(app =>
        {
            app.Use((context, next) => { context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(address); return next(); });
            app.UseTrustedProxyHeaders(); app.Run(context => context.Response.WriteAsync(context.Request.Scheme + "://" + context.Request.Host));
        }));
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://api.example.org/");
        request.Headers.Add("X-Forwarded-Host", "staff.example.org"); request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request);
        Assert.Equal(expectedScheme + "://" + expectedHost, await response.Content.ReadAsStringAsync());
    }

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddEfaasAuthentication(Config(), new EnvironmentStub("Production"));
        return services.BuildServiceProvider();
    }
    private static AuthenticationTicket Ticket() => new(new ClaimsPrincipal(new ClaimsIdentity([new Claim("idnumber", "A123456")], "efaas")),
        new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) }, "Cookies");

    [Fact]
    public async Task Stored_efaas_tokens_are_encrypted_and_revoked_on_remove()
    {
        using var provider = Services(); var store = provider.GetRequiredService<ProtectedTicketStore>();
        var ticket = Ticket(); ticket.Properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = "test-private-id-token" }]);
        var key = await store.StoreAsync(ticket);
        var cached = await provider.GetRequiredService<IDistributedCache>().GetAsync(key);
        Assert.NotNull(cached); Assert.DoesNotContain("test-private-id-token", Encoding.UTF8.GetString(cached));
        Assert.Equal("test-private-id-token", (await store.RetrieveAsync(key))!.Properties.GetTokenValue("id_token"));
        await store.RemoveAsync(key); Assert.Null(await store.RetrieveAsync(key));
        await store.RenewAsync(key, ticket); Assert.Null(await store.RetrieveAsync(key));
    }

    [Fact]
    public async Task Tampered_or_unknown_tickets_fail_closed()
    {
        using var provider = Services(); var store = provider.GetRequiredService<ProtectedTicketStore>();
        var key = await store.StoreAsync(Ticket()); var cache = provider.GetRequiredService<IDistributedCache>();
        var data = (await cache.GetAsync(key))!; data[^1] ^= 1; await cache.SetAsync(key, data);
        Assert.Null(await store.RetrieveAsync(key)); Assert.Null(await store.RetrieveAsync("user-supplied-key"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public async Task Server_tickets_reject_expired_sessions_and_bound_long_lifetimes(int hours)
    {
        using var provider = Services(); var store = provider.GetRequiredService<ProtectedTicketStore>();
        var ticket = Ticket(); ticket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(hours);
        var key = await store.StoreAsync(ticket); var restored = await store.RetrieveAsync(key);
        if (hours < 0) Assert.Null(restored);
        else { Assert.NotNull(restored); Assert.True(restored.Properties.ExpiresUtc <= DateTimeOffset.UtcNow.AddHours(8)); }
    }

    [Fact]
    public async Task Api_cookie_contains_only_a_reference_and_logout_revokes_replayed_cookie()
    {
        using var server = new TestServer(new WebHostBuilder().UseEnvironment("Testing").ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider(); services.AddRouting();
            services.AddEfaasAuthentication(Config(), new EnvironmentStub("Production"));
            services.PostConfigure<OpenIdConnectOptions>("OpenIdConnect", options =>
            {
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                { Issuer = "https://identity.example.org/efaas", EndSessionEndpoint = "https://identity.example.org/efaas/logout" };
                options.ConfigurationManager = new Microsoft.IdentityModel.Protocols.StaticConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>(options.Configuration);
            });
        }).Configure(app =>
        {
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/test/sign-in", async context => { var ticket = Ticket(); ticket.Properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = "test-provider-id-token" }]); await context.SignInAsync("Cookies", ticket.Principal, ticket.Properties); }).AllowAnonymous();
                endpoints.MapGet("/api/protected", context => context.Response.WriteAsync("authenticated")); // Fallback policy must secure even this undecorated route.
                endpoints.MapPost("/test/sign-out", async context => { await context.SignOutAsync("Cookies"); await context.SignOutAsync("OpenIdConnect"); });
            });
        }));
        using var client = server.CreateClient(); using var login = await client.GetAsync("/test/sign-in");
        var cookie = login.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        var options = server.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
        var browserTicket = options.TicketDataFormat.Unprotect(Uri.UnescapeDataString(cookie[(cookie.IndexOf('=') + 1)..]));
        Assert.NotNull(browserTicket); Assert.DoesNotContain(browserTicket.Principal.Claims, c => c.Type == "idnumber");
        Assert.Contains(browserTicket.Principal.Claims, c => c.Type.EndsWith("sessionid", StringComparison.OrdinalIgnoreCase));
        using var authenticated = new HttpRequestMessage(HttpMethod.Get, "/api/protected"); authenticated.Headers.Add("Cookie", cookie);
        using var ok = await client.SendAsync(authenticated); Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/test/sign-out"); logout.Headers.Add("Cookie", cookie);
        using var signedOut = await client.SendAsync(logout); Assert.Equal(HttpStatusCode.Redirect, signedOut.StatusCode);
        Assert.Equal("identity.example.org", signedOut.Headers.Location!.Host);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(signedOut.Headers.Location.Query);
        Assert.Equal("test-provider-id-token", query["id_token_hint"].ToString());
        Assert.Equal("https://staff.example.org/signout-callback-oidc", query["post_logout_redirect_uri"].ToString());
        using var replay = new HttpRequestMessage(HttpMethod.Get, "/api/protected"); replay.Headers.Add("Cookie", cookie);
        using var denied = await client.SendAsync(replay); Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using var forged = new HttpRequestMessage(HttpMethod.Get, "/api/protected"); forged.Headers.Add("Authorization", "Bearer forged"); forged.Headers.Add("X-User-Id", "1");
        using var rejected = await client.SendAsync(forged); Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }
}
