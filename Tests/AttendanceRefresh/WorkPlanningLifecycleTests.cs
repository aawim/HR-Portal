using HRM.DTOs.UserContext;
using HRM.DTOs.WorkPlanning;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Results;
using HRM.WorkPlanning.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

static class WorkPlanningLifecycleTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var factory = new ReceiptFactory(options);
        var day = DateTime.UtcNow.AddHours(5).Date.AddDays(3);
        var allowed = true;
        var access = Proxy.For<IUserAccessService>((method, args) => method switch
        {
            "RequireContextAsync" => Task.FromResult(new UserContextDto { UserId = 1, IndividualId = 100 }),
            "GetCurrentOrganisationIdAsync" => Task.FromResult(10), "HasOrganisationAccessAsync" => Task.FromResult((int)args[0]! == 10),
            "HasPermissionAsync" => Task.FromResult(allowed), "HasUserGroupAsync" => Task.FromResult(false), _ => throw new Exception(method)
        });
        var audit = Proxy.For<IOperationLogService>((method, args) => method == "CreateAsync" ? Log((HrmTeContext)args[0]!) : throw new Exception(method));
        static async Task<OperationLog> Log(HrmTeContext db)
        { var row = new OperationLog { CreatedByContextId = 1, UpdatedContextId = 1 }; db.OperationLogs.Add(row); await db.SaveChangesAsync(); return row; }
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.AddRange(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant A" }, new Organisation { BusinessEntityID = 20, OrganisationName = "Tenant B" });
            db.Individuals.AddRange(new Individual { BusinessEntityId = 100, FirstNameEnglish = "Current" }, new Individual { BusinessEntityId = 200, FirstNameEnglish = "Other" });
            db.Jobs.AddRange(new Job { JobId = 1, IndividualID = 100, OrganisationID = 10, JobStateId = 4 }, new Job { JobId = 2, IndividualID = 200, OrganisationID = 20, JobStateId = 4 });
            db.WorkTemplateTypes.Add(new WorkTemplateType { WorkTemplateTypeId = 1, Code = "SHIFT", Name = "Shift", IsActive = true });
            db.WorkSegmentTypes.AddRange(new WorkSegmentType { WorkSegmentTypeId = 1, Code = "CHECK_IN", Name = "Check in", IsActive = true }, new WorkSegmentType { WorkSegmentTypeId = 2, Code = "CHECK_OUT", Name = "Check out", IsActive = true });
            db.WorkAssignmentStates.Add(new WorkAssignmentState { WorkAssignmentStateId = 7, Code = "PLANNED", Name = "Planned", IsActive = true });
            foreach (var id in new[] { 1, 2, 3, 4 })
            {
                var start = id == 4 ? 10 : 8;
                db.WorkTemplates.Add(new WorkTemplate { WorkTemplateId = id, OrganisationBusinessEntityId = id == 2 ? 20 : id == 3 ? null : 10,
                    IsGlobal = id == 3, Name = "Template " + id, WorkTemplateTypeId = 1, IsActive = true, DefaultStartTime = new(start, 0), DefaultEndTime = new(id == 4 ? 12 : 14, 0),
                    WorkTemplateSegments = [new() { WorkTemplateSegmentId = id * 10, WorkSegmentTypeId = 1, Name = "In", IsActive = true, SequenceNumber = 1, DurationMinutes = 30, IsMandatory = true, RequiresAttendance = true },
                        new() { WorkTemplateSegmentId = id * 10 + 1, WorkSegmentTypeId = 2, Name = "Out", IsActive = true, SequenceNumber = 2, OffsetMinutes = id == 4 ? 120 : 360, DurationMinutes = 30, IsMandatory = true, RequiresAttendance = true }] });
            }
            db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment { JobID = 1, WorkTemplateID = 1, EffectiveFrom = day.AddDays(-10), ScheduleMode = "Weekly", WorkingDaysMask = 127, IsActive = true });
            db.PlanningProviders.Add(new PlanningProvider { PlanningProviderId = 1, OrganisationBusinessEntityID = 10, Code = "WORK_PLANNING", IsActive = true });
            db.OrganisationWorkPlanningSettings.Add(new OrganisationWorkPlanningSetting { OrganisationBusinessEntityId = 10, PlanningProviderId = 1, IsActive = true });
            await db.SaveChangesAsync();
        }
        var templates = new WorkTemplateService(factory, audit, access);
        Check((await templates.GetAllAsync()).Select(x => x.WorkTemplateId).Order().SequenceEqual([1, 3, 4]), "Template list leaked another tenant");
        Check((await templates.GetAllSegmentsAsync(1)).Count == 2, "Segment list included other templates");
        await Denied(() => templates.GetSegmentsAsync(2));
        await Denied(() => templates.ArchiveAsync(3));
        Check(!(await templates.ArchiveAsync(1)).Success, "Template with live staff assignments was archived");
        var provider = Proxy.For<IPlanningProviderResolver>((_, _) => Task.FromResult(new PlanningProviderResolution { PlanningProviderId = 1, Code = "WORK_PLANNING" }));
        var planner = new WorkPlanGenerator(factory, provider);
        var unused = await planner.GenerateOrGetAsync(100, 1, 10, DateOnly.FromDateTime(day));
        var used = await planner.GenerateOrGetAsync(100, 1, 10, DateOnly.FromDateTime(day.AddDays(1)));
        var finalized = await planner.GenerateOrGetAsync(100, 1, 10, DateOnly.FromDateTime(day.AddDays(2)));
        await using (var db = factory.CreateDbContext())
        {
            db.AttendanceLogs.Add(new AttendanceLog { IndividualId = 100, OrganisationId = 10, Date = day.AddDays(1).AddHours(8) });
            (await db.WorkPlans.SingleAsync(x => x.WorkPlanId == finalized!.WorkPlanId)).IsFinalized = true;
            await db.SaveChangesAsync();
        }
        var edit = new SaveWorkTemplateSegmentDto { WorkTemplateId = 1, WorkTemplateSegmentId = 10, Name = "Updated In", WorkSegmentTypeId = 1,
            SequenceNumber = 1, OffsetMinutes = 60, DurationMinutes = 45, GraceBeforeMinutes = 40, IsMandatory = false, RequiresAttendance = true, IsActive = true };
        Check((await templates.UpdateSegmentAsync(edit)).Success, "Valid segment edit failed");
        var changed = (await templates.GetSegmentsAsync(1)).Single(x => x.WorkTemplateSegmentId == 10);
        Check(!changed.IsMandatory && changed.OffsetMinutes == 60 && changed.GraceBeforeMinutes == 40, "Segment flags/timing did not persist");
        await using (var db = factory.CreateDbContext())
        {
            Check(!await db.WorkPlans.Where(x => x.WorkPlanId == unused!.WorkPlanId).Select(x => x.IsValid).SingleAsync(), "Unused future plan did not refresh after segment edit");
            Check(await db.WorkPlans.Where(x => x.WorkPlanId == used!.WorkPlanId || x.WorkPlanId == finalized!.WorkPlanId).AllAsync(x => x.IsValid), "Used or finalized history was invalidated");
        }
        var rebuilt = await planner.GenerateOrGetAsync(100, 1, 10, DateOnly.FromDateTime(day));
        Check(rebuilt!.WorkPlanId != unused!.WorkPlanId && rebuilt.Segments.Single(x => x.WorkTemplateSegmentId == 10).StartDateTime.Hour == 9, "Future plan used stale segment timing");
        edit.SequenceNumber = 2;
        Check(!(await templates.UpdateSegmentAsync(edit)).Success, "Duplicate segment sequence was accepted");
        edit.WorkTemplateSegmentId = 20;
        await Denied(() => templates.UpdateSegmentAsync(edit));
        await CheckManualEditorAsync(day);
        var generator = new HRM.Services.WorkPlanning.WorkAssignmentGeneratorService(factory, NullLogger<HRM.Services.WorkPlanning.WorkAssignmentGeneratorService>.Instance, access);
        var plans = new WorkPlanService(factory, audit, access, generator, planner);
        var automatic = new GenerateWorkPlanDto { IndividualId = 100, JobId = 1, OrganisationBusinessEntityId = 10, WorkDate = day };
        var repeated = await plans.GenerateWorkPlanAsync(automatic);
        Check(repeated.Success && repeated.WorkPlanId == rebuilt.WorkPlanId, "Plans screen replaced the existing daily plan");
        var manual = new GenerateWorkPlanDto { IndividualId = 100, JobId = 1, OrganisationBusinessEntityId = 10, WorkDate = day, WorkTemplateId = 4,
            WorkPlanGenerationSource = HRM.Enum.WorkPlanGenerationSource.Manual };
        var added = await plans.GenerateWorkPlanAsync(manual);
        Check(added.Success && added.WorkPlanId == rebuilt.WorkPlanId, "Manual extra duty did not join the daily plan: " + string.Join(";", added.Errors));
        Check(!(await plans.GenerateWorkPlanAsync(manual)).Success, "Duplicate manual assignment was accepted");
        var lookup = new ManualWorkAssignmentLookupService(factory, access);
        var preview = await lookup.GetAssignmentPreviewAsync(1, 4, day);
        Check(preview?.AssignmentBaseDateTime.Hour == 10, "Manual preview used hardcoded 08:00");
        await using (var db = factory.CreateDbContext())
        {
            Check(await db.WorkAssignments.CountAsync(x => x.WorkPlanId == rebuilt.WorkPlanId && x.IsValid) == 2, "Manual assignment lost the official assignment");
            var duty = await db.WorkAssignments.SingleAsync(x => x.WorkPlanId == rebuilt.WorkPlanId && x.WorkTemplateId == 4);
            Check(duty.SchedulePurpose == "Duty" && duty.WorkAssignmentStateId == 7 && duty.StartDateTime.Hour == 10, "Manual duty metadata/state/hours incorrect");
        }
        manual.WorkDate = day.AddDays(2);
        Check(!(await plans.GenerateWorkPlanAsync(manual)).Success, "Finalized plan accepted manual additions");
        manual.OrganisationBusinessEntityId = 20;
        Check(!(await plans.GenerateWorkPlanAsync(manual)).Success, "Cross-tenant manual plan was accepted");
        await using (var db = factory.CreateDbContext())
        {
            db.OrganisationStructures.Add(new OrganisationStructure { OrganisationStructureId = 111, OrganisationBusinessEntityID = 10, OrganisationStructureTypeId = 1, Name = "Current department" });
            (await db.Jobs.SingleAsync(x => x.JobId == 1)).OrganisationStructureId = 111;
            db.Individuals.Add(new Individual { BusinessEntityId = 101, FirstNameEnglish = "Second" });
            db.Jobs.Add(new Job { JobId = 3, IndividualID = 101, OrganisationID = 10, OrganisationStructureId = 111, JobStateId = 4 });
            db.JobWorkTemplateAssignments.Add(new(){JobID=3,WorkTemplateID=1,SchedulePurpose="Official",IsActive=true,EffectiveFrom=day.AddYears(-1),WorkingDaysMask=31});
            await db.SaveChangesAsync();
        }
        var manager = new HRM.Services.WorkPlanning.WorkAssignmentManagementService(factory, access, audit,
            NullLogger<HRM.Services.WorkPlanning.WorkAssignmentManagementService>.Instance, generator);
        var bulkDay = day.AddDays(7);
        var bulk = new BulkDepartmentAssignmentRequest { OrganisationStructureId = 111, WorkPlanId = (int)used!.WorkPlanId,
            AssignmentName = "Department duty", ScheduledStart = bulkDay.AddHours(16), ScheduledEnd = bulkDay.AddHours(22), SelectedIndividualIds = [100, 101, 200] };
        var assigned = await manager.AssignDepartmentAsync(bulk);
        Check(assigned.Success && assigned.AssignedCount == 2 && assigned.SkippedCount == 1, "Bulk department assignment failed or included foreign staff: " + assigned.Message);
        await using (var db = factory.CreateDbContext())
        {
            var rows = await db.WorkPlans.Include(x => x.WorkAssignments).Include(x => x.WorkPlanSegments).Where(x => x.WorkDate == bulkDay && x.IsValid).ToListAsync();
            Check(rows.Count == 2 && rows.Select(x => x.IndividualId).Distinct().Count() == 2 && rows.All(x => x.WorkPlanSegments.Count == 2 && x.WorkAssignments.Single().SchedulePurpose == "Duty"),
                "Bulk duty reused one employee's plan or omitted attendance segments");
            Check(rows.All(x => x.WorkAssignments.Single().StartDateTime.Hour == 16 && x.WorkPlanSegments.Min(s => s.StartDateTime).Hour == 17),
                "Bulk duty did not apply the chosen start time and template offsets");
        }
        var afterBulk = await planner.GenerateOrGetAsync(100, 1, 10, DateOnly.FromDateTime(bulkDay));
        Check(afterBulk!.Assignments.Count == 2 && afterBulk.Assignments.Any(x => x.SchedulePurpose == "Official" && x.StartDateTime.Hour == 8),
            "A manual duty using the official template suppressed the official schedule");
        var manualService = new ManualWorkAssignmentService(factory, generator, audit, NullLogger<ManualWorkAssignmentService>.Instance, null!, access);
        long dutyId;
        await using (var db = factory.CreateDbContext()) dutyId = (await db.WorkAssignments.SingleAsync(x => x.WorkPlanId == rebuilt.WorkPlanId && x.WorkTemplateId == 4)).WorkAssignmentId;
        var update = new ManualWorkAssignmentRequest { IndividualId = 100, JobId = 1, OrganisationBusinessEntityId = 10, WorkDate = day, WorkTemplateId = 4,
            AssignmentTitle = "Edited duty", RequiresAttendance = false, RequiresCheckout = false };
        var edited = await manualService.UpdateAsync(dutyId, update);
        Check(edited.Success && edited.WorkAssignmentId == dutyId, "Editing manual assignment created a duplicate instead of updating: " + edited.Message);
        await using (var db = factory.CreateDbContext())
        {
            Check((await db.WorkAssignments.SingleAsync(x => x.WorkAssignmentId == dutyId)).Name == "Edited duty", "Manual assignment title was not saved");
            Check(await db.WorkPlanSegments.Where(x => x.WorkPlanId == rebuilt.WorkPlanId && x.SchedulePurpose == "Duty").AllAsync(x => !x.RequiresAttendance), "Manual rule edits did not update attendance segments");
            Check(await db.WorkPlanSegments.Where(x => x.WorkPlanId == rebuilt.WorkPlanId && x.SchedulePurpose == "Official").AllAsync(x => x.RequiresAttendance), "Manual rule edit changed official clock requirements");
        }
        Check((await manualService.CancelAsync(dutyId, "Duty no longer needed")).Success, "Manual cancellation failed");
        await using (var db = factory.CreateDbContext())
        {
            Check(!await db.WorkAssignments.Where(x => x.WorkAssignmentId == dutyId).Select(x => x.IsValid).SingleAsync(), "Cancelled manual assignment remained active");
            Check(await db.WorkPlanSegments.Where(x => x.WorkPlanId == rebuilt.WorkPlanId && x.SchedulePurpose == "Duty").AllAsync(x => !x.IsValid), "Cancelled duty still required clocking");
            Check(await db.WorkAssignments.AnyAsync(x => x.WorkPlanId == rebuilt.WorkPlanId && x.IsValid && x.SchedulePurpose == "Official"), "Cancelling duty removed official work");
        }
        var withClock = await generator.GenerateAsync(new() { IndividualId = 100, JobId = 1, OrganisationBusinessEntityId = 10, WorkTemplateId = 4,
            WorkDate = day.AddDays(1), GenerationSource = HRM.Enum.WorkPlanGenerationSource.Manual, AssignmentSource = HRM.Enum.WorkAssignmentSource.Manual, GeneratedByUserId = 1, OperationLogId = 1 });
        Check(withClock.Success && !(await manualService.CancelAsync(withClock.WorkAssignmentId!.Value, "Must preserve clocks")).Success,
            "Manual cancellation overwrote a plan with attendance");
        allowed = false;
        Check(!(await plans.GenerateWorkPlanAsync(automatic)).Success, "Plan generation ignored permission removal");
        await Denied(() => templates.UpdateSegmentAsync(edit));
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static async Task CheckManualEditorAsync(DateTime day)
    {
        long capturedId = 0;
        ManualWorkAssignmentRequest? captured = null;
        var service = Proxy.For<IManualWorkAssignmentService>((method, args) => method == "UpdateAsync" ? Save((long)args[0]!, (ManualWorkAssignmentRequest)args[1]!) : throw new Exception("Editing invoked " + method));
        Task<GeneratedWorkPlanResult> Save(long id, ManualWorkAssignmentRequest request)
        { capturedId = id; captured = request; return Task.FromResult(new GeneratedWorkPlanResult { Success = true }); }
        var lookup = Proxy.For<IManualWorkAssignmentLookupService>((method, _) => method == "GetAvailableTemplatesAsync"
            ? Task.FromResult<IReadOnlyList<AssignedWorkTemplateDto>>([new() { WorkTemplateId = 4, TemplateName = "Duty" }]) : throw new Exception(method));
        using var provider = new ServiceCollection().AddLogging().AddSingleton<Microsoft.AspNetCore.Components.IComponentActivator, TestActivator>().BuildServiceProvider();
        await using var renderer = new TestRenderer(provider);
        var form = new HRM.Components.Admin.My.Workspace.Staff.SchduleComponents.Summary.ItemComponents.ManualAssignmentDrawer {
            IsOpen = true, IsEditing = true, StaffId = 100, JobId = 1, OrganisationBusinessEntityId = 10,
            Assignment = new HRM.DTOs.StaffSchedule.ManualWorkAssignmentListDto { WorkAssignmentId = 42, WorkTemplateId = 4, WorkDate = day, AssignmentName = "Existing duty", RequiresAttendance = true } };
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        form.GetType().GetProperty("ManualAssignmentService", flags)!.SetValue(form, service);
        form.GetType().GetProperty("LookupService", flags)!.SetValue(form, lookup);
        await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(form); await renderer.Render(); await renderer.RootClick("Save changes"); });
        renderer.CheckErrors();
        Check(capturedId == 42 && captured?.AssignmentTitle == "Existing duty" && captured.WorkDate == day && captured.WorkTemplateId == 4,
            "Manual Edit form reset the existing assignment or created a new one");
    }
    static async Task Denied<T>(Func<Task<T>> action)
    { try { await action(); } catch (UnauthorizedAccessException) { return; } throw new Exception("Expected access denial"); }
}
