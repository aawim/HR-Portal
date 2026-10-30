using HRM.DTOs.Job.JobWorkTemplateAssignment;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using HRM.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Results;
using HRM.WorkPlanning.Services.JobWorkTemplateAssignmentService;
using HRM.WorkPlanning.Services.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

static class ScheduleFlowTests
{
    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var factory = new ReceiptFactory(options);
        var today = DateTime.UtcNow.AddHours(5).Date;
        var within = HRM.Helpers.WorkHourCoverage.Calculate([(today.AddHours(8), today.AddHours(14))], [(today.AddHours(9), today.AddHours(11))]);
        Check(within.OfficialMinutes == 360 && within.DutyWithinOfficialMinutes == 120 && within.PotentialOvertimeMinutes == 0,
            "Duty inside office hours was counted as additional hours or overtime");
        var outside = HRM.Helpers.WorkHourCoverage.Calculate([(today.AddHours(8), today.AddHours(14))], [(today.AddHours(13), today.AddHours(16))]);
        Check(outside.DutyWithinOfficialMinutes == 60 && outside.PotentialOvertimeMinutes == 120, "Duty outside office hours was not split at the official boundary");
        Check(HRM.Helpers.WorkHourCoverage.Minutes([(today.AddHours(8), today.AddHours(14)), (today.AddHours(9), today.AddHours(11))]) == 360,
            "Overlapping worked hours were counted twice");
        var overnight = HRM.Helpers.WorkHourCoverage.Calculate([(today.AddHours(22), today.AddDays(1).AddHours(6))], [(today.AddDays(1).AddHours(5), today.AddDays(1).AddHours(8))]);
        Check(overnight.DutyWithinOfficialMinutes == 60 && overnight.PotentialOvertimeMinutes == 120, "Overnight duty coverage incorrect");
        var actor = new UserContextDto { UserId = 1, IndividualId = 1000 };
        var attachAllowed = true;
        var access = Proxy.For<IUserAccessService>((method, values) => method switch {
            "RequireContextAsync" => Task.FromResult(actor), "GetCurrentOrganisationIdAsync" => Task.FromResult(10),
            "HasOrganisationAccessAsync" => Task.FromResult((int)values[0]! == 10),
            "HasPermissionAsync" => Task.FromResult(attachAllowed), _ => throw new Exception(method) });
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual { BusinessEntityId = 1000, FirstNameEnglish = "Schedule", LastNameEnglish = "Staff" });
            db.Organisations.Add(new Organisation { BusinessEntityID = 10, OrganisationName = "Tenant A" });
            db.Jobs.Add(new Job { JobId = 100, IndividualID = 1000, OrganisationID = 10, JobStateId = 4, JoinedDate = today.AddYears(-1) });
            db.WorkTemplateTypes.Add(new WorkTemplateType { WorkTemplateTypeId = 1, Name = "Shift", Code = "SHIFT", IsActive = true });
            db.WorkSegmentTypes.Add(new WorkSegmentType { WorkSegmentTypeId = 1, Name = "Work", Code = "WORK", IsActive = true });
            db.WorkAssignmentStates.Add(new WorkAssignmentState { WorkAssignmentStateId = 1, Name = "Planned", Code = "PLANNED", IsActive = true });
            foreach (var id in new[] { 1, 2, 3 })
                db.WorkTemplates.Add(new WorkTemplate { WorkTemplateId = id, OrganisationBusinessEntityId = 10,
                    WorkTemplateTypeId = 1, Name = id == 1 ? "Office" : "Duty " + id, IsActive = true,
                    DefaultStartTime = new TimeOnly(id == 1 ? 8 : 9, 0), DefaultEndTime = new TimeOnly(id == 1 ? 14 : 11, 0),
                    WorkTemplateSegments = [new WorkTemplateSegment { WorkTemplateSegmentId = id, Name = "Work", WorkSegmentTypeId = 1,
                        IsActive = true, DurationMinutes = id == 1 ? 360 : 120, RequiresAttendance = true, SequenceNumber = 1 }] });
            await db.SaveChangesAsync();
        }
        var logs = Proxy.For<IOperationLogService>((method, values) => method == "CreateAsync" ? AuditAsync((HrmTeContext)values[0]!) : throw new Exception(method));
        async Task<OperationLog> AuditAsync(HrmTeContext db)
        {
            var log = new OperationLog { CreatedByContextId = 1, UpdatedContextId = 1 };
            db.OperationLogs.Add(log); await db.SaveChangesAsync(); return log;
        }
        var service = new JobWorkTemplateAssignmentService(factory, NullLogger<JobWorkTemplateAssignmentService>.Instance, access, logs);
        AssignJobWorkTemplateDto Request(int template, string purpose) => new() { JobId = 100, IndividualId = 1000,
            OrganisationBusinessEntityId = 10, WorkTemplateId = template, SchedulePurpose = purpose, EffectiveFrom = today,
            ScheduleMode = "Weekly", WorkingDaysMask = 127 };
        Check((await service.AssignAsync(Request(1, "Official"))).Success, "Official assignment failed");
        Check((await service.AssignAsync(Request(2, "Duty"))).Success, "Duty assignment failed");
        var schedule = await new StaffScheduleService(factory, access).GetScheduleAsync(1000);
        Check(schedule.Success && schedule.Schedule!.CurrentTemplates.Count == 2, "Staff schedule did not show both templates");
        Check(schedule.Schedule.CurrentTemplate!.SchedulePurpose == "Official", "Duty replaced official summary");
        var provider = Proxy.For<IPlanningProviderResolver>((method, values) => Task.FromResult(new PlanningProviderResolution { PlanningProviderId = 1, Code = "WORK_PLANNING" }));
        var generator = new WorkPlanGenerator(factory, provider);
        var plan = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(today));
        Check(plan!.Assignments.Count == 2 && plan.Segments.Count == 2, "Overlapping official and duty schedules were collapsed");
        Check(plan.HourCoverage.OfficialMinutes == 360 && plan.HourCoverage.DutyWithinOfficialMinutes == 120 && plan.HourCoverage.PotentialOvertimeMinutes == 0,
            "Generated duty was not classified within regular hours");
        var again = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(today));
        Check(again!.WorkPlanId == plan.WorkPlanId && again.Assignments.Count == 2, "Repeated generation duplicated daily plan or assignments");
        await using (var db = factory.CreateDbContext())
            Check(await db.WorkAssignmentSegments.CountAsync() == 2 && await db.WorkAssignmentOwners.CountAsync() == 2, "Separate attendance segments or owners missing");
        Check((await service.AssignAsync(Request(3, "Duty"))).Success, "Changing duty schedule failed");
        await using (var db = factory.CreateDbContext())
            Check(await db.JobWorkTemplateAssignments.AnyAsync(x => x.WorkTemplateID == 1 && x.IsActive && x.EffectiveTo == null), "Changing duty closed the official schedule");
        var changed = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(today));
        Check(changed!.Assignments.Count == 3 && changed.Assignments.Any(x => x.WorkTemplateId == 3) && changed.Assignments.Any(x => x.WorkTemplateId == 2), "Unused plan was not rebuilt after duty change");
        var current = (await new StaffScheduleService(factory, access).GetScheduleAsync(1000)).Schedule!;
        var duty = current.CurrentTemplates.Single(x => x.SchedulePurpose == "Duty" && x.WorkTemplateId == 3);
        Check((await service.RemoveAsync(new RemoveJobWorkTemplateDto { JobId = 100, JobWorkTemplateAssignmentId = duty.JobWorkTemplateAssignmentId, EffectiveDate = today })).Success,
            "Removing duty schedule failed");
        var removed = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(today));
        Check(removed!.Assignments.Count == 2 && removed.Assignments.Any(x => x.WorkTemplateId == 1) && removed.Assignments.Any(x => x.WorkTemplateId == 2) && !removed.Assignments.Any(x => x.WorkTemplateId == 3), "Removing duty removed official hours or retained stale duty hours");
        var remainingDuty=(await new StaffScheduleService(factory,access).GetScheduleAsync(1000)).Schedule!.CurrentTemplates.Single(x=>x.SchedulePurpose=="Duty"&&x.WorkTemplateId==2);
        Check((await service.RemoveAsync(new RemoveJobWorkTemplateDto{JobId=100,JobWorkTemplateAssignmentId=remainingDuty.JobWorkTemplateAssignmentId,EffectiveDate=today})).Success,"Second duty could not be ended independently");
        removed=await generator.GenerateOrGetAsync(1000,100,10,DateOnly.FromDateTime(today));
        Check(removed!.Assignments.Count==1&&removed.Assignments[0].WorkTemplateId==1,"Ending all duties affected official hours");
        await using (var db = factory.CreateDbContext())
        {
            db.PlanningProviders.Add(new PlanningProvider { PlanningProviderId = 1, OrganisationBusinessEntityID = 10, Code = "WORK_PLANNING", IsActive = true });
            db.OrganisationWorkPlanningSettings.Add(new OrganisationWorkPlanningSetting { OrganisationWorkPlanningSettingId = 1,
                OrganisationBusinessEntityId = 10, PlanningProviderId = 1, IsActive = true });
            db.AttendanceLogs.Add(new AttendanceLog { AttendanceLogId = 1, IndividualId = 1000, OrganisationId = 10, Date = today.AddHours(8) });
            await db.SaveChangesAsync();
        }
        var sameDayDuty = Request(2, "Duty"); sameDayDuty.EffectiveTo = today;
        Check((await service.AssignAsync(sameDayDuty)).Success, "Duty could not be attached to an already used day");
        var manualGenerator = new HRM.Services.WorkPlanning.WorkAssignmentGeneratorService(factory,
            NullLogger<HRM.Services.WorkPlanning.WorkAssignmentGeneratorService>.Instance, access);
        var manualRequest = new HRM.DTOs.WorkPlanning.GenerateWorkPlanRequest { JobId = 100, IndividualId = 1000,
            OrganisationBusinessEntityId = 10, WorkTemplateId = 2, WorkDate = today, OperationLogId = 1, GeneratedByUserId = 1,
            GenerationSource = HRM.Enum.WorkPlanGenerationSource.Manual, AssignmentSource = HRM.Enum.WorkAssignmentSource.Manual };
        var manual = await manualGenerator.GenerateAsync(manualRequest);
        Check(manual.Success && manual.WorkPlanId == removed.WorkPlanId, "Manual duty could not join the existing official daily plan: " + manual.Message);
        Check(!(await manualGenerator.GenerateAsync(manualRequest)).Success, "Repeated manual generation duplicated duty");
        var combined = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(today));
        Check(combined!.Assignments.Count == 2 && combined.Segments.Count == 2, "Manual duty segments were missing from the combined attendance plan");
        var foreign = Request(2, "Duty"); foreign.OrganisationBusinessEntityId = 20;
        Check(!(await service.AssignAsync(foreign)).Success, "Cross-tenant assignment was allowed");
        attachAllowed = false;
        Check(!(await service.AssignAsync(Request(2, "Duty"))).Success, "Missing Schedule.Attach permission was ignored");
        attachAllowed = true;
        var dutyRequest = Request(3, "Duty"); dutyRequest.EffectiveFrom = today.AddDays(1); dutyRequest.EffectiveTo = dutyRequest.EffectiveFrom;
        Check((await service.AssignAsync(dutyRequest)).Success, "Configured dutyRequest template assignment failed");
        int assignmentCount;
        await using (var db = factory.CreateDbContext()) assignmentCount = await db.JobWorkTemplateAssignments.CountAsync();
        Check((await service.AssignAsync(dutyRequest)).Success, "Configured dutyRequest template reassignment failed");
        await using (var db = factory.CreateDbContext())
        {
            Check(await db.WorkTemplates.CountAsync() == 3 && await db.WorkTemplateSegments.CountAsync() == 3,
                "Duty assignment duplicated templates or segments");
            Check(await db.JobWorkTemplateAssignments.CountAsync() == assignmentCount, "Saving the same duty twice duplicated staff assignments");
            Check(await db.JobWorkTemplateAssignments.AnyAsync(x => x.WorkTemplateID == 1 && x.IsActive && x.EffectiveTo == null), "Duty replaced official hours");
            Check(!await db.JobWorkTemplateAssignments.AnyAsync(x => x.WorkTemplateID == 2 && x.IsActive && x.EffectiveFrom == today.AddDays(2)), "Temporary duty unexpectedly revived a separately ended duty template");
        }
        var tomorrow = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(today.AddDays(1)));
        var expectedTomorrowAssignments = 2;
        Check(tomorrow!.Assignments.Count == expectedTomorrowAssignments && tomorrow.Assignments.Any(x => x.WorkTemplateId == 3), "Selected dutyRequest template was not used in the plan: "+string.Join(",",tomorrow!.Assignments.Select(x=>$"{x.WorkTemplateId}:{x.SchedulePurpose}:{x.IsValid}")));
        dutyRequest.OrganisationBusinessEntityId = 20;
        Check(!(await service.AssignAsync(dutyRequest)).Success, "Duty accepted another tenant");
        dutyRequest.OrganisationBusinessEntityId = 10; dutyRequest.EffectiveTo = today;
        Check(!(await service.AssignAsync(dutyRequest)).Success, "Invalid dutyRequest dates accepted");
        await CheckEvaluationAsync();
        await CheckSimpleDutyFormAsync();
        var friday = today.AddDays(14);
        while (friday.DayOfWeek != DayOfWeek.Friday) friday = friday.AddDays(1);
        var dutyPattern = new JobWorkTemplateAssignment { SchedulePurpose = "Duty", WorkTemplateID = 3, EffectiveFrom = friday,
            ScheduleMode = "Weekly", WorkingDaysMask = 1 << (int)DayOfWeek.Friday };
        Check(HRM.Helpers.StaffScheduleCalendar.TemplateOn(dutyPattern, friday) == 3 && HRM.Helpers.StaffScheduleCalendar.TemplateOn(dutyPattern, friday.AddDays(2)) == 0,
            "Weekly duty ignored the selected weekdays");
        var rotatingDuty = Request(3, "Duty"); rotatingDuty.ScheduleMode = "Rotation"; rotatingDuty.RotationAnchorDate = friday;
        rotatingDuty.EffectiveFrom = friday; rotatingDuty.RotationTemplateIds = [3, 0];
        Check((await service.AssignAsync(rotatingDuty)).Success, "Duty rotation could not reuse existing templates");
        await using (var db = factory.CreateDbContext())
        {
            db.Jobs.Add(new Job { JobId = 101, IndividualID = 1001, OrganisationID = 10, JobStateId = 4 });
            await db.SaveChangesAsync();
        }
        var pattern = new AssignJobWorkTemplateDto { JobId = 101, IndividualId = 1001, OrganisationBusinessEntityId = 10,
            WorkTemplateId = 1, EffectiveFrom = friday };
        Check((await service.AssignAsync(pattern)).Success, "Normal staff pattern failed");
        await using (var db = factory.CreateDbContext())
        {
            var rest = await HRM.Helpers.StaffScheduleCalendar.RestDaysAsync(db, 1001, friday, friday.AddDays(3));
            Check(rest.Contains(friday) && rest.Contains(friday.AddDays(1)) && !rest.Contains(friday.AddDays(2)), "Normal Friday/Saturday weekends incorrect");
        }
        Check(await generator.GenerateOrGetAsync(1001, 101, 10, DateOnly.FromDateTime(friday)) == null, "Normal staff got a mandatory Friday plan");
        pattern.ScheduleMode = "Weekly"; pattern.WorkingDaysMask = (1 << (int)DayOfWeek.Friday) | (1 << (int)DayOfWeek.Saturday);
        Check((await service.AssignAsync(pattern)).Success, "Weekly shift configuration failed");
        Check((await generator.GenerateOrGetAsync(1001, 101, 10, DateOnly.FromDateTime(friday)))?.Assignments.Count == 1, "Friday shift work was excluded as weekend");
        pattern.ScheduleMode = "Rotation"; pattern.RotationAnchorDate = friday; pattern.RotationTemplateIds = [1, 2, 0, 0];
        Check((await service.AssignAsync(pattern)).Success, "Rotation configuration failed");
        var shift1 = await generator.GenerateOrGetAsync(1001, 101, 10, DateOnly.FromDateTime(friday));
        var shift2 = await generator.GenerateOrGetAsync(1001, 101, 10, DateOnly.FromDateTime(friday.AddDays(1)));
        Check(shift1?.Assignments.Single().WorkTemplateId == 1 && shift2?.Assignments.Single().WorkTemplateId == 2, "Rotation did not select the correct shift on Friday/Saturday");
        Check(await generator.GenerateOrGetAsync(1001, 101, 10, DateOnly.FromDateTime(friday.AddDays(2))) == null, "Cycle rest day generated mandatory attendance");
        await using (var db = factory.CreateDbContext())
        {
            var rest = await HRM.Helpers.StaffScheduleCalendar.RestDaysAsync(db, 1001, friday, friday.AddDays(7));
            Check(!rest.Contains(friday) && !rest.Contains(friday.AddDays(1)) && rest.Contains(friday.AddDays(2)) &&
                rest.Contains(friday.AddDays(3)) && !rest.Contains(friday.AddDays(4)), "Rotation calendar did not repeat work/rest days");
        }
        var displayed = await service.GetCurrentAsync(101, friday);
        Check(displayed?.ScheduleMode == "Rotation" && displayed.RotationTemplateIds == "[1,2,0,0]", "Rotation settings did not round-trip through the staff schedule");
        pattern.RotationTemplateIds = [0, 0];
        Check(!(await service.AssignAsync(pattern)).Success, "All-rest cycle was accepted");
        pattern.RotationTemplateIds = [1, 99999];
        Check(!(await service.AssignAsync(pattern)).Success, "Unavailable or foreign shift accepted in rotation");
    }

    private static async Task CheckSimpleDutyFormAsync()
    {
        AssignJobWorkTemplateDto? submitted = null;
        AssignJobWorkTemplateDto? submittedPattern = null;
        var searched = false;
        var saved = false;
        var service = Proxy.For<HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment.IJobWorkTemplateAssignmentService>((method, values) => method switch
        {
            "AssignAsync" => CapturePattern((AssignJobWorkTemplateDto)values[0]!),
            "SearchTemplatesAsync" => Search(),
            _ => throw new Exception(method)
        });
        Task<HRM.DTOs.ServiceResult> CapturePattern(AssignJobWorkTemplateDto dto) { submitted = dto; submittedPattern = dto; return Task.FromResult(HRM.DTOs.ServiceResult.Ok("Saved")); }
        Task<List<HRM.DTOs.WorkPlanning.WorkTemplateLookupDto>> Search() { searched = true; return Task.FromResult(new List<HRM.DTOs.WorkPlanning.WorkTemplateLookupDto> { new() { WorkTemplateId = 1, Name = "Morning" }, new() { WorkTemplateId = 2, Name = "Evening" } }); }
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions.AddLogging(services);
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<Microsoft.AspNetCore.Components.IComponentActivator, TestActivator>(services);
        var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        await using var renderer = new TestRenderer(provider);
        var form = new HRM.Components.Admin.My.Workspace.Staff.SchduleComponents.Summary.ItemComponents.AssignTemplateDrawer
        {
            IsOpen = true, SchedulePurpose = "Duty", StaffName = "Schedule Staff", StaffId = 1000, JobId = 100, OrganisationBusinessEntityId = 10,
            OnSaved = Microsoft.AspNetCore.Components.EventCallback.Factory.Create(new object(), () => saved = true)
        };
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        form.GetType().GetProperty("AssignmentService", flags)!.SetValue(form, service);
        await renderer.Dispatcher.InvokeAsync(async () => { renderer.Attach(form); await renderer.Render(); });
        renderer.CheckErrors();
        Check(searched && renderer.RootText().Contains("Select a configured template") && !renderer.RootText().Contains("Duty name"),
            "Duty form did not reuse templates configured in Settings");
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            form.GetType().GetField("templateId", flags)!.SetValue(form, 2);
            await renderer.Render();
            await renderer.RootClick("Save schedule");
        });
        renderer.CheckErrors();
        Check(saved && submitted?.SchedulePurpose == "Duty" && submitted.WorkTemplateId == 2 && submitted.EffectiveTo == submitted.EffectiveFrom && submitted.JobId == 100,
            "Duty form did not assign the selected existing template for one day");
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            form.IsOpen = false; await renderer.Render();
            form.SchedulePurpose = "Official"; form.IsOpen = true; await renderer.Render();
            form.GetType().GetField("scheduleMode", flags)!.SetValue(form, "Rotation");
            form.GetType().GetField("rotation", flags)!.SetValue(form, new List<int> { 1, 2, 0 });
            await renderer.Render();
        });
        renderer.CheckErrors();
        Check(renderer.RootText().Contains("Cycle day 1 date") && renderer.RootText().Contains("Add cycle day") && renderer.RootText().Contains("Rest day"),
            "Rotation form did not expose anchor, ordered shift days and rest days");
        await renderer.Dispatcher.InvokeAsync(async () => { await renderer.RootClick("Add cycle day"); await renderer.RootClick("Save schedule"); });
        renderer.CheckErrors();
        Check(submittedPattern?.ScheduleMode == "Rotation" && submittedPattern.RotationTemplateIds.SequenceEqual([1, 2, 0, 0]) &&
            submittedPattern.RotationAnchorDate.HasValue && submittedPattern.WorkTemplateId == 1,
            "Rotation form did not save the displayed repeating pattern");
        await provider.DisposeAsync();
    }

    private static async Task CheckEvaluationAsync()
    {
        var options = new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), x => x.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        var factory = new ReceiptFactory(options);
        var date = DateTime.UtcNow.AddHours(5).Date.AddDays(-1);
        await using (var db = factory.CreateDbContext())
        {
            db.Jobs.Add(new Job { JobId = 100, IndividualID = 1000, OrganisationID = 10, JobStateId = 4, JoinedDate = date.AddYears(-1) });
            db.JobPositions.Add(new JobPosition { JobPositionId = 1, JobId = 100, PositionId = 1, JobPositionStateId = 4, FromDate = date.AddYears(-1) });
            db.WorkAssignmentStates.Add(new WorkAssignmentState { WorkAssignmentStateId = 1, Code = "PLANNED", IsActive = true });
            db.WorkSegmentTypes.AddRange(new WorkSegmentType { WorkSegmentTypeId = 1, Code = "CHECK_IN", Name = "Check in" },
                new WorkSegmentType { WorkSegmentTypeId = 2, Code = "CHECK_OUT", Name = "Check out" });
            db.WorkTemplates.AddRange(
                new WorkTemplate { WorkTemplateId = 1, OrganisationBusinessEntityId = 10, Name = "Office", IsActive = true, DefaultStartTime = new(8, 0), DefaultEndTime = new(14, 0),
                    WorkTemplateSegments = [new() { WorkTemplateSegmentId = 1, SequenceNumber = 1, WorkSegmentTypeId = 1, Name = "In", IsActive = true, IsMandatory = true, RequiresAttendance = true, DurationMinutes = 30 },
                        new() { WorkTemplateSegmentId = 2, SequenceNumber = 2, WorkSegmentTypeId = 2, Name = "Out", IsActive = true, RequiresAttendance = true, OffsetMinutes = 360, DurationMinutes = 30 }] },
                new WorkTemplate { WorkTemplateId = 2, OrganisationBusinessEntityId = 10, Name = "Duty", IsActive = true, DefaultStartTime = new(9, 0), DefaultEndTime = new(11, 0),
                    WorkTemplateSegments = [new() { WorkTemplateSegmentId = 3, SequenceNumber = 1, WorkSegmentTypeId = 1, Name = "In", IsActive = true, IsMandatory = true, RequiresAttendance = true, DurationMinutes = 30 },
                        new() { WorkTemplateSegmentId = 4, SequenceNumber = 2, WorkSegmentTypeId = 2, Name = "Out", IsActive = true, RequiresAttendance = true, OffsetMinutes = 120, DurationMinutes = 30 }] });
            db.JobWorkTemplateAssignments.AddRange(new JobWorkTemplateAssignment { JobID = 100, WorkTemplateID = 1, SchedulePurpose = "Official", ScheduleMode = "Weekly", WorkingDaysMask = 127, IsActive = true, EffectiveFrom = date.AddDays(-10) },
                new JobWorkTemplateAssignment { JobID = 100, WorkTemplateID = 2, SchedulePurpose = "Duty", IsActive = true, EffectiveFrom = date.AddDays(-10) });
            await db.SaveChangesAsync();
        }
        var provider = Proxy.For<IPlanningProviderResolver>((method, values) => Task.FromResult(new PlanningProviderResolution { PlanningProviderId = 1, Code = "WORK_PLANNING" }));
        var generator = new WorkPlanGenerator(factory, provider);
        async Task AddClocks(long planId, DateTime day, int dutyStart, int dutyEnd)
        {
            await using var db = factory.CreateDbContext();
            foreach (var (source, hour, mode) in new[] { (1, 8, 1), (3, dutyStart, 1), (4, dutyEnd, 2), (2, 14, 2) })
            {
                var segment = await db.WorkPlanSegments.SingleAsync(x => x.WorkPlanId == planId && x.WorkTemplateSegmentId == source);
                db.AttendanceLogResolutions.Add(new AttendanceLogResolution { WorkPlanId = planId, WorkPlanSegmentId = segment.WorkPlanSegmentId,
                    JobId = 100, IsValid = true, AttendanceClockTypeId = mode,
                    AttendanceLog = new AttendanceLog { IndividualId = 1000, OrganisationId = 10, Date = day.AddHours(hour) } });
            }
            await db.SaveChangesAsync();
        }
        var plan = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(date));
        await AddClocks(plan!.WorkPlanId, date, 9, 11);
        var evaluated = await new HRM.Services.Attendance.AttendanceEvaluationService(factory).EvaluateAsync(1000, 100, date);
        Check(evaluated.PlannedMinutes == 360 && evaluated.WorkedMinutes == 360 && evaluated.PotentialOvertimeMinutes == 0,
            "Duty clock events shortened or doubled the official working period");
        await using (var db = factory.CreateDbContext())
        {
            var duty = await db.WorkTemplates.SingleAsync(x => x.WorkTemplateId == 2);
            duty.DefaultStartTime = new(13, 0); duty.DefaultEndTime = new(16, 0);
            (await db.WorkTemplateSegments.SingleAsync(x => x.WorkTemplateSegmentId == 4)).OffsetMinutes = 180;
            await db.SaveChangesAsync();
        }
        var nextDate = date.AddDays(-1);
        var extended = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(nextDate));
        await AddClocks(extended!.WorkPlanId, nextDate, 13, 16);
        var extendedResult = await new HRM.Services.Attendance.AttendanceEvaluationService(factory).EvaluateAsync(1000, 100, nextDate);
        Check(extendedResult.PlannedMinutes == 360 && extendedResult.WorkedMinutes == 360 && extendedResult.PotentialOvertimeMinutes == 120 && extendedResult.OvertimeMinutes == 0,
            "Duty beyond office hours changed official hours or automatically became approved OT");
        var singleClockDate = date.AddDays(-2);
        var singleClockPlan = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(singleClockDate));
        await using (var db = factory.CreateDbContext())
        {
            foreach (var (source, hour, mode) in new[] { (1, 8, 1), (4, 16, 2) })
            {
                var segment = await db.WorkPlanSegments.SingleAsync(x => x.WorkPlanId == singleClockPlan!.WorkPlanId && x.WorkTemplateSegmentId == source);
                db.AttendanceLogResolutions.Add(new AttendanceLogResolution { WorkPlanId = singleClockPlan!.WorkPlanId, WorkPlanSegmentId = segment.WorkPlanSegmentId,
                    JobId = 100, IsValid = true, AttendanceClockTypeId = mode,
                    AttendanceLog = new AttendanceLog { IndividualId = 1000, OrganisationId = 10, Date = singleClockDate.AddHours(hour) } });
            }
            await db.SaveChangesAsync();
        }
        var singleClockResult = await new HRM.Services.Attendance.AttendanceEvaluationService(factory).EvaluateAsync(1000, 100, singleClockDate);
        Check(singleClockResult.WorkedMinutes == 0 && singleClockResult.PotentialOvertimeMinutes == 120,
            "A physical clock-in/out spanning office and duty was discarded");
        Check(singleClockResult.IsComplete && singleClockResult.LastCheckOut == null &&
            !singleClockResult.Issues.Any(x => x.Message.Contains("Duty")),
            "Duty checkout leaked into official attendance despite its optional official checkout");
        await using (var db = factory.CreateDbContext())
        {
            var duty = await db.WorkTemplates.SingleAsync(x => x.WorkTemplateId == 2);
            duty.DefaultStartTime = new(10, 0); duty.DefaultEndTime = new(12, 0);
            (await db.WorkTemplateSegments.SingleAsync(x => x.WorkTemplateSegmentId == 4)).OffsetMinutes = 120;
            await db.SaveChangesAsync();
        }
        var mandatoryDate = date.AddDays(-3);
        var mandatoryPlan = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(mandatoryDate));
        var resolver = new HRM.Services.AttendanceWorkPlanResolver(factory, generator);
        var officePunch = await resolver.ResolveAsync(1000, 100, 10, mandatoryDate.AddHours(8));
        var dutyPunch = await resolver.ResolveAsync(1000, 100, 10, mandatoryDate.AddHours(10));
        Check(dutyPunch.ClockType == HRM.Enum.AttendanceClockType.CheckIn, "10:00 duty check-in was treated as checkout");
        Check((await resolver.ResolveAsync(1000, 100, 10, mandatoryDate.AddHours(12))).ClockType == HRM.Enum.AttendanceClockType.CheckOut,
            "12:00 duty check-out was treated as check-in");
        Check((await resolver.ResolveAsync(1000, 100, 10, mandatoryDate.AddHours(14))).ClockType == HRM.Enum.AttendanceClockType.CheckOut,
            "14:00 official checkout was lost after adding duty");
        Check(officePunch.IsResolved && dutyPunch.IsResolved && officePunch.WorkPlanSegmentId != dutyPunch.WorkPlanSegmentId,
            "08:00 and 10:00 punches did not resolve to separate schedules");
        await using (var db = factory.CreateDbContext())
        {
            foreach (var (source, hour, mode) in new[] { (1, 8, 1), (2, 14, 2) })
            {
                var segment = await db.WorkPlanSegments.SingleAsync(x => x.WorkPlanId == mandatoryPlan!.WorkPlanId && x.WorkTemplateSegmentId == source);
                db.AttendanceLogResolutions.Add(new AttendanceLogResolution { WorkPlanId = mandatoryPlan!.WorkPlanId, WorkPlanSegmentId = segment.WorkPlanSegmentId,
                    JobId = 100, IsValid = true, AttendanceClockTypeId = mode,
                    AttendanceLog = new AttendanceLog { IndividualId = 1000, OrganisationId = 10, Date = mandatoryDate.AddHours(hour) } });
            }
            await db.SaveChangesAsync();
        }
        var dutyAfterOfficialPunch = await resolver.ResolveAsync(1000, 100, 10, mandatoryDate.AddHours(10));
        Check(dutyAfterOfficialPunch.ClockType == HRM.Enum.AttendanceClockType.CheckIn && dutyAfterOfficialPunch.WorkPlanSegmentId == dutyPunch.WorkPlanSegmentId,
            "An existing official check-in caused duty to toggle to checkout");
        var missingDuty = await new HRM.Services.Attendance.AttendanceEvaluationService(factory).EvaluateAsync(1000, 100, mandatoryDate);
        Check(missingDuty.IsComplete && !missingDuty.Issues.Any(x =>
            x.Type == HRM.Enum.AttendanceEvaluationIssueType.MissingCheckIn && x.WorkPlanSegmentId == dutyPunch.WorkPlanSegmentId),
            "Missing duty check-in affected official attendance");
        await using (var db = factory.CreateDbContext())
        {
            var dutySegment = await db.WorkPlanSegments.SingleAsync(x => x.WorkPlanSegmentId == dutyPunch.WorkPlanSegmentId);
            dutySegment.IsMandatory = false; await db.SaveChangesAsync();
        }
        var optionalDuty = await new HRM.Services.Attendance.AttendanceEvaluationService(factory).EvaluateAsync(1000, 100, mandatoryDate);
        Check(optionalDuty.IsComplete && !optionalDuty.Issues.Any(x => x.Type == HRM.Enum.AttendanceEvaluationIssueType.MissingCheckIn),
            "Optional duty check-in was treated as mandatory");
        await using (var db = factory.CreateDbContext())
        {
            (await db.WorkPlanSegments.SingleAsync(x => x.WorkPlanSegmentId == dutyPunch.WorkPlanSegmentId)).IsMandatory = true;
            db.AttendanceLogResolutions.Add(new AttendanceLogResolution { WorkPlanId = mandatoryPlan!.WorkPlanId, WorkPlanSegmentId = dutyPunch.WorkPlanSegmentId,
                JobId = 100, IsValid = true, AttendanceClockTypeId = 1,
                AttendanceLog = new AttendanceLog { IndividualId = 1000, OrganisationId = 10, Date = mandatoryDate.AddHours(10) } });
            await db.SaveChangesAsync();
        }
        var bothChecks = await new HRM.Services.Attendance.AttendanceEvaluationService(factory).EvaluateAsync(1000, 100, mandatoryDate);
        Check(bothChecks.IsComplete && bothChecks.WorkedMinutes == 360 && bothChecks.PotentialOvertimeMinutes == 0,
            "Separate mandatory check-ins did not complete attendance or double-counted hours");
        var nightDate = date.AddDays(-8);
        await using (var db = factory.CreateDbContext())
        {
            var office = await db.WorkTemplates.SingleAsync(x => x.WorkTemplateId == 1);
            office.DefaultStartTime = new(22, 0); office.DefaultEndTime = new(6, 0); office.EndsNextDay = true;
            (await db.WorkTemplateSegments.SingleAsync(x => x.WorkTemplateSegmentId == 2)).OffsetMinutes = 480;
            var official = await db.JobWorkTemplateAssignments.SingleAsync(x => x.SchedulePurpose == "Official");
            official.ScheduleMode = "Rotation"; official.RotationAnchorDate = nightDate; official.RotationTemplateIds = "[1,0]";
            (await db.JobWorkTemplateAssignments.SingleAsync(x => x.SchedulePurpose == "Duty")).IsActive = false;
            await db.SaveChangesAsync();
        }
        var nightPlan = await generator.GenerateOrGetAsync(1000, 100, 10, DateOnly.FromDateTime(nightDate));
        var overnightOut = await resolver.ResolveAsync(1000, 100, 10, nightDate.AddDays(1).AddHours(6));
        Check(overnightOut.IsResolved && overnightOut.ClockType == HRM.Enum.AttendanceClockType.CheckOut && overnightOut.WorkPlanId == nightPlan!.WorkPlanId,
            "Overnight rotating shift checkout was lost on the following rest day");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}

