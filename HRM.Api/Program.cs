using HRM.Api.Authentication;
using HRM.Hosting;
using HRM.Api.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM;


using HRM.Services.Repositories;
using HRM.Caching;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Builders;
using HRM.WorkPlanning.Services;

using HRM.Services.WorkPlanning;
using HRM.Services.Attendance.Repositories;
using HRM.Services.Attendance.Abstraction;
using HRM.Services.Attendance.Abstraction.Services;
using HRM.WorkPlanning.Services.Schedules;
using HRM.WorkPlanning.Abstractions.Schedules;
using HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment;
using HRM.WorkPlanning.Services.JobWorkTemplateAssignmentService;
using HRM.Services.Interfaces.Profile;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Leave;
using HRM.Services.JobLeaveTypes;
using HRM.Services.Interfaces.JobLeaveTypes;
using HRM.Services.JobPosition;
using HRM.Services.Interfaces.JobPosition;
using HRM.Services.Interfaces.LeaveType;

using HRM.Services.Attendance.AttendancePlan;
using HRM.WorkPlanning;
using HRM.Models.Archives;
using HRM.Services.Attendance.Evaluation;
using HRM.Services.Attendance;
using HRM.Services.Interfaces.AppovalWF;



var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddDbContextFactory<HrmTeContext>(options => options.UseSqlServer(mainConnection));

builder.Services.AddDbContextFactory<AuditDbContext>(options =>
    options.UseSqlServer(auditConnection));


builder.Logging.AddDebug();
builder.Logging.AddConsole();
builder.Services.AddHrmBackendServices();

builder.Services.AddEfaasAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();
if (builder.Environment.IsDevelopment() && CredentialExposure.IsExposed(builder.Configuration["Authentication:OpenIdConnect:ClientSecret"]))
    app.Logger.LogWarning("The development eFaas client secret matches a credential exposed in Git history. Rotate it at the provider and update the development secret store.");
app.UseTrustedProxyHeaders();

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
app.MapGet("/Online/Auth/login", () =>
{
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        new[] { OpenIdConnectDefaults.AuthenticationScheme }
    );
}).AllowAnonymous();

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
