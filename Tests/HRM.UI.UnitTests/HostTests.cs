using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using HRM.Application.Contracts;
using HRM.Application.Transport;
using HRM.UI.Authentication;
using HRM.UI.Services.Transport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.UI.UnitTests;

public sealed class HostTests
{
    private sealed class ClientFactory(TestServer backend) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) { var client = backend.CreateClient(); client.BaseAddress = new("https://localhost:62840/"); return client; }
    }
    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation() => Initialize("https://localhost:62839/", "https://localhost:62839/");
        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException();
    }
    private static TestServer Backend() => new(new WebHostBuilder().UseEnvironment("Testing")
        .ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddAntiforgery();
            services.AddRouting(); services.AddAuthorization();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
            });
        }).Configure(app =>
        {
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                // Test-only login replaces the external eFaas identity provider. No production app or database is started.
                endpoints.MapGet("/Online/Auth/login", async context =>
                {
                    context.Response.Headers["X-Test-Forwarded-Host"] = context.Request.Headers["X-Forwarded-Host"];
                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(new ClaimsIdentity([new Claim("idnumber", "A123456"), new Claim("full_name", "Test Staff")], "efaas")));
                    context.Response.Redirect("/");
                });
                endpoints.MapGet("/Online/Auth/LogoutToken", context =>
                {
                    var tokens = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().GetAndStoreTokens(context);
                    return context.Response.WriteAsJsonAsync(new { formFieldName = tokens.FormFieldName, requestToken = tokens.RequestToken });
                });
                endpoints.MapPost("/Online/Auth/Logout", async context =>
                {
                    try { await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context); }
                    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException) { context.Response.StatusCode = 400; return; }
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); context.Response.Redirect("/Online/Auth/login");
                });
                endpoints.MapGet("/api/v1/session/identity", context => context.Response.WriteAsJsonAsync(new ApiIdentityDto(true,
                    context.User.FindFirst("idnumber")?.Value, context.User.FindFirst("full_name")?.Value))).RequireAuthorization();
            });
        }));

    private static TestServer Ui(TestServer backend, bool components = false) => new(new WebHostBuilder().UseEnvironment("Testing")
        .UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; })
        .ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddSingleton(new BackendOrigins(new("https://localhost:62840/"), new("https://localhost:62839/")));
            services.AddSingleton<BackendSessionStore>(); services.AddSingleton<IHttpClientFactory>(new ClientFactory(backend));
            services.AddAuthentication(BackendAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, BackendAuthenticationHandler>(BackendAuthenticationHandler.SchemeName, _ => { });
            services.AddAuthorization(); services.AddRouting();
            if (components)
            {
                services.AddRazorComponents().AddInteractiveServerComponents(); services.AddCascadingAuthenticationState();
                services.AddHrmApiClients(); services.AddScoped<NavigationManager, TestNavigation>();
            }
        }).Configure(app =>
        {
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/protected", context => context.Response.WriteAsJsonAsync(new { name = context.User.Identity!.Name })).RequireAuthorization();
                endpoints.MapBackendAuthentication();
            });
        }));

    [Fact]
    public async Task Unauthenticated_ui_requests_challenge_the_public_login_route()
    {
        using var backend = Backend(); using var ui = Ui(backend); using var client = ui.CreateClient();
        using var response = await client.GetAsync("/protected");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal("/Online/Auth/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Ui_accepts_only_identity_verified_by_the_api_and_never_exposes_the_backend_handle()
    {
        using var backend = Backend(); using var ui = Ui(backend); using var client = ui.CreateClient();
        using var login = await client.GetAsync("/Online/Auth/login");
        var cookie = login.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        using var request = new HttpRequestMessage(HttpMethod.Get, "/protected"); request.Headers.Add("Cookie", cookie);
        request.Headers.Add("X-User-Id", "another-staff"); request.Headers.Add("X-Organisation-Id", "999");
        using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(); Assert.Contains("Test Staff", body); Assert.DoesNotContain(BackendSessionStore.ClaimType, body);
    }

    [Fact]
    public async Task Forged_cookie_and_identity_headers_cannot_create_a_ui_session()
    {
        using var backend = Backend(); using var ui = Ui(backend); using var client = ui.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add("Cookie", ".AspNetCore.Cookies=forged"); request.Headers.Add("X-User-Id", "1");
        using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Public_logout_proxy_preserves_the_antiforgery_cookie_token_and_redirect()
    {
        using var backend = Backend(); using var ui = Ui(backend); using var client = ui.CreateClient();
        using var login = await client.GetAsync("/Online/Auth/login");
        var cookie = login.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, "/Online/Auth/LogoutToken"); tokenRequest.Headers.Add("Cookie", cookie);
        using var tokenResponse = await client.SendAsync(tokenRequest);
        var token = await tokenResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var securityCookie = tokenResponse.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        using var invalid = new HttpRequestMessage(HttpMethod.Post, "/Online/Auth/Logout") { Content = new FormUrlEncodedContent([]) };
        invalid.Headers.Add("Cookie", cookie);
        using var denied = await client.SendAsync(invalid); Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        using var valid = new HttpRequestMessage(HttpMethod.Post, "/Online/Auth/Logout")
        { Content = new FormUrlEncodedContent(new Dictionary<string, string> { [token.GetProperty("formFieldName").GetString()!] = token.GetProperty("requestToken").GetString()! }) };
        valid.Headers.Add("Cookie", cookie + "; " + securityCookie);
        using var signedOut = await client.SendAsync(valid);
        Assert.Equal(HttpStatusCode.Redirect, signedOut.StatusCode);
        Assert.Equal("/Online/Auth/login", signedOut.Headers.Location!.OriginalString);
        Assert.Contains(signedOut.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".AspNetCore.Cookies=;", StringComparison.Ordinal));
        Assert.Equal("no-store", signedOut.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task Public_proxy_does_not_expose_general_api_routes()
    {
        using var backend = Backend(); using var ui = Ui(backend); using var client = ui.CreateClient();
        using var response = await client.GetAsync("/api/v1/session/identity"); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Every_component_injection_and_reviewed_client_resolves_without_backend_services()
    {
        using var backend = Backend(); using var ui = Ui(backend, components: true); using var scope = ui.Services.CreateScope();
        foreach (var contract in ApplicationContractCatalog.All) Assert.NotNull(scope.ServiceProvider.GetRequiredService(contract));
        var components = typeof(HRM.Components.App).Assembly.GetTypes().Where(t => typeof(ComponentBase).IsAssignableFrom(t) && t.Namespace?.StartsWith("HRM.Components", StringComparison.Ordinal) == true);
        foreach (var component in components)
        foreach (var property in component.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.GetCustomAttribute<InjectAttribute>() is not null))
            Assert.True(scope.ServiceProvider.GetService(property.PropertyType) is not null, $"Missing UI registration: {component.Name}.{property.Name}: {property.PropertyType}");
    }

    [Theory]
    [InlineData("http://localhost:62840/")]
    [InlineData("https://user:password@localhost:62840/")]
    [InlineData("https://localhost:62840/other")]
    public void Invalid_upstream_destinations_fail_at_startup(string value)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Backend:Origin"] = value, ["Application:PublicOrigin"] = "https://localhost:62839/" }).Build();
        Assert.Throws<InvalidOperationException>(() => BackendOrigins.FromConfiguration(config));
    }
}
