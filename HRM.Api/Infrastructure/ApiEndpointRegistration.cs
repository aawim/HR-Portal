using HRM.Application.Contracts.Pages;
using HRM.Services.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace HRM.Api.Infrastructure;

public static class ApiEndpointRegistration
{
    public static IServiceCollection AddHrmApiEndpoints(this IServiceCollection services)
    {
        services.AddScoped<ApiExceptionFilter>();
        services.AddScoped<ApiSessionFilter>();
        services.AddScoped<ApiReadScope>();
        services.AddScoped<IStaffFinance, HRM.Services.StaffFinanceService>();
        services.AddScoped<ICurrentEmployeeSession, CurrentEmployeeSession>();
        services.AddScoped<IEmployeeAddresses, EmployeeAddresses>();
        services.AddScoped<IAttendanceRequestQueries, AttendanceRequestQueries>();
        services.AddScoped<ILeaveRequestWorkflowQueries, LeaveRequestWorkflowQueries>();
        services.AddScoped<IOrganisationDirectoryQueries, OrganisationDirectoryQueries>();
        services.AddScoped<IOrganisationAttendanceQueries, OrganisationAttendanceQueries>();
        services.AddScoped<IOutOfOfficeRequests, OutOfOfficeRequests>();
        services.TryAddScoped<ILeaveApplications, HRM.Services.Transport.LeaveApplicationsService>();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
        services.TryAddScoped<global::HRM.Services.UserPreferenceService>();
        services.TryAddScoped<global::HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.UserPreferenceService>());
        services.TryAddScoped<global::HRM.Services.LateCheckInRequestService>();
        services.TryAddScoped<global::HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.LateCheckInRequestService>());
        services.TryAddScoped<global::HRM.Services.RequestNotificationService>();
        services.TryAddScoped<global::HRM.Services.Contracts.RequestNotificationServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.RequestNotificationService>());
        services.TryAddScoped<global::HRM.Services.OvertimeRequestService>();
        services.TryAddScoped<global::HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.OvertimeRequestService>());
        services.TryAddScoped<global::HRM.Services.StaffPositionManagementService>();
        services.TryAddScoped<global::HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.StaffPositionManagementService>());
        services.TryAddScoped<global::HRM.Services.SupervisorChangeRequestService>();
        services.TryAddScoped<global::HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.SupervisorChangeRequestService>());
        services.TryAddScoped<global::HRM.Services.WeeklyHourExceptionService>();
        services.TryAddScoped<global::HRM.Services.Contracts.WeeklyHourExceptionServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.WeeklyHourExceptionService>());
        services.TryAddScoped<global::HRM.Services.WeeklyWorkingHoursService>();
        services.TryAddScoped<global::HRM.Services.Contracts.WeeklyWorkingHoursServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.WeeklyWorkingHoursService>());
        services.TryAddScoped<global::HRM.Services.AttendanceLogService>();
        services.TryAddScoped<global::HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.AttendanceLogService>());
        services.TryAddScoped<global::HRM.Services.StaffChatService>();
        services.TryAddScoped<global::HRM.Services.Contracts.StaffChatServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.StaffChatService>());
        services.TryAddScoped<global::HRM.WorkPlanning.Services.DutyRosterService>();
        services.TryAddScoped<global::HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.WorkPlanning.Services.DutyRosterService>());
        services.TryAddScoped<global::HRM.Services.Leave.LeaveHandoverService>();
        services.TryAddScoped<global::HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.Leave.LeaveHandoverService>());
        services.TryAddScoped<global::HRM.Services.Leave.LeaveCalendarService>();
        services.TryAddScoped<global::HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.Leave.LeaveCalendarService>());
        services.TryAddScoped<global::HRM.Services.LeaveConfigurationSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.LeaveConfigurationSettingsService>());
        services.TryAddScoped<global::HRM.Services.LeaveSetConfigurationService>();
        services.TryAddScoped<global::HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.LeaveSetConfigurationService>());
        services.TryAddScoped<global::HRM.Services.Leave.LeaveHandoverRuleSettingsService>();
        services.TryAddScoped<global::HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.Leave.LeaveHandoverRuleSettingsService>());
        services.TryAddScoped<global::HRM.Services.Leave.LeaveAccrualSettingsService>();
        services.TryAddScoped<global::HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.Leave.LeaveAccrualSettingsService>());
        services.TryAddScoped<global::HRM.Services.Leave.LeaveCarryForwardService>();
        services.TryAddScoped<global::HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.Leave.LeaveCarryForwardService>());
        services.TryAddScoped<global::HRM.Services.Leave.LeaveFlowConfigurationService>();
        services.TryAddScoped<global::HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.Leave.LeaveFlowConfigurationService>());
        services.TryAddScoped<global::HRM.Services.RequestTransitionSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.RequestTransitionSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.RequestTransitionSettingsService>());
        services.TryAddScoped<global::HRM.Services.WorkingHoursSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.WorkingHoursSettingsService>());
        services.TryAddScoped<global::HRM.Services.ScopeScheduleSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.ScopeScheduleSettingsService>());
        services.TryAddScoped<global::HRM.Services.AdminAccessGrantService>();
        services.TryAddScoped<global::HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.AdminAccessGrantService>());
        services.TryAddScoped<global::HRM.Services.UserSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.UserSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.UserSettingsService>());
        services.TryAddScoped<global::HRM.Services.PermissionSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.PermissionSettingsService>());
        services.TryAddScoped<global::HRM.Services.PositionOverviewService>();
        services.TryAddScoped<global::HRM.Services.Contracts.PositionOverviewServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.PositionOverviewService>());
        services.TryAddScoped<global::HRM.Services.OrganisationResourceSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.OrganisationResourceSettingsService>());
        services.TryAddScoped<global::HRM.Services.OrganisationPermissionService>();
        services.TryAddScoped<global::HRM.Services.Contracts.OrganisationPermissionServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.OrganisationPermissionService>());
        services.TryAddScoped<global::HRM.Services.OrganisationSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.OrganisationSettingsService>());
        services.TryAddScoped<global::HRM.Services.JobTypeConfigurationService>();
        services.TryAddScoped<global::HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.JobTypeConfigurationService>());
        services.TryAddScoped<global::HRM.Services.JobTypeOvertimeSettingsService>();
        services.TryAddScoped<global::HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.JobTypeOvertimeSettingsService>());
        services.TryAddScoped<global::HRM.Services.WorkspaceRecentSearchService>();
        services.TryAddScoped<global::HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.WorkspaceRecentSearchService>());
        services.TryAddScoped<global::HRM.Services.OrganisationWorkspaceService>();
        services.TryAddScoped<global::HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.OrganisationWorkspaceService>());
        services.TryAddScoped<global::HRM.Services.StaffOnboardingService>();
        services.TryAddScoped<global::HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.StaffOnboardingService>());
        services.TryAddScoped<global::HRM.Services.FormerStaffService>();
        services.TryAddScoped<global::HRM.Services.Contracts.FormerStaffServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.FormerStaffService>());
        services.TryAddScoped<global::HRM.Services.GroupsManagementService>();
        services.TryAddScoped<global::HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.GroupsManagementService>());
        services.TryAddScoped<global::HRM.Services.IndividualRegistrationService>();
        services.TryAddScoped<global::HRM.Services.Contracts.IndividualRegistrationServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.IndividualRegistrationService>());
        services.TryAddScoped<global::HRM.Services.HrRequestInboxService>();
        services.TryAddScoped<global::HRM.Services.Contracts.HrRequestInboxServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.HrRequestInboxService>());
        services.TryAddScoped<global::HRM.Services.UserService>();
        services.TryAddScoped<global::HRM.Services.Contracts.UserServiceContracts.IOperations>(provider => provider.GetRequiredService<global::HRM.Services.UserService>());
        services.PostConfigure<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 401;
                    else context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 403;
                    else context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });
        services.AddControllersWithViews().AddApplicationPart(typeof(HrmApiController).Assembly).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.IncludeFields = true;
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
        return services;
    }
}
