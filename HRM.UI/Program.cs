using HRM.Components;
using HRM.UI.Authentication;
using HRM.UI.Services.Transport;
using Microsoft.AspNetCore.Authentication;
using HRM.Hosting;

var builder = WebApplication.CreateBuilder(args);
RuntimeRequirements.RequireCurrent(builder.Environment);
var origins = BackendOrigins.FromConfiguration(builder.Configuration, !builder.Environment.IsDevelopment());
builder.Services.AddProtectedHostKeys(builder.Configuration, builder.Environment, "HRM.UI");
builder.Services.AddTrustedProxyHeaders(builder.Configuration, origins.Public);
builder.Services.AddSingleton(origins);
builder.Services.AddSingleton<BackendSessionStore>();
builder.Services.AddHttpClient("backend", client => { client.BaseAddress = origins.Api; client.Timeout = TimeSpan.FromSeconds(60); })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false });
builder.Services.AddAuthentication(BackendAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BackendAuthenticationHandler>(BackendAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => { options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHrmApiClients();
var app = builder.Build();
app.UseTrustedProxyHeaders();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'; object-src 'none'; base-uri 'self'";
    await next();
});
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapBackendAuthentication();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
