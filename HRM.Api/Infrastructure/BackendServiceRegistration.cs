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




namespace HRM.Api.Infrastructure;
public static class BackendServiceRegistration
{
    public static IServiceCollection AddHrmBackendServices(this IServiceCollection services, bool includeBackgroundWorkers = true)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, HRM.Api.Authentication.ApiAuthenticationStateProvider>();
        services.AddScoped<Microsoft.AspNetCore.Components.NavigationManager, HRM.Api.Authentication.ApiNavigationManager>();
        services.AddScoped<WorkingHoursSettingsService>();
        services.AddScoped<JobTypeOvertimeSettingsService>();
        services.AddScoped<ScopeScheduleSettingsService>();
        services.AddScoped<GroupsManagementService>();
        services.AddScoped<LeaveConfigurationSettingsService>();
        services.AddScoped<IndividualRegistrationService>();
        services.AddScoped<OvertimeRequestService>();
        services.AddScoped<FormerStaffService>();
        services.AddScoped<WeeklyWorkingHoursService>();
        services.AddScoped<WeeklyHourExceptionService>();

        services.AddScoped<ISequenceNumberService, SequenceNumberService>();
        services.AddScoped<IRequestService, RequestService>();

        services.AddScoped<
            IAttendanceSegmentEvaluator,
            AttendanceSegmentEvaluator>();

        services.AddScoped<
            IAttendanceWorkPlanResolver,
            AttendanceWorkPlanResolver>();

        services.AddScoped<IWorkPlanGenerator, WorkPlanGenerator>();
        services.AddScoped<HRM.WorkPlanning.Services.DutyRosterService>();

        services.AddScoped<IJobWorkTemplateService, JobWorkTemplateService>();

        services.AddScoped<
            ILeaveTypeCatalogueService,
            LeaveTypeCatalogueService>();

        services.AddScoped<
            ILeavePolicyService,
            LeavePolicyService>();

        services.AddScoped<
            IAttendanceResolver,
            AttendanceResolver>();

        services.AddScoped<
            IAttendanceEvaluationService,
            AttendanceEvaluationService>();

        services.AddScoped<
            IWorkflowDefinitionService,
            WorkflowDefinitionService>();

        services.AddScoped<
            IWorkflowApproverResolver,
            WorkflowApproverResolver>();

        services.AddScoped<
            IWorkflowRequestContextResolver,
            WorkflowRequestContextResolver>();

        services.AddScoped<
            IWorkflowEngine,
            WorkflowEngine>();

        services.AddScoped<
            IWorkflowActorProvider,
            WorkflowActorProvider>();

        services.AddScoped<UserContext, UserContext>();

        services.AddScoped<IJobPosition, JobPositionService>();

        services.AddScoped<IProfileService, ProfileService>();

        services.AddScoped<IJobWorkTemplateAssignmentService,JobWorkTemplateAssignmentService>();

        services.AddScoped<IStaffScheduleService, StaffScheduleService>();

        services.AddScoped<IManualWorkAssignmentService,ManualWorkAssignmentService>();

        services.AddScoped<IManualWorkAssignmentLookupService,ManualWorkAssignmentLookupService>();

        services.AddScoped<IWorkPlanService, WorkPlanService>();

        services.AddScoped<IWorkTemplateService, WorkTemplateService>();

        services.AddScoped<IWorkTypeService, WorkTypeService>();

        services.AddScoped<IWorkAssignmentManagementService,WorkAssignmentManagementService>();

        services.AddScoped<IWorkAssignmentOwnershipService,WorkAssignmentOwnershipService>();

        services.AddScoped<IAttendanceDuplicateValidator,AttendanceDuplicateValidator>();

        services.AddScoped<IAttendanceLogProcessor,AttendanceLogProcessor>();

        services.AddScoped<IAttendancePlanService,AttendancePlanService>();

        services.AddScoped<IAttendanceLogResolutionRepository, AttendanceLogResolutionRepository>();

        services.AddScoped<IWorkAssignmentGenerator, WorkAssignmentGeneratorService>();

        services.AddScoped<IWorkAssignmentResolver,WorkAssignmentResolver>();

        services.AddScoped<IWorkPlanningService,WorkPlanningService>();

        services.AddScoped<IWorkPlanBuilder,WorkPlanBuilder>();

        services.AddScoped<IPlanningProviderResolver, PlanningProviderResolver>();

        services.AddScoped<IWorkPlanValidator, WorkPlanValidator>();

        services.AddScoped<HRM.Services.Leave.LeaveHandoverRuleSettingsService>();
        services.AddScoped<IJobLeaveTypeService, JobLeaveTypeService>();
        services.AddScoped<IEffectivePermissionService,EffectivePermissionService>();
        services.AddScoped<IUserContextBuilder, UserContextBuilder>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<IIndividualRegistryService, IndividualRegistryService>();
        services.AddScoped<IWorkspaceDirectoryService, WorkspaceDirectoryService>();
        services.AddScoped<IMyAttendanceSupportService, MyAttendanceSupportService>();
        services.AddScoped<IAttendanceRequestResourceBuilder,AttendanceRequestResourceBuilder>();
        services.AddScoped<ILeaveResourceBuilder,LeaveResourceBuilder>();
        services.AddScoped<IIndividualResourceBuilder,IndividualResourceBuilder>();
        services.AddScoped<IBusinessAccessService,BusinessAccessService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<UserService>();
        services.AddScoped<IUserService>(sp => sp.GetRequiredService<UserService>());
        services.AddScoped<UserPreferenceService>();
        services.AddScoped<StaffChatService>();
        services.AddScoped<WorkspaceRecentSearchService>();
        services.AddScoped<RequestNotificationService>();
        services.AddScoped<HRM.Services.Leave.LeaveAccrualSettingsService>();
        services.AddScoped<HRM.Services.Leave.LeaveCarryForwardService>();
        if (includeBackgroundWorkers) services.AddHostedService<HRM.Services.Leave.LeaveAccrualWorker>();
        services.AddMemoryCache();
        services.AddScoped<IUserContextCache, UserContextCache>();

        services.AddScoped<IJobService, JobService>();
        services.AddScoped<ToastService>();

        services.AddScoped<AttendanceLogService>();

        services.AddScoped<IOrganisationService, OrganisationService>();
        services.AddScoped<IOperationLogService, OperationLogService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<ILeaveReceiptService, LeaveReceiptService>();
        services.AddScoped<LeaveFlowConfigurationService>();
        services.AddScoped<RequestTransitionSettingsService>();
        services.AddScoped<HRM.Services.Leave.LeaveHandoverService>();
        services.AddScoped<HRM.Services.Leave.LeaveCalendarService>();
        services.AddScoped<HRM.Services.OrganisationWorkspaceService>();
        services.AddScoped<IUserTodoService, UserTodoService>();
        services.AddScoped<ILeaveChangeService, HRM.Services.Leave.LeaveChangeService>();
        services.AddScoped<HRM.Services.JobTypeConfigurationService>();
        services.AddScoped<HRM.Services.StaffOnboardingService>();
        services.AddScoped<HRM.Services.PositionOverviewService>();
        services.AddScoped<HRM.Services.StaffPositionManagementService>();
        services.AddScoped<HRM.Services.PermissionSettingsService>();
        services.AddScoped<HRM.Services.UserSettingsService>();
        services.AddScoped<HRM.Services.AdminAccessGrantService>();
        services.AddScoped<HRM.Services.LateCheckInRequestService>();
        services.AddScoped<HRM.Services.OrganisationPermissionService>();
        services.AddScoped<HRM.Services.OrganisationResourceSettingsService>();
        services.AddScoped<HRM.Services.OrganisationSettingsService>();
        services.AddScoped<HRM.Services.SupervisorChangeRequestService>();
        services.AddScoped<HRM.Services.HrRequestInboxService>();
        services.AddScoped<HRM.Services.LeaveSetConfigurationService>();

        services.AddUserAppData();
        services.AddScoped<HRM.Application.Contracts.Pages.ILeaveApplications, HRM.Services.Transport.LeaveApplicationsService>();
        services.AddScoped<PortalContext>();
        services.AddScoped<UserRepository>();

        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceRequestService, AttendanceRequestService>();
        services.AddScoped<IJobDataLoader, JobDataLoader>();

        services.AddScoped<IAttendanceLogDataLoader, AttendanceLogDataLoader>();
        services.AddScoped<IGeographyService, GeographyService>();
        services.AddScoped<IIndividual, IndividualService>();
        services.AddScoped<IAddress, AddressService>();
        services.AddScoped<ITeam, TeamService>();

        services.AddHrmApiEndpoints();
        services.AddAuthorization();

        return services;
    }
}
