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

using HRM.WorkPlanning.Services.TestServicesToBeRemoved;
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
using HRM.Services.Interfaces.Policy;
using HRM.Services.Attendance.AttendancePlan;
using HRM.WorkPlanning;
using HRM.Models.Archives;
using HRM.Services.Attendance.Evaluation;
using HRM.Services.Attendance;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();



builder.Logging.AddDebug();
builder.Logging.AddConsole();


builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });



builder.Services.AddDbContextFactory<HrmTeContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContextFactory<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuditConnectionStrings")));

builder.Services.AddScoped<ISequenceNumberService, SequenceNumberService>();
builder.Services.AddScoped<IRequestService, RequestService>();



/// Should be removed
builder.Services.AddScoped<IWorkPlanningTestService,WorkPlanningTestService>();

builder.Services.AddScoped<WorkAssignmentResolverTestService>();


////////////////////////////////////////////////////////////////////////////////
///

builder.Services.AddScoped<
    IAttendanceSegmentEvaluator,
    AttendanceSegmentEvaluator>();


builder.Services.AddScoped<
    IAttendanceWorkPlanResolver,
    AttendanceWorkPlanResolver>();

builder.Services.AddScoped<IWorkPlanGenerator, WorkPlanGenerator>();

builder.Services.AddScoped<IJobWorkTemplateService, JobWorkTemplateService>();

builder.Services.AddScoped<
    ILeaveDefinitionService,
    LeaveDefinitionService>();

builder.Services.AddScoped<
    ILeaveFrameworkResolver,
    LeaveFrameworkResolver>();

builder.Services.AddScoped<
    ILeavePolicyService,
    LeavePolicyService>();

builder.Services.AddScoped<
    ILeavePolicyAccrualRuleService,
    LeavePolicyAccrualRuleService>();

builder.Services.AddScoped<
    IAttendanceResolver,
    AttendanceResolver>();


builder.Services.AddScoped<
    IAttendanceEvaluationService,
    AttendanceEvaluationService>();





builder.Services.AddScoped<UserContext, UserContext>();


builder.Services.AddScoped<ILeaveTypeService,LeaveTypeService>();

builder.Services.AddScoped<IJobPosition, JobPositionService>();

builder.Services.AddScoped<IProfileService, ProfileService>();

builder.Services.AddScoped<IJobWorkTemplateAssignmentService,JobWorkTemplateAssignmentService>();

builder.Services.AddScoped<IStaffScheduleService, StaffScheduleService>();

builder.Services.AddScoped<IManualWorkAssignmentService,ManualWorkAssignmentService>();

builder.Services.AddScoped<IManualWorkAssignmentLookupService,ManualWorkAssignmentLookupService>();

builder.Services.AddScoped<ILeaveTypeMappingService,LeaveTypeMappingService>();

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




builder.Services.AddScoped<IJobLeaveTypeService, JobLeaveTypeService>();
builder.Services.AddScoped<IEffectivePermissionService,EffectivePermissionService>();
builder.Services.AddScoped<IUserContextBuilder, UserContextBuilder>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<IAttendanceRequestResourceBuilder,AttendanceRequestResourceBuilder>();
builder.Services.AddScoped<ILeaveResourceBuilder,LeaveResourceBuilder>();
builder.Services.AddScoped<IIndividualResourceBuilder,IndividualResourceBuilder>();
builder.Services.AddScoped<IBusinessAccessService,BusinessAccessService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
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


builder.Services.AddScoped<LeaveApplicationState>();
builder.Services.AddScoped<LeaveDataLoader>();
builder.Services.AddScoped<LeaveStore>();
builder.Services.AddScoped<UserAppService>();
builder.Services.AddScoped<PortalContext>();
builder.Services.AddScoped<UserRepository>();


builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IJobDataLoader, JobDataLoader>();
builder.Services.AddScoped<IJobStore, JobStore>();
builder.Services.AddScoped<IAttendanceLogDataLoader, AttendanceLogDataLoader>();
builder.Services.AddScoped<IGeographyService, GeographyService>();
builder.Services.AddScoped<IIndividual, IndividualService>();
builder.Services.AddScoped<IAddress, AddressService>();
builder.Services.AddScoped<ITeam, TeamService>();

//builder.Services.AddScoped<AttendanceLogService, AttendanceLogService>();


//builder.Services.AddScoped<AuditLogger>();



builder.Services.AddScoped(sp =>
{
    // We use NavigationManager to automatically get the current URL (e.g., https://localhost:7123/)
    // This ensures your relative API calls like "api/leavetypes" work perfectly.
    var navManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navManager.BaseUri) };
});


// 1. Add Blazor Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Add Controller Services (Required to trigger the Login/Logout redirects)
builder.Services.AddControllers();

// 3. Configure OpenID Connect & Cookie Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Authentication:OpenIdConnect:Authority"];
    options.ClientId = builder.Configuration["Authentication:OpenIdConnect:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:OpenIdConnect:ClientSecret"];

    options.CallbackPath = "/Online/Auth/Callback";
    options.ResponseType = "code";
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

    //options.Events = new OpenIdConnectEvents
    //{
    //    // This runs after eFaas returns data from:
    //    // https://developer.gov.mv/efaas/connect/userinfo
    //    OnUserInformationReceived = async context =>
    //    {
    //        var userInfo = context.User.RootElement;

    //        var idNumber = userInfo.TryGetProperty("idnumber", out var idNumberValue)
    //            ? idNumberValue.GetString()
    //            : null;


    //        //var ssoUserKey = userInfo.TryGetProperty("sub", out var subValue)
    //        //  ? subValue.GetString()
    //        //  : null;

    //        var ssoUserKeyText = userInfo.TryGetProperty("sub", out var subValue)
    //            ? subValue.GetString()
    //            : null;

    //        Guid? ssoUserKey = Guid.TryParse(ssoUserKeyText, out var parsedSsoUserKey)
    //            ? parsedSsoUserKey
    //            : null;



    //        var fullName = userInfo.TryGetProperty("full_name", out var fullNameValue)
    //            ? fullNameValue.GetString()
    //            : null;

    //        var email = userInfo.TryGetProperty("email", out var emailValue)
    //            ? emailValue.GetString()
    //            : null;

    //        var mobile = userInfo.TryGetProperty("mobile", out var mobileValue)
    //            ? mobileValue.GetString()
    //            : null;

    //        if (string.IsNullOrWhiteSpace(idNumber))
    //        {
    //            context.Fail("eFaas UserInfo did not return idnumber.");
    //            return;
    //        }



    //        var dbFactory = context.HttpContext.RequestServices
    //            .GetRequiredService<IDbContextFactory<HrmTeContext>>();

    //        await using var db =
    //            await dbFactory.CreateDbContextAsync(context.HttpContext.RequestAborted);


    //        var user = await db.Users
    // .FirstOrDefaultAsync(x =>
    //     x.Username == idNumber ||
    //     (ssoUserKey.HasValue && x.SsouserKey == ssoUserKey.Value));

    //        if (user == null)
    //        {
    //            user = new User
    //            {
    //                // UserID is generated by SQL Server.
    //                SsouserKey = ssoUserKey ?? Guid.NewGuid(),
    //                Username = idNumber,
    //                FullName = fullName ?? idNumber,
    //                Email = email,
    //                Mobile = mobile,

    //                ApplicationName = "HRM",
    //                Comment = "Created automatically from eFaas sign-in.",

    //                CreationDate = DateTime.Now,
    //                LastUpdatedDate = DateTime.Now,

    //                IsOnline = true,

    //                // Choose these according to your approval policy.
    //                IsActive = true,
    //                IsApproved = true

    //                // Set required values if these columns are NOT NULL in your database:
    //                // BusinessEntityID = ...,
    //                // UserOrganisationID = ...,
    //                // LocalUserStateID = ...,
    //                // SSOUserStateID = ...
    //            };

    //            db.Users.Add(user);
    //        }
    //        else
    //        {
    //            // Refresh profile details from eFaas without changing local permissions/states.
    //            user.SsouserKey = ssoUserKey ?? user.SsouserKey;
    //            user.FullName = fullName ?? user.FullName;
    //            user.Email = email ?? user.Email;
    //            user.Mobile = mobile ?? user.Mobile;
    //            user.IsOnline = true;
    //            user.LastUpdatedDate = DateTime.Now;
    //        }

    //        await db.SaveChangesAsync(context.HttpContext.RequestAborted);
    //    }
    //};










});

// 4. Add Blazor Cascading Authentication State
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


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

app.UseRouting();

app.UseSession();



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

app.MapGet("/Online/Auth/NoAccessLogout", (HttpContext context) =>
{
    context.Session.SetString("AccessDeniedMessage", "1");

    return Results.SignOut(
        new AuthenticationProperties
        {
            RedirectUri = "/"
        },
        new[]
        {
            "Cookies",
            OpenIdConnectDefaults.AuthenticationScheme
        });
})
.AllowAnonymous();


app.Run();