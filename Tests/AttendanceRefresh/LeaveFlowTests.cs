using HRM.Components.Admin.Settings;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Leave;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

static class LeaveFlowTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var factory = new ReceiptFactory(options);
        var org = 10;
        var user = new UserContextDto { IndividualId = 1, UserId = 1, IsHRStaff = true };
        var access = Proxy.For<IUserAccessService>((name, _) => name switch
        {
            "RequireContextAsync" => Task.FromResult(user),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(org),
            "HasOrganisationAccessAsync" => Task.FromResult(true),
            "HasPermissionAsync" => Task.FromResult(false),
            _ => throw new Exception(name)
        });
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.Add(new Organisation { BusinessEntityID = 10, OrganisationName = "Example organisation" });
            db.JobTypes.Add(new JobType { JobTypeId = 1, TypeName = "Permanent", IsActive = true });
            db.LeaveTypes.Add(new LeaveType { LeaveTypeId = 1, Name = "Annual", RequestTypeId = 100, OrganisationId = 10 });
            db.Roles.Add(new Role { RoleID = 20, Name = "HR", RoleKey = "HR" });
            db.Roles.Add(new Role { RoleID = 21, Name = "Supervisor", RoleKey = "Supervisor" });
            db.Roles.Add(new Role { RoleID = 22, Name = "Department head", RoleKey = "DepartmentHead" });
            db.RequestStates.Add(new RequestState { RequestStateId = 9, StateName = "Department verification", IsProcessingState = true });
            db.RequestTypes.AddRange(new RequestType { RequestTypeId = 48, TypeName = "Leave change" }, new RequestType { RequestTypeId = 100, TypeName = "Annual leave" });
            db.RequestTypes.AddRange(new RequestType { RequestTypeId = 105, TypeName = "Early Return Request" }, new RequestType { RequestTypeId = 106, TypeName = "Leave Extension Request" });
            for (var i = 1; i <= 6; i++) db.RequestStates.Add(new RequestState { RequestStateId = i, StateName = $"State {i}", IsProcessingState = i <= 3, IsFinalState = i >= 4 });
            foreach (var type in new[] { 48, 100, 105, 106 })
                foreach (var source in new[] { 1, 2, 3, 9 })
                    foreach (var target in new[] { 1, 2, 3, 4, 5, 6, 9 })
                        if (source != target) db.RequestTypesAllowedRequestStateTransitions.Add(new()
                            { RequestTypeId = type, FromRequestStateId = source, ToRequestStateId = target });
            db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 1, OrganisationID = 10, JobTypeId = 1, OrganisationStructureId = 7 },
                new Job { JobId = 4, IndividualID = 4, OrganisationID = 10, JobTypeId = 1, OrganisationStructureId = 7 },
                new Job { JobId = 2, IndividualID = 2, OrganisationID = 10, JobStateId = 4, JoinedDate = DateTime.Today.AddYears(-1), OrganisationStructureId = 7 });
            db.UserOrganisations.Add(new UserOrganisation { UserOrganisationID = 1, BusinessEntityID = 10 });
            db.UserGroups.AddRange(new UserGroup { UserGroupId = 120, UserOrganisationId = 1, GroupName = "HR" },
                new UserGroup { UserGroupId = 121, UserOrganisationId = 1, GroupName = "Supervisor" },
                new UserGroup { UserGroupId = 122, UserOrganisationId = 1, GroupName = "Department Head" });
            db.UserAssignedUserGroups.AddRange(new UserAssignedUserGroup { UserId = 1, UserGroupId = 120 },
                new UserAssignedUserGroup { UserId = 3, UserGroupId = 120 },
                new UserAssignedUserGroup { UserId = 2, UserGroupId = 121 },
                new UserAssignedUserGroup { UserId = 4, UserGroupId = 121 },
                new UserAssignedUserGroup { UserId = 5, UserGroupId = 121 },
                new UserAssignedUserGroup { UserId = 6, UserGroupId = 122 });
            db.Users.AddRange(new User { UserId = 1, BusinessEntityID = 1, IsActive = true, UserOrganisationId = 1 },
                new User { UserId = 2, BusinessEntityID = 2, IsActive = true, UserOrganisationId = 1 },
                new User { UserId = 3, BusinessEntityID = 3, IsActive = true, UserOrganisationId = 1 },
                new User { UserId = 4, BusinessEntityID = 4, IsActive = true, UserOrganisationId = 1 },
                new User { UserId = 5, BusinessEntityID = 5, IsActive = true, UserOrganisationId = 1 },
                new User { UserId = 6, BusinessEntityID = 6, IsActive = true, UserOrganisationId = 1 });
            db.UserRoles.AddRange(new UserRole { UserId = 1, RoleId = 20, IsActive = true, UserOrganisationId = 1 },
                new UserRole { UserId = 3, RoleId = 20, IsActive = true, UserOrganisationId = 1 });
            db.UserRoles.AddRange(new UserRole { UserId = 2, RoleId = 21, IsActive = true, UserOrganisationId = 1 },
                new UserRole { UserId = 4, RoleId = 21, IsActive = true, UserOrganisationId = 1 },
                new UserRole { UserId = 5, RoleId = 21, IsActive = true, UserOrganisationId = 1 },
                new UserRole { UserId = 6, RoleId = 22, IsActive = true, UserOrganisationId = 1 });
            db.Jobs.Add(new Job { JobId = 5, IndividualID = 5, OrganisationID = 10, OrganisationStructureId = 8, JobStateId = 4, JoinedDate = DateTime.Today.AddYears(-1) });
            db.OrganisationStructureSupervisors.Add(new OrganisationStructureSupervisor
                { OrganisationStructureSupervisorId = 2, OrganisationStructureId = 8, SupervisorStaffId = 5, IsActive = true, IsValid = true });
            db.OrganisationStructureSupervisors.Add(new OrganisationStructureSupervisor
                { OrganisationStructureSupervisorId = 1, OrganisationStructureId = 7, SupervisorStaffId = 2, IsActive = true, IsValid = true });
            db.Leaves.Add(new Leaf { LeaveId = 1, JobId = 1, LeaveTypeId = 1, LeaveStateId = 4 });
            db.Leaves.Add(new Leaf { LeaveId = 4, JobId = 4, LeaveTypeId = 1, LeaveStateId = 2 });
            db.Requests.AddRange(new Request { RequestId = 1, RequestTypeId = 100, ApplicantBusinessEntityId = 1, OrganisationBusinessEntityID = 10 },
                new Request { RequestId = 2, RequestTypeId = 48, ApplicantBusinessEntityId = 1, OrganisationBusinessEntityID = 10 },
                new Request { RequestId = 3, RequestTypeId = 48, ApplicantBusinessEntityId = 1, OrganisationBusinessEntityID = 10 },
                new Request { RequestId = 4, RequestTypeId = 100, ApplicantBusinessEntityId = 4, OrganisationBusinessEntityID = 10 });
            db.Requests.AddRange(new Request { RequestId = 5, RequestTypeId = 105, ApplicantBusinessEntityId = 1, OrganisationBusinessEntityID = 10 },
                new Request { RequestId = 6, RequestTypeId = 106, ApplicantBusinessEntityId = 1, OrganisationBusinessEntityID = 10 },
                new Request { RequestId = 7, RequestTypeId = 105, ApplicantBusinessEntityId = 1, OrganisationBusinessEntityID = 10 });
            db.LeaveRequests.Add(new LeaveRequest { RequestId = 1, LeaveId = 1 });
            db.LeaveRequests.Add(new LeaveRequest { RequestId = 4, LeaveId = 4 });
            db.LeaveChangeRequests.AddRange(new LeaveChangeRequest { LeaveChangeRequestId = 1, RequestId = 2, LeaveId = 1, LeaveChangeRequestTypeId = 1 },
                new LeaveChangeRequest { LeaveChangeRequestId = 2, RequestId = 3, LeaveId = 1, LeaveChangeRequestTypeId = 2 });
            db.LeaveChangeRequests.AddRange(new LeaveChangeRequest { LeaveChangeRequestId = 3, RequestId = 5, LeaveId = 1, LeaveChangeRequestTypeId = 2 },
                new LeaveChangeRequest { LeaveChangeRequestId = 4, RequestId = 6, LeaveId = 1, LeaveChangeRequestTypeId = 1 },
                new LeaveChangeRequest { LeaveChangeRequestId = 5, RequestId = 7, LeaveId = 1, LeaveChangeRequestTypeId = 1 });
            await db.SaveChangesAsync();
        }
        var contextResolver = new WorkflowRequestContextResolver(factory);
        Check((await contextResolver.ResolveAsync(1, 1)).LeaveRequestKind == 0 &&
            (await contextResolver.ResolveAsync(2, 1)).LeaveRequestKind == 1 &&
            (await contextResolver.ResolveAsync(3, 1)).LeaveRequestKind == 2,
            "Submissions are not classified by new leave, extension and early return");
        var config = new LeaveFlowConfigurationService(factory, access);
        var superPermission = true;
        var organisationAllowed = true;
        var superAccess = Proxy.For<IUserAccessService>((name, args) => name switch
        {
            "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = 425 }),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult(organisationAllowed),
            "HasPermissionAsync" => Task.FromResult(superPermission && (string)args[0]! == "Super.Admin.Manage"),
            _ => throw new Exception(name)
        });
        var superConfig = new LeaveFlowConfigurationService(factory, superAccess);
        Check((await superConfig.LoadAsync()).Organisation == "Example organisation", "Super Admin permission without legacy flag was denied");
        await superConfig.GetAsync(1, 105);
        organisationAllowed = false;
        await ThrowsAsync(() => superConfig.LoadAsync(), "Super Admin permission bypassed organisation scope");
        organisationAllowed = true;
        superPermission = false;
        await ThrowsAsync(() => superConfig.LoadAsync(), "Missing configuration permission was accepted");
        await ThrowsAsync(() => superConfig.SaveAsync(new LeaveFlowConfigurationDto()), "Save bypassed configuration permission");
        Check((await contextResolver.ResolveAsync(5, 1)).RequestTypeId == 105 && (await contextResolver.ResolveAsync(6, 1)).RequestTypeId == 106,
            "Independent leave change Request Types are not resolved");
        await ThrowsAsync(() => contextResolver.ResolveAsync(7, 1), "Early Return Request accepted an extension payload");
        var settings = await config.LoadAsync();
        Check(settings.RequestTypes.Any(x => x.Id == 105) && settings.RequestTypes.Any(x => x.Id == 106) && !settings.RequestTypes.Any(x => x.Id == 48),
            "New Request Types are missing or the legacy combined type is still selectable");
        var early = new LeaveFlowConfigurationDto { JobTypeId = 1, RequestTypeId = 105, HrUserGroupId = 120, SupervisorFirst = false };
        Check((await config.SaveAsync(early)).Success, "HR-only early-return configuration failed");
        await using (var db = factory.CreateDbContext())
        {
            db.RequestTypesAllowedRequestStateTransitions.Remove(await db.RequestTypesAllowedRequestStateTransitions.SingleAsync(x => x.RequestTypeId == 105 && x.FromRequestStateId == 3 && x.ToRequestStateId == 4));
            await db.SaveChangesAsync();
        }
        var blockedFlow = await config.SaveAsync(early);
        Check(!blockedFlow.Success && blockedFlow.Message.Contains("Transition not allowed"), "Workflow save restored a removed approval transition");
        await using (var db = factory.CreateDbContext())
        {
            Check(!await db.RequestTypesAllowedRequestStateTransitions.AnyAsync(x => x.RequestTypeId == 105 && x.FromRequestStateId == 3 && x.ToRequestStateId == 4),
                "Workflow configuration silently enabled approval");
            db.RequestTypesAllowedRequestStateTransitions.Add(new() { RequestTypeId = 105, FromRequestStateId = 3, ToRequestStateId = 4 });
            await db.SaveChangesAsync();
        }
        var extension = new LeaveFlowConfigurationDto { JobTypeId = 1, RequestTypeId = 106, HrUserGroupId = 120, SupervisorFirst = true, SupervisorUserGroupId = 121 };
        Check((await config.SaveAsync(extension)).Success, "Supervisor/HR extension configuration failed");
        var application = new LeaveFlowConfigurationDto { JobTypeId = 1, RequestTypeId = 100, HrUserGroupId = 120, SupervisorFirst = false };
        Check((await config.SaveAsync(application)).Success, "New leave configuration failed");
        await using (var db = factory.CreateDbContext())
        {
            var staffOne = await contextResolver.ResolveAsync(1, 1);
            var staffFour = await contextResolver.ResolveAsync(4, 4);
            Check((await LeaveFlowConfigurationService.ResolveAsync(db, staffOne)).WorkflowDefinitionId ==
                (await LeaveFlowConfigurationService.ResolveAsync(db, staffFour)).WorkflowDefinitionId,
                "Staff sharing a Job Type did not inherit the same leave flow");
        }
        var menuMethod = typeof(SettingsCategoryPage).GetMethod("GetSubItems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var menuItems = (System.Collections.IEnumerable)menuMethod.Invoke(new SettingsCategoryPage(), ["leaves"])!;
        Check(menuItems.Cast<object>().Any(x => (Type?)x.GetType().GetProperty("ComponentType")!.GetValue(x) == typeof(LeaveFlowSettings)),
            "Leave approval configuration is missing from Leaves Management");
        Check(!(await config.SaveAsync(new() { JobTypeId = 1, RequestTypeId = 106, HrUserGroupId = 120, SupervisorFirst = false })).Success,
            "Extension bypassed supervisor stage");
        Check(!(await config.SaveAsync(new() { JobTypeId = 1, RequestTypeId = 105, HrUserGroupId = 120, SupervisorFirst = true })).Success,
            "Early return accepted a supervisor stage");
        var context = new WorkflowRequestContextDto { RequestTypeId = 105, OrganisationBusinessEntityId = 10, JobTypeId = 1,
            SubjectJobId = 1, ApplicantBusinessEntityId = 1, LeaveTypeId = 1, LeaveRequestKind = 2 };
        var resolver = new WorkflowApproverResolver(factory);
        await using (var db = factory.CreateDbContext())
        {
            var selected = await LeaveFlowConfigurationService.ResolveAsync(db, context);
            Check(selected.Stages.Count == 1 && selected.Stages.Single().RequestStateId == 3, "Early return is not HR only");
            var candidates = await resolver.ResolveAsync(selected.Stages.Single().WorkflowStageId, context);
            Check(candidates.Select(x => x.UserId).SequenceEqual(new[] { 3 }), "HR role resolution includes the applicant or misses HR");
            context.LeaveRequestKind = 1;
            context.RequestTypeId = 106;
            selected = await LeaveFlowConfigurationService.ResolveAsync(db, context);
            Check(selected.Stages.OrderBy(x => x.StageOrder).Select(x => x.RequestStateId).SequenceEqual(new[] { 2, 3 }), "Extension stage sequence incorrect");
            candidates = await resolver.ResolveAsync(selected.Stages.Single(x => x.StageOrder == 1).WorkflowStageId, context);
            Check(candidates.Single().UserId == 2, "Employee's actual supervisor was not resolved");
            var oldId = selected.WorkflowDefinitionId;
            Check((await config.SaveAsync(extension)).Success, "Flow version update failed");
            var newDefinition = await LeaveFlowConfigurationService.ResolveAsync(db, context);
            Check(newDefinition.WorkflowDefinitionId != oldId && await db.WfWorkflowStages.AnyAsync(x => x.WorkflowDefinitionId == oldId),
                "Saving config modified or deleted existing workflow stages");
            var stale = new LeaveFlowConfigurationDto { JobTypeId = 1, RequestTypeId = 106, HrUserGroupId = 120, SupervisorFirst = true, SupervisorUserGroupId = 121 };
            Check(!(await config.SaveAsync(stale)).Success, "Stale settings overwrote a newer flow");
            context.JobTypeId = 2;
            await ThrowsAsync(() => LeaveFlowConfigurationService.ResolveAsync(db, context), "Missing Job Type configuration fell back to another flow");
            context.JobTypeId = 1; context.LeaveTypeId = 2;
            Check((await LeaveFlowConfigurationService.ResolveAsync(db, context)).WorkflowDefinitionId == newDefinition.WorkflowDefinitionId,
                "Leave Types sharing a Request Type did not share its flow");
            context.RequestTypeId = 999;
            await ThrowsAsync(() => LeaveFlowConfigurationService.ResolveAsync(db, context), "Missing Request Type configuration fell back");
            context.RequestTypeId = 106;
            context.LeaveTypeId = 1; context.OrganisationBusinessEntityId = 99;
            await ThrowsAsync(() => LeaveFlowConfigurationService.ResolveAsync(db, context), "Organisation scope leaked");
            context.OrganisationBusinessEntityId = 10;
            var supervisorGroup = await db.UserAssignedUserGroups.SingleAsync(x => x.UserId == 2 && x.UserGroupId == 121);
            db.UserAssignedUserGroups.Remove(supervisorGroup);
            await db.SaveChangesAsync();
            await ThrowsAsync(() => resolver.ResolveAsync(newDefinition.Stages.Single(x => x.StageOrder == 1).WorkflowStageId, context), "Supervisor without the configured role was allowed to verify");
            db.UserAssignedUserGroups.Add(supervisorGroup);
            var supervisor = await db.OrganisationStructureSupervisors.SingleAsync(x => x.SupervisorStaffId == 2); supervisor.IsActive = false;
            await db.SaveChangesAsync();
            await ThrowsAsync(() => resolver.ResolveAsync(newDefinition.Stages.Single(x => x.StageOrder == 1).WorkflowStageId, context), "Missing supervisor did not block approval routing");
            var hr = await db.Users.SingleAsync(x => x.UserId == 3); hr.IsActive = false;
            await db.SaveChangesAsync();
            await ThrowsAsync(() => resolver.ResolveAsync(newDefinition.Stages.Single(x => x.StageOrder == 2).WorkflowStageId, context), "Self-only HR role did not block routing");
        }
        await TestOrderedStagesAsync(factory, config, application, access, contextResolver, user);
        await using (var db = factory.CreateDbContext())
        {
            db.RequestTypes.Add(new RequestType { RequestTypeId = 102, TypeName = "Other request" });
            db.RequestTypesAllowedRequestStateTransitions.AddRange(new RequestTypesAllowedRequestStateTransition { RequestTypeId = 102, FromRequestStateId = 1, ToRequestStateId = 3 },
                new RequestTypesAllowedRequestStateTransition { RequestTypeId = 102, FromRequestStateId = 2, ToRequestStateId = 3 },
                new RequestTypesAllowedRequestStateTransition { RequestTypeId = 102, FromRequestStateId = 3, ToRequestStateId = 4 });
            db.WfWorkflowDefinitions.Add(new WfWorkflowDefinition
            {
                RequestTypeId = 102, LeaveTypeId = 2, LeaveRequestKind = 0, OrganisationBusinessEntityId = 10,
                Name = "Legacy leave flow", VersionNumber = 1,
                WorkflowJobTypes = [new() { JobTypeId = 1 }],
                Stages = [new() { StageName = "HR", StageOrder = 1, RequestStateId = 3,
                    Approvers = [new() { ApproverType = "UserGroup", ApproverReferenceId = 120 }] }]
            });
            await db.SaveChangesAsync();
        }
        var imported = await config.GetAsync(1, 102);
        Check(imported.ImportedFromLegacy && imported.Version == 0 && imported.Stages!.Single().UserGroupId == 120,
            "Existing leave-specific configuration was not offered as a Request Type draft");
        Check((await config.SaveAsync(imported)).Success && !imported.ImportedFromLegacy, "Imported Request Type flow could not be saved");
        await using (var db = factory.CreateDbContext())
        {
            var genericContext = new WorkflowRequestContextDto { OrganisationBusinessEntityId = 10, JobTypeId = 1, RequestTypeId = 102 };
            Check((await LeaveFlowConfigurationService.ResolveAsync(db, genericContext)).LeaveTypeId == null,
                "Request Type flow still depends on a Leave Type");
        }
        user.ActiveJob = new ActiveJobDto { JobId = 1 };
        var denyOrganisation = false;
        var fallbackAccess = Proxy.For<IUserAccessService>((name, _) => name switch
        {
            "RequireContextAsync" => Task.FromResult(user),
            "GetCurrentOrganisationIdAsync" => throw new InvalidOperationException("Current user has no organisation."),
            "HasOrganisationAccessAsync" => Task.FromResult(!denyOrganisation),
            "HasPermissionAsync" => Task.FromResult(false),
            _ => throw new Exception(name)
        });
        var fallbackConfig = new LeaveFlowConfigurationService(factory, fallbackAccess);
        Check((await fallbackConfig.LoadAsync()).Organisation == "Example organisation", "Opening configuration failed without an explicit organisation context");
        Check((await fallbackConfig.GetAsync(1, 105)).Version > 0, "Active-job fallback did not load the saved configuration");
        denyOrganisation = true;
        await ThrowsAsync(() => fallbackConfig.LoadAsync(), "Active-job fallback bypassed organisation access checks");
        denyOrganisation = false;
        user.ActiveJob = new ActiveJobDto { JobId = 4 };
        await ThrowsAsync(() => fallbackConfig.LoadAsync(), "Fallback accepted another employee's job");
        user.ActiveJob = new ActiveJobDto { JobId = 1 };
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using (var renderer = new TestRenderer(services))
        {
            var page = new LeaveFlowSettings();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            typeof(LeaveFlowSettings).GetProperty("Configuration", flags)!.SetValue(page, fallbackConfig);
            typeof(LeaveFlowSettings).GetProperty("Logger", flags)!.SetValue(page, Microsoft.Extensions.Logging.Abstractions.NullLogger<LeaveFlowSettings>.Instance);
            await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(page); await renderer.Render(); });
            Check(renderer.RootText().Contains("Select both a Job Type and a Request Type"), "Stage controls are hidden without an explanation");
            await renderer.Dispatcher.InvokeAsync(async () => {
                await renderer.RootChange("flow-job-type", "1"); await renderer.RootChange("flow-request-type", "105"); });
            Check(!renderer.RootText().Contains("Leave change kind"), "Configuration still requires a special leave-change kind");
            renderer.CheckErrors();
            Check(renderer.RootText().Contains("HR approval") && renderer.RootText().Contains("Eligible reviewers"), "Settings page did not load the chosen leave flow");
            Check(renderer.RootButtonCount("+ Add approval stage") == 1, "Add stage button is not visible");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootClick("+ Add approval stage"));
            var editingModel = (LeaveFlowConfigurationDto)typeof(LeaveFlowSettings).GetField("model", flags)!.GetValue(page)!;
            Check(editingModel.Stages!.Count == 2, "Add stage button did not increase the level");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootChange("flow-stage-group-1", "121"));
            Check(editingModel.Stages[1].UserGroupId == 121, "New stage role selection did not bind");
            await renderer.Dispatcher.InvokeAsync(() => renderer.RootChange("flow-level", "0"));
            Check(renderer.RootText().Contains("reserved for future AI"), "Level 0 is not clearly reserved for future automation");
        }
        user.IsHRStaff = false;
        await ThrowsAsync(() => config.LoadAsync(), "Unauthorised staff can manage leave flows");
    }
    static async Task TestOrderedStagesAsync(ReceiptFactory factory, LeaveFlowConfigurationService config,
        LeaveFlowConfigurationDto model, IUserAccessService access, WorkflowRequestContextResolver contexts, UserContextDto user)
    {
        model.Stages = [new() { Name = "Supervisor", UserGroupId = 121, StateId = 2, ApplicantSupervisorOnly = true },
            new() { Name = "Department head", UserGroupId = 122, StateId = 9 }, new() { Name = "HR", UserGroupId = 120, StateId = 3 }];
        Check((await config.SaveAsync(model)).Success, "Level 3 configuration failed");
        var loaded = await config.GetAsync(1, 100);
        Check(loaded.Stages!.Select(x => x.Name).SequenceEqual(new[] { "Supervisor", "Department head", "HR" }), "Stage ordering was not preserved");
        loaded.Stages = [];
        Check(!(await config.SaveAsync(loaded)).Success, "Reserved Level 0 enabled automatic approval");
        await using var db = factory.CreateDbContext();
        (await db.Users.SingleAsync(x => x.UserId == 3)).IsActive = true;
        (await db.OrganisationStructureSupervisors.SingleAsync(x => x.SupervisorStaffId == 2)).IsActive = true;
        var definition = await db.WfWorkflowDefinitions.Include(x => x.Stages).SingleAsync(x => x.RequestTypeId == 100 && x.LeaveRequestKind == 0 && x.IsActive);
        var stageList = definition.Stages.OrderBy(x => x.StageOrder).ToList();
        var instance = new WfRequestInstance { WorkflowRequestInstanceId = 50, WorkflowDefinitionId = definition.WorkflowDefinitionId, RequestId = 1, CurrentRequestStageInstanceId = 50 };
        for (var i = 0; i < 3; i++) db.WfRequestStageInstances.Add(new WfRequestStageInstance
        {
            RequestStageInstanceId = 50 + i, WorkflowRequestInstanceId = 50, WorkflowStageId = stageList[i].WorkflowStageId,
            Approvals = [new() { RequestApprovalId = 50 + i, ApproverUserId = new[] { 2, 6, 3 }[i] }]
        });
        await db.SaveChangesAsync();
        var service = new LeaveChangeService(factory, access, null!, null!, null!, new WorkflowApproverResolver(factory), contexts);
        var method = typeof(LeaveChangeService).GetMethod("AllowedActionsAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var request = new Request { RequestId = 1, RequestTypeId = 100, RequestStateId = 2, OrganisationBusinessEntityID = 10 };
        var leave = new Leaf { LeaveId = 1, JobId = 1, LeaveStateId = 2, Job = await db.Jobs.SingleAsync(x => x.JobId == 1) };
        async Task<List<RequestActionDto>> Actions() => await (Task<List<RequestActionDto>>)method.Invoke(service, [db, request, leave, instance, user])!;
        user.UserId = 2; user.IndividualId = 2;
        var actions = await Actions();
        Check(actions.Any(x => x.ToStateId == 9) && !actions.Any(x => x.ToStateId is 3 or 4), "Supervisor can skip the middle stage");
        user.UserId = 3; user.IndividualId = 3;
        Check((await Actions()).Count == 0, "HR can approve before its stage is active");
        request.RequestStateId = 9; instance.CurrentRequestStageInstanceId = 51;
        user.UserId = 6; user.IndividualId = 6;
        Check((await Actions()).Any(x => x.ToStateId == 3) && !(await Actions()).Any(x => x.ToStateId == 4), "Middle stage bypassed HR");
        request.RequestStateId = 3; instance.CurrentRequestStageInstanceId = 52;
        user.UserId = 3; user.IndividualId = 3;
        Check((await Actions()).Any(x => x.ToStateId == 4), "Last stage cannot approve");
        db.UserAssignedUserGroups.Remove(await db.UserAssignedUserGroups.SingleAsync(x => x.UserId == 3 && x.UserGroupId == 120));
        await db.SaveChangesAsync();
        Check((await Actions()).Count == 0, "Assigned reviewer kept permission after losing the role");
        db.RequestStates.Add(new RequestState { RequestStateId = 40, StateName = "Custom review", IsProcessingState = true });
        await db.SaveChangesAsync();
        var applyMethod = typeof(LeaveChangeService).GetMethod("ApplyApplicationStateAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await (Task)applyMethod.Invoke(service, [db, leave, 40])!;
        Check(leave.LeaveStateId == 3, "Custom processing stage cannot map to the leave's pending state");
        user.UserId = 1; user.IndividualId = 1;
    }
    static async Task ThrowsAsync(Func<Task> action, string message)
    {
        try { await action(); } catch (InvalidOperationException) { return; } catch (UnauthorizedAccessException) { return; }
        throw new Exception(message);
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
