using HRM.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM;
using HRM.Services.State;
using HRM.Services.Stores;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WorkingHoursSettingsService>();
builder.Services.AddScoped<JobTypeOvertimeSettingsService>();
builder.Services.AddScoped<ScopeScheduleSettingsService>();
builder.Services.AddScoped<GroupsManagementService>();
builder.Services.AddScoped<LeaveConfigurationSettingsService>();
builder.Services.AddScoped<IndividualRegistrationService>();
builder.Services.AddScoped<OvertimeRequestService>();
builder.Services.AddScoped<FormerStaffService>();
builder.Services.AddScoped<WeeklyWorkingHoursService>();
builder.Services.AddScoped<WeeklyHourExceptionService>();



builder.Logging.AddDebug();
builder.Logging.AddConsole();


builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });



var mainConnection = HRM.Helpers.DatabaseConnectionPolicy.Validate(
    builder.Configuration.GetConnectionString("DefaultConnection"), builder.Environment.IsDevelopment(), "ConnectionStrings:DefaultConnection");
var configuredAuditConnection = builder.Configuration.GetConnectionString("AuditConnection");
if (string.IsNullOrWhiteSpace(configuredAuditConnection)) configuredAuditConnection = builder.Configuration["AuditConnectionStrings:DefaultConnection"];
var auditConnection = HRM.Helpers.DatabaseConnectionPolicy.Validate(
    configuredAuditConnection, builder.Environment.IsDevelopment(), "ConnectionStrings:AuditConnection");
builder.Services.AddDbContextFactory<HrmTeContext>(options => options.UseSqlServer(mainConnection));

builder.Services.AddDbContextFactory<AuditDbContext>(options =>
    options.UseSqlServer(auditConnection));

builder.Services.AddScoped<ISequenceNumberService, SequenceNumberService>();
builder.Services.AddScoped<IRequestService, RequestService>();





////////////////////////////////////////////////////////////////////////////////
///

builder.Services.AddScoped<
    IAttendanceSegmentEvaluator,
    AttendanceSegmentEvaluator>();


builder.Services.AddScoped<
    IAttendanceWorkPlanResolver,
    AttendanceWorkPlanResolver>();

builder.Services.AddScoped<IWorkPlanGenerator, WorkPlanGenerator>();
builder.Services.AddScoped<HRM.WorkPlanning.Services.DutyRosterService>();

builder.Services.AddScoped<IJobWorkTemplateService, JobWorkTemplateService>();

builder.Services.AddScoped<
    ILeaveTypeCatalogueService,
    LeaveTypeCatalogueService>();



builder.Services.AddScoped<
    ILeavePolicyService,
    LeavePolicyService>();



builder.Services.AddScoped<
    IAttendanceResolver,
    AttendanceResolver>();


builder.Services.AddScoped<
    IAttendanceEvaluationService,
    AttendanceEvaluationService>();


builder.Services.AddScoped<
    IWorkflowDefinitionService,
    WorkflowDefinitionService>();

builder.Services.AddScoped<
    IWorkflowApproverResolver,
    WorkflowApproverResolver>();


builder.Services.AddScoped<
    IWorkflowRequestContextResolver,
    WorkflowRequestContextResolver>();

builder.Services.AddScoped<
    IWorkflowEngine,
    WorkflowEngine>();


builder.Services.AddScoped<
    IWorkflowActorProvider,
    WorkflowActorProvider>();



builder.Services.AddScoped<UserContext, UserContext>();




builder.Services.AddScoped<IJobPosition, JobPositionService>();

builder.Services.AddScoped<IProfileService, ProfileService>();

builder.Services.AddScoped<IJobWorkTemplateAssignmentService,JobWorkTemplateAssignmentService>();

builder.Services.AddScoped<IStaffScheduleService, StaffScheduleService>();

builder.Services.AddScoped<IManualWorkAssignmentService,ManualWorkAssignmentService>();

builder.Services.AddScoped<IManualWorkAssignmentLookupService,ManualWorkAssignmentLookupService>();



builder.Services.AddScoped<IWorkPlanService, WorkPlanService>();

builder.Services.AddScoped<IWorkTemplateService, WorkTemplateService>();

builder.Services.AddScoped<IWorkAssignmentManagementService,WorkAssignmentManagementService>();

builder.Services.AddScoped<IWorkAssignmentOwnershipService,WorkAssignmentOwnershipService>();


builder.Services.AddScoped<IAttendanceDuplicateValidator,AttendanceDuplicateValidator>();

builder.Services.AddScoped<IAttendanceLogProcessor,AttendanceLogProcessor>();




//builder.Services.AddScoped<IAttendancePlanService, AttendancePlanService>();


builder.Services.AddScoped<IAttendancePlanService,AttendancePlanService>();


builder.Services.AddScoped<IAttendanceLogResolutionRepository, AttendanceLogResolutionRepository>();

builder.Services.AddScoped<IWorkAssignmentGenerator, WorkAssignmentGeneratorService>();

builder.Services.AddScoped<IWorkAssignmentResolver,WorkAssignmentResolver>();


builder.Services.AddScoped<IWorkPlanningService,WorkPlanningService>();

builder.Services.AddScoped<IWorkPlanBuilder,WorkPlanBuilder>();

builder.Services.AddScoped<IPlanningProviderResolver, PlanningProviderResolver>();
 


//builder.Services.AddScoped<IWorkTemplateResolver,WorkTemplateResolver>();

//builder.Services.AddScoped<IWorkAssignmentBuilder,WorkAssignmentBuilder>();

//builder.Services.AddScoped<IWorkSegmentBuilder,WorkSegmentBuilder>();

//builder.Services.AddScoped<IWorkOwnershipBuilder,WorkOwnershipBuilder>();


builder.Services.AddScoped<IWorkPlanValidator, WorkPlanValidator>();




builder.Services.AddScoped<HRM.Services.Leave.LeaveHandoverRuleSettingsService>();
builder.Services.AddScoped<IJobLeaveTypeService, JobLeaveTypeService>();
builder.Services.AddScoped<IEffectivePermissionService,EffectivePermissionService>();
builder.Services.AddScoped<IUserContextBuilder, UserContextBuilder>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<IIndividualRegistryService, IndividualRegistryService>();
builder.Services.AddScoped<IWorkspaceDirectoryService, WorkspaceDirectoryService>();
builder.Services.AddScoped<IMyAttendanceSupportService, MyAttendanceSupportService>();
builder.Services.AddScoped<IAttendanceRequestResourceBuilder,AttendanceRequestResourceBuilder>();
builder.Services.AddScoped<ILeaveResourceBuilder,LeaveResourceBuilder>();
builder.Services.AddScoped<IIndividualResourceBuilder,IndividualResourceBuilder>();
builder.Services.AddScoped<IBusinessAccessService,BusinessAccessService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService>(sp => sp.GetRequiredService<UserService>());
builder.Services.AddScoped<UserPreferenceService>();
builder.Services.AddScoped<StaffChatService>();
builder.Services.AddScoped<WorkspaceRecentSearchService>();
builder.Services.AddScoped<RequestNotificationService>();
builder.Services.AddScoped<HRM.Services.Leave.LeaveAccrualSettingsService>();
builder.Services.AddScoped<HRM.Services.Leave.LeaveCarryForwardService>();
builder.Services.AddHostedService<HRM.Services.Leave.LeaveAccrualWorker>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserContextCache, UserContextCache>();
 
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<ToastService>();


builder.Services.AddScoped<AttendanceLogService>();

 


builder.Services.AddScoped<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<IOperationLogService, OperationLogService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<ILeaveReceiptService, LeaveReceiptService>();
builder.Services.AddScoped<LeaveFlowConfigurationService>();
builder.Services.AddScoped<RequestTransitionSettingsService>();
builder.Services.AddScoped<HRM.Services.Leave.LeaveHandoverService>();
builder.Services.AddScoped<HRM.Services.Leave.LeaveCalendarService>();
builder.Services.AddScoped<HRM.Services.OrganisationWorkspaceService>();
builder.Services.AddScoped<IUserTodoService, UserTodoService>();
builder.Services.AddScoped<ILeaveChangeService, HRM.Services.Leave.LeaveChangeService>();
builder.Services.AddScoped<HRM.Services.JobTypeConfigurationService>();
builder.Services.AddScoped<HRM.Services.StaffOnboardingService>();
builder.Services.AddScoped<HRM.Services.PositionOverviewService>();
builder.Services.AddScoped<HRM.Services.StaffPositionManagementService>();
builder.Services.AddScoped<HRM.Services.PermissionSettingsService>();
builder.Services.AddScoped<HRM.Services.UserSettingsService>();
builder.Services.AddScoped<HRM.Services.AdminAccessGrantService>();
builder.Services.AddScoped<HRM.Services.LateCheckInRequestService>();
builder.Services.AddScoped<HRM.Services.OrganisationPermissionService>();
builder.Services.AddScoped<HRM.Services.OrganisationResourceSettingsService>();
builder.Services.AddScoped<HRM.Services.OrganisationSettingsService>();
builder.Services.AddScoped<HRM.Services.SupervisorChangeRequestService>();
builder.Services.AddScoped<HRM.Services.HrRequestInboxService>();
builder.Services.AddScoped<HRM.Services.LeaveSetConfigurationService>();


builder.Services.AddScoped<LeaveApplicationState>();

builder.Services.AddScoped<LeaveStore>();
builder.Services.AddUserAppData();
builder.Services.AddScoped<HRM.Application.Contracts.Pages.ILeaveApplications, HRM.Services.Transport.LeaveApplicationsService>();
builder.Services.AddScoped<PortalContext>();
builder.Services.AddScoped<UserRepository>();


builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAttendanceRequestService, AttendanceRequestService>();
builder.Services.AddScoped<IJobDataLoader, JobDataLoader>();

builder.Services.AddScoped<IAttendanceLogDataLoader, AttendanceLogDataLoader>();
builder.Services.AddScoped<IGeographyService, GeographyService>();
builder.Services.AddScoped<IIndividual, IndividualService>();
builder.Services.AddScoped<IAddress, AddressService>();
builder.Services.AddScoped<ITeam, TeamService>();

//builder.Services.AddScoped<AttendanceLogService, AttendanceLogService>();


//builder.Services.AddScoped<AuditLogger>();





// 1. Add Blazor Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Add Controller Services (Required to trigger the Login/Logout redirects)
// MVC antiforgery authorization filters require the view-feature services.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization();

// 3. Configure OpenID Connect & Cookie Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Authentication:OpenIdConnect:Authority"];
    options.ClientId = builder.Configuration["Authentication:OpenIdConnect:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:OpenIdConnect:ClientSecret"];

    options.CallbackPath = "/Online/Auth/Callback";
    options.SignedOutRedirectUri = "/Online/Auth/login";
    options.ResponseType = "code";
    options.UsePkce = true;
    options.RequireHttpsMetadata = true;
    options.SaveTokens = true;

    options.GetClaimsFromUserInfoEndpoint = true;

    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        // Tell ASP.NET Core: "Whenever you need the user's name, use the 'full_name' claim from eFaas"
        NameClaimType = "full_name"
    };

    ////query["scope"] = "openid efaas.profile efaas.email efaas.mobile efaas.birthdate";   // openid MUST be included   
    ////options.TokenValidationParameters.NameClaimType = "name";
    //options.GetClaimsFromUserInfoEndpoint = true;
    //options.SaveTokens = true;
    //options.Scope.Clear();
    //options.Scope.Add("openid");
    //options.Scope.Add("efaas.profile");
    //options.Scope.Add("efaas.email");

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("efaas.profile");
    options.Scope.Add("efaas.email");
    options.Scope.Add("efaas.mobile");
    options.Scope.Add("efaas.birthdate");

    // Claims to keep in authState.User after eFaas login
    options.ClaimActions.MapUniqueJsonKey("idnumber", "idnumber");
    options.ClaimActions.MapUniqueJsonKey("full_name", "full_name");
    options.ClaimActions.MapUniqueJsonKey("first_name", "first_name");
    options.ClaimActions.MapUniqueJsonKey("last_name", "last_name");
    options.ClaimActions.MapUniqueJsonKey("email", "email");
    options.ClaimActions.MapUniqueJsonKey("mobile", "mobile");
    options.ClaimActions.MapUniqueJsonKey("dob", "dob");
    options.ClaimActions.MapUniqueJsonKey("photo", "photo");
    options.ClaimActions.MapUniqueJsonKey("user_type", "user_type");
    options.ClaimActions.MapUniqueJsonKey("sub", "sub");
    options.ClaimActions.MapUniqueJsonKey("middle_name", "middle_name");
    options.ClaimActions.MapUniqueJsonKey("given_name", "given_name");
    options.ClaimActions.MapUniqueJsonKey("family_name", "family_name");

 








});

// 4. Add Blazor Cascading Authentication State
builder.Services.AddCascadingAuthenticationState();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
// This creates the Login route without needing a Controller file
app.MapGet("/Online/Auth/login", () =>
{
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        new[] { OpenIdConnectDefaults.AuthenticationScheme }
    );
});

app.UseHttpsRedirection();

app.UseStaticFiles();

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


app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// 6. Map the controllers so /Account/Login and /Account/Logout work
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapGet("/Online/Auth/Logout", async context =>
//{
//    await context.SignOutAsync("Cookies");

//    context.Response.Redirect("/");
//})
//.AllowAnonymous();

//app.MapGet("/Online/Auth/Logout", () =>
//{
//    return Results.SignOut(
//        new AuthenticationProperties
//        {
//            RedirectUri = "/?access=denied"
//        },
//        new[]
//        {
//            "Cookies",
//            OpenIdConnectDefaults.AuthenticationScheme
//        });
//})
//.AllowAnonymous();

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
