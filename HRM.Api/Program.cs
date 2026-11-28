using HRM.Api.Authentication;
using HRM.Hosting;
using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using HRM.Models;
using HRM;
using HRM.ErrorLogs.Extensions;
using HRM.ErrorLogs.Services;
using HRM.Services.Diagnostics;
using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Extensions;
using HRM.AuditLogs.Interceptors;
using HRM.AuditLogs.Persistence;
using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Extensions;
using HRM.ActivityLogs.Persistence;



var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddSecureExternalConnectionStrings();
// Edge ingestion independently validates an enrolled client certificate.  Other HTTPS endpoints remain usable without one.
builder.WebHost.ConfigureKestrel(options => options.ConfigureHttpsDefaults(https =>
{
    https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
    https.AllowAnyClientCertificate();
}));
RuntimeRequirements.RequireCurrent(builder.Environment);
var publicOrigin = HRM.Application.Transport.DeploymentOrigin.Require(builder.Configuration["Application:PublicOrigin"], "Application:PublicOrigin", !builder.Environment.IsDevelopment());
builder.Services.AddProtectedHostKeys(builder.Configuration, builder.Environment, "HRM.Api");
builder.Services.AddTrustedProxyHeaders(builder.Configuration, publicOrigin);
var mainConnection = HRM.Helpers.DatabaseConnectionPolicy.Validate(
    builder.Configuration.GetConnectionString("DefaultConnection"), builder.Environment.IsDevelopment(), "ConnectionStrings:DefaultConnection");
var configuredAuditConnection = builder.Configuration.GetConnectionString("AuditConnection");
if (string.IsNullOrWhiteSpace(configuredAuditConnection)) configuredAuditConnection = builder.Configuration["AuditConnectionStrings:DefaultConnection"];
var auditConnection = HRM.Helpers.DatabaseConnectionPolicy.Validate(
    configuredAuditConnection, builder.Environment.IsDevelopment(), "ConnectionStrings:AuditConnection");
var configuredErrorLogsConnection = builder.Configuration.GetConnectionString("ErrorLogsConnection");
if (string.IsNullOrWhiteSpace(configuredErrorLogsConnection))
{
    var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(mainConnection) { InitialCatalog = "HrmErrorLogs" };
    configuredErrorLogsConnection = csb.ConnectionString;
}
var errorLogsConnection = HRM.Helpers.DatabaseConnectionPolicy.Validate(
    configuredErrorLogsConnection, builder.Environment.IsDevelopment(), "ConnectionStrings:ErrorLogsConnection");
var configuredAuditLogsConnection = builder.Configuration.GetConnectionString("AuditLogsConnection");
if (string.IsNullOrWhiteSpace(configuredAuditLogsConnection))
{
    var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(mainConnection) { InitialCatalog = "HRMAuditLogs" };
    configuredAuditLogsConnection = csb.ConnectionString;
}
var auditLogsConnection = HRM.Helpers.DatabaseConnectionPolicy.Validate(
    configuredAuditLogsConnection, builder.Environment.IsDevelopment(), "ConnectionStrings:AuditLogsConnection");
builder.Services.AddSingleton<IAuditContextAccessor, HrmAuditContextAccessor>();
builder.Services.AddAuditLogs(builder.Configuration, opt =>
{
    opt.ConnectionString = auditLogsConnection;
});

var configuredActivityLogsConnection = builder.Configuration.GetConnectionString("ActivityLogsConnection");
if (string.IsNullOrWhiteSpace(configuredActivityLogsConnection))
{
    var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(mainConnection) { InitialCatalog = "HRMActivityLogs" };
    configuredActivityLogsConnection = csb.ConnectionString;
}
var activityLogsConnection = HRM.Helpers.DatabaseConnectionPolicy.Validate(
    configuredActivityLogsConnection, builder.Environment.IsDevelopment(), "ConnectionStrings:ActivityLogsConnection");
builder.Services.AddSingleton<IActivityContextAccessor, HrmActivityContextAccessor>();
builder.Services.AddActivityLogs(builder.Configuration, includeBackgroundWorkers: true, configure: opt =>
{
    opt.ConnectionString = activityLogsConnection;
});

builder.Services.AddDbContextFactory<HrmTeContext>((sp, options) =>
{
    options.UseSqlServer(mainConnection);
    options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});

builder.Services.AddDbContextFactory<HRM.AuditDbContext>(options =>
    options.UseSqlServer(auditConnection));


builder.Logging.AddDebug();
builder.Logging.AddConsole();
builder.Services.AddSingleton<IErrorLogStore>(sp => new SqlErrorLogStore(builder.Configuration, sp.GetService<ILogger<SqlErrorLogStore>>(), errorLogsConnection));
builder.Services.AddHrmErrorLogging("HRM.Api");
builder.Services.AddHrmBackendServices();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(
    options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(
    options => options.Level = System.IO.Compression.CompressionLevel.Fastest);

builder.Services.AddEfaasAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddHrmRateLimiting(builder.Configuration);
builder.Services.AddHrmHealthChecks(mainConnection, auditLogsConnection, activityLogsConnection);

var app = builder.Build();
var runStartupBootstrap = builder.Configuration.GetValue<bool?>("Database:RunStartupSchemaBootstrap")
                          ?? app.Environment.IsDevelopment();
if (runStartupBootstrap || app.Environment.IsDevelopment())
{
    await AuditSchemaBootstrapper.EnsureSchemaAsync(app.Services);
    await ActivitySchemaBootstrapper.EnsureSchemaAsync(app.Services);
}
if (runStartupBootstrap)
{
    _ = Task.Run(async () =>
    {
        await HRM.Services.Attendance.Reconciliation.AttendanceReconciliationSchemaBootstrapper.EnsureSchemaAsync(app.Services);
        await HRM.Helpers.OrganisationalAccessScopeResolver.EnsureSchemaAsync(app.Services);
    });
}
app.UseResponseCompression();
if (builder.Environment.IsDevelopment() && CredentialExposure.IsExposed(builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]))
    app.Logger.LogWarning("The development eFaas client secret matches a credential exposed in Git history. Rotate it at the provider and update the development secret store.");
app.UseTrustedProxyHeaders();
app.UseHrmErrorLogging("HRM.Api");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(handler => handler.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { message = "This action could not be completed. Please try again." });
    }));
    app.UseHsts();
}
// This creates the Login route without needing a Controller file
app.MapGet("/Online/Auth/login", (string? returnUrl) =>
{
    var safeRedirect = !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//")
        && !returnUrl.StartsWith("/\\")
        ? returnUrl
        : "/";
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = safeRedirect },
        new[] { OpenIdConnectDefaults.AuthenticationScheme }
    );
}).AllowAnonymous().RequireRateLimiting(HrmRateLimitPolicies.Auth);

app.UseHttpsRedirection();



app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'; object-src 'none'; base-uri 'self'";
    await next();
});

app.UseRouting();




// 5. Add Authentication Middleware (MUST be before Antiforgery and Routing)
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHrmHealthEndpoints();
app.MapControllers();
app.UseAntiforgery();




// 6. Map the controllers so /Account/Login and /Account/Logout work
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Direct links open confirmation; only the antiforgery-protected POST signs out.
app.MapGet("/Online/Auth/Logout", () => Results.LocalRedirect("/Online/Auth/NoAccessLogout"))
    .AllowAnonymous();

// Legacy GET links show confirmation. Only the antiforgery-protected POST signs out.
app.MapGet("/Online/Auth/NoAccessLogout", (HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    var field = System.Net.WebUtility.HtmlEncode(tokens.FormFieldName);
    var token = System.Net.WebUtility.HtmlEncode(tokens.RequestToken);
    context.Response.Headers.CacheControl = "no-store";
    return Results.Content($"""
        <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Sign out</title></head>
        <body><main><h1>Sign out</h1><p>Sign out of your current account to sign in again.</p>
        <form action="/Online/Auth/Logout" method="post"><input type="hidden" name="{field}" value="{token}"><button type="submit">Sign out</button></form>
        <p><a href="/">Return to home</a></p></main></body></html>
        """, "text/html; charset=utf-8");
})
.AllowAnonymous();


app.Run();
