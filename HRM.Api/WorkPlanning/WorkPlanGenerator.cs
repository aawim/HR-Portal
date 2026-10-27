using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models.WorkPlanning;
using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning
{
    public class WorkPlanGenerator : IWorkPlanGenerator
    {
        private const string PlannedAssignmentStateCode = "PLANNED";

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IPlanningProviderResolver _providerResolver;

        public WorkPlanGenerator(
            IDbContextFactory<HrmTeContext> dbFactory,
            IPlanningProviderResolver providerResolver)
        {
            _dbFactory = dbFactory;
            _providerResolver = providerResolver;
        }

        public async Task<AttendanceWorkPlanDto?> GenerateOrGetAsync(
            int individualId, int jobId, int organisationBusinessEntityId, DateOnly workDate,
            CancellationToken cancellationToken = default)
        {
            var provider = await _providerResolver.ResolveAsync(organisationBusinessEntityId, workDate, cancellationToken);
            if (!provider.UsesWorkPlanning) return null;
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var date = workDate.ToDateTime(TimeOnly.MinValue);
            if (!await db.Jobs.AnyAsync(x => x.JobId == jobId && x.IndividualID == individualId &&
                x.OrganisationID == organisationBusinessEntityId, cancellationToken))
                throw new UnauthorizedAccessException("The job does not belong to this staff member and organisation.");
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var plan = await db.WorkPlans.Include(x => x.WorkPlanSegments)
                .Include(x => x.WorkAssignments).ThenInclude(x => x.WorkAssignmentOwners)
                .Include(x => x.WorkAssignments).ThenInclude(x => x.WorkAssignmentSegments)
                .SingleOrDefaultAsync(x => x.JobId == jobId && x.WorkDate == date && x.IsValid, cancellationToken);
            if (plan != null && (plan.IndividualId != individualId || plan.OrganisationBusinessEntityId != organisationBusinessEntityId))
                throw new UnauthorizedAccessException("The existing work plan belongs to another staff member or organisation.");
            if (plan?.IsFinalized == true) return await HRM.Helpers.OfficialScheduleAmendments.ApplyAsync(db, MapToDto(plan), cancellationToken);

            var selections = new List<(WorkTemplate Template, TimeOnly? Start, int Priority, string Purpose)>();
            var job = await db.Jobs.AsNoTracking().SingleAsync(j=>j.JobId==jobId && j.OrganisationID==organisationBusinessEntityId, cancellationToken);
            var candidates = await HRM.Helpers.EffectiveStaffSchedules.LoadAsync(db,job,date,date,cancellationToken);
            var attached = HRM.Helpers.EffectiveStaffSchedules.On(candidates,date).Select(c=>c.Assignment).ToList();
            foreach (var item in attached)
            {
                if(item.SchedulePurpose=="Duty" && !attached.Any(a=>a.SchedulePurpose=="Official")) continue;
                var templateId = HRM.Helpers.StaffScheduleCalendar.TemplateOn(item,date);
                if(templateId==0) continue;
                var template = templateId==item.WorkTemplateID ? item.WorkTemplate : await db.WorkTemplates.AsNoTracking().Include(t=>t.WorkTemplateSegments)
                    .SingleOrDefaultAsync(t=>t.WorkTemplateId==templateId && t.IsActive && (t.IsGlobal || t.OrganisationBusinessEntityId==organisationBusinessEntityId) &&
                        (!t.EffectiveFrom.HasValue || t.EffectiveFrom<=date) && (!t.EffectiveTo.HasValue || t.EffectiveTo>=date),cancellationToken);
                if(template==null) throw new InvalidOperationException("A schedule template is unavailable. Please contact HR.");
                selections.Add((template,item.ScheduleMode=="Rotation"?null:item.ScheduledStartTime,0,item.SchedulePurpose));
            }
            var hasRecordedAttendance = plan != null && (plan.WorkPlanSegments.Any(s=>s.IsCompleted || s.AttendanceId.HasValue) ||
                await db.AttendanceLogResolutions.AnyAsync(r=>r.WorkPlanId==plan.WorkPlanId,cancellationToken) ||
                await db.AttendanceLogs.AnyAsync(l=>l.IndividualId==individualId && l.OrganisationId==organisationBusinessEntityId && l.Date.Date==date,cancellationToken));
            if(plan!=null && !plan.IsManual && !hasRecordedAttendance)
            {
                foreach(var assignment in plan.WorkAssignments.Where(a=>a.IsValid && a.AssignmentSource==WorkAssignmentSource.Template))
                {
                    var desired=selections.Where(s=>s.Template.WorkTemplateId==assignment.WorkTemplateId && s.Purpose==assignment.SchedulePurpose).ToList();
                    if(desired.Count==1 && (desired[0].Start??desired[0].Template.DefaultStartTime) is TimeOnly clock && assignment.StartDateTime==date.Add(clock.ToTimeSpan())) continue;
                    foreach(var snapshot in plan.WorkPlanSegments.Where(s=>s.IsValid && s.SchedulePurpose==assignment.SchedulePurpose &&
                        assignment.WorkAssignmentSegments.Any(a=>a.IsValid && a.WorkTemplateSegmentId==s.WorkTemplateSegmentId && a.WorkSegmentTypeId==s.WorkSegmentTypeId && a.StartDateTime==s.StartDateTime))) snapshot.IsValid=false;
                    assignment.IsValid=false;
                    foreach(var segment in assignment.WorkAssignmentSegments)segment.IsValid=false;
                    foreach(var owner in assignment.WorkAssignmentOwners)owner.IsValid=false;
                }
            }
            if(selections.Count==0)
            {
                if(plan!=null && !plan.IsManual && !hasRecordedAttendance) plan.IsValid=false;
                await db.SaveChangesAsync(cancellationToken);await tx.CommitAsync(cancellationToken);
                return plan==null || !plan.IsManual && !hasRecordedAttendance ? null : await HRM.Helpers.OfficialScheduleAmendments.ApplyAsync(db,MapToDto(plan),cancellationToken);
            }
            var state = await db.WorkAssignmentStates.Where(x => x.IsActive && x.Code == PlannedAssignmentStateCode)
                .Select(x => (int?)x.WorkAssignmentStateId).SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("An active PLANNED work-assignment state is required.");
            var now = DateTime.UtcNow;
            if (plan == null)
            {
                plan = new WorkPlan { IndividualId = individualId, JobId = jobId, OrganisationBusinessEntityId = organisationBusinessEntityId,
                    PlanningProviderId = provider.PlanningProviderId, WorkDate = date, GenerationSource = WorkPlanGenerationSource.Template,
                    GeneratedDate = now, CreatedDate = now, IsValid = true, IsGenerated = true, PlanGuid = Guid.NewGuid(),
                    Version = (await db.WorkPlans.Where(x => x.JobId == jobId && x.WorkDate == date).MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1 };
                db.WorkPlans.Add(plan);
            }
            foreach (var selection in selections)
            {
                if(selection.Purpose=="Official" && (hasRecordedAttendance || plan.IsManual) && plan.WorkAssignments.Any(a=>a.IsValid && a.SchedulePurpose=="Official")) continue;
                if (selection.Purpose == "Duty" && plan.WorkAssignments.Any(x => x.WorkTemplateId == selection.Template.WorkTemplateId &&
                    x.SchedulePurpose == "Duty" && x.CancellationReason != null && x.CancellationReason.StartsWith(HRM.WorkPlanning.Services.DutyRosterService.MovementMarker))) continue;
                if (plan.WorkAssignments.Any(x => x.IsValid && x.WorkTemplateId == selection.Template.WorkTemplateId && x.SchedulePurpose == selection.Purpose)) continue;
                AddTemplate(plan, selection.Template, selection.Start, selection.Priority, state, now, workDate, selection.Purpose);
            }
            var ids = plan.WorkAssignments.Where(x => x.IsValid).Select(x => x.WorkTemplateId).Distinct().ToList();
            plan.WorkTemplateId = ids.Count == 1 ? ids[0] : null;
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return await HRM.Helpers.OfficialScheduleAmendments.ApplyAsync(db, MapToDto(plan), cancellationToken);
        }

        private static void AddTemplate(WorkPlan plan, WorkTemplate template, TimeOnly? assignedStart, int priority, int state, DateTime now, DateOnly date, string purpose)
        {
            var start = assignedStart ?? template.DefaultStartTime ?? throw new InvalidOperationException($"Template '{template.Name}' has no start time.");
            var sources = template.WorkTemplateSegments.Where(x => x.IsActive).OrderBy(x => x.SequenceNumber).ToList();
            if (sources.Count == 0) throw new InvalidOperationException($"Template '{template.Name}' has no active segments.");
            if (sources.Any(x => x.OffsetMinutes < 0 || x.DurationMinutes <= 0 || x.SequenceNumber <= 0 || x.GraceBeforeMinutes < 0 || x.GraceAfterMinutes < 0) ||
                sources.GroupBy(x => x.SequenceNumber).Any(x => x.Count() > 1))
                throw new InvalidOperationException($"Template '{template.Name}' contains invalid timing or duplicate segment sequence numbers. Update the template before generating plans.");
            var assignment = new WorkAssignment { SchedulePurpose = purpose, WorkTemplateId = template.WorkTemplateId, WorkTemplateTypeId = template.WorkTemplateTypeId,
                WorkAssignmentStateId = state, Name = template.Name, Code = template.Code, Description = template.Description,
                GraceMinutes = template.DefaultGraceMinutes, RequiresAttendance = template.RequiresAttendance, RequiresCheckOut = template.RequiresCheckOut,
                Priority = priority, AssignmentSource = WorkAssignmentSource.Template, IsValid = true, CreatedDate = now };
            foreach (var source in sources)
            {
                var from = date.ToDateTime(start).AddMinutes(source.OffsetMinutes);
                var to = from.AddMinutes(source.DurationMinutes);
                plan.WorkPlanSegments.Add(new WorkPlanSegment { SchedulePurpose = purpose, WorkTemplateSegmentId = source.WorkTemplateSegmentId, WorkSegmentTypeId = source.WorkSegmentTypeId,
                    Name = $"{template.Name} · {source.Name}", Description = source.Description, SequenceNumber = plan.WorkPlanSegments.Count + 1,
                    StartDateTime = from, EndDateTime = to, GraceBeforeMinutes = source.GraceBeforeMinutes, GraceAfterMinutes = source.GraceAfterMinutes,
                    IsMandatory = source.IsMandatory, RequiresAttendance = source.RequiresAttendance, RequiresLocationValidation = source.RequiresLocationValidation,
                    RequiresDeviceValidation = source.RequiresDeviceValidation, IsPaid = source.IsPaid, IsValid = true, CreatedDate = now, OperationLogId = source.OperationLogId });
                assignment.WorkAssignmentSegments.Add(new WorkAssignmentSegment { WorkTemplateSegmentId = source.WorkTemplateSegmentId,
                    WorkSegmentTypeId = source.WorkSegmentTypeId, Name = source.Name, Description = source.Description, SequenceNumber = source.SequenceNumber,
                    StartDateTime = from, EndDateTime = to, GraceBeforeMinutes = source.GraceBeforeMinutes, GraceAfterMinutes = source.GraceAfterMinutes,
                    IsMandatory = source.IsMandatory, RequiresAttendance = source.RequiresAttendance, RequiresLocationValidation = source.RequiresLocationValidation,
                    RequiresDeviceValidation = source.RequiresDeviceValidation, IsValid = true, OperationLogId = source.OperationLogId });
            }
            assignment.StartDateTime = date.ToDateTime(start);
            assignment.EndDateTime = assignment.WorkAssignmentSegments.Max(x => x.EndDateTime!.Value);
            if (template.DefaultEndTime.HasValue)
            {
                assignment.EndDateTime = date.ToDateTime(template.DefaultEndTime.Value);
                if (template.EndsNextDay || assignment.EndDateTime <= date.ToDateTime(start)) assignment.EndDateTime = assignment.EndDateTime.AddDays(1);
            }
            assignment.WorkAssignmentOwners.Add(new WorkAssignmentOwner { IndividualId = plan.IndividualId, JobId = plan.JobId,
                OwnershipType = WorkOwnershipType.Original, AssignedDate = now, EffectiveFrom = assignment.StartDateTime,
                IsCurrentOwner = true, IsValid = true });
            plan.WorkAssignments.Add(assignment);
        }

        private static AttendanceWorkPlanDto MapToDto(WorkPlan plan)
        {
            return new AttendanceWorkPlanDto
            {
                WorkPlanId = plan.WorkPlanId,
                IndividualId = plan.IndividualId,
                JobId = plan.JobId,
                OrganisationId = plan.OrganisationBusinessEntityId,
                WorkDate = plan.WorkDate,
                WorkTemplateId = plan.WorkTemplateId,

                IsFinalized = plan.IsFinalized,
                IsGenerated = plan.IsGenerated,
                IsManual = plan.IsManual,

                Assignments = plan.WorkAssignments
                    .Where(x => x.IsValid)
                    .OrderBy(x => x.StartDateTime)
                    .ThenByDescending(x => x.Priority)
                    .Select(x => new AttendanceWorkAssignmentDto
                    {
                        SchedulePurpose = x.SchedulePurpose,
                        WorkAssignmentId = x.WorkAssignmentId,
                        WorkPlanId = x.WorkPlanId,

                        WorkTemplateId = x.WorkTemplateId,
                        WorkTemplateTypeId = x.WorkTemplateTypeId,
                        WorkAssignmentStateId = x.WorkAssignmentStateId,

                        Name = x.Name,
                        Code = x.Code,
                        Description = x.Description,

                        StartDateTime = x.StartDateTime,
                        EndDateTime = x.EndDateTime,

                        GraceMinutes = x.GraceMinutes,

                        RequiresAttendance = x.RequiresAttendance,
                        RequiresCheckOut = x.RequiresCheckOut,

                        Priority = x.Priority,

                        AssignmentSource = x.AssignmentSource,

                        OwnershipType = x.WorkAssignmentOwners
                            .Where(o =>
                                o.IndividualId == plan.IndividualId &&
                                o.JobId == plan.JobId &&
                                o.IsValid &&
                                o.IsCurrentOwner)
                            .Select(o => (WorkOwnershipType?)o.OwnershipType)
                            .FirstOrDefault(),

                        IsValid = x.IsValid
                    })
                    .ToList(),

                    Segments = plan.WorkPlanSegments
                        .Where(x => x.IsValid)
                        .OrderBy(x => x.SequenceNumber)
                        .Select(x => new AttendanceWorkSegmentDto
                        {
                            WorkAssignmentId = plan.WorkAssignments.Where(a=>a.IsValid && a.SchedulePurpose==x.SchedulePurpose &&
                                a.WorkAssignmentSegments.Any(s=>s.IsValid && s.WorkTemplateSegmentId==x.WorkTemplateSegmentId && s.WorkSegmentTypeId==x.WorkSegmentTypeId && s.StartDateTime==x.StartDateTime))
                                .Select(a=>(long?)a.WorkAssignmentId).FirstOrDefault(),
                            SchedulePurpose = x.SchedulePurpose,
                            WorkPlanSegmentId = x.WorkPlanSegmentId,
                            WorkPlanId = x.WorkPlanId,
                            WorkTemplateSegmentId = x.WorkTemplateSegmentId,
                            WorkSegmentTypeId = x.WorkSegmentTypeId,

                            Name = x.Name,
                            Description = x.Description,

                            SequenceNumber = x.SequenceNumber,

                            StartDateTime = x.StartDateTime,
                            EndDateTime = x.EndDateTime,

                            GraceBeforeMinutes = x.GraceBeforeMinutes,
                            GraceAfterMinutes = x.GraceAfterMinutes,

                            IsMandatory = x.IsMandatory,
                            RequiresAttendance = x.RequiresAttendance,
                            RequiresLocationValidation = x.RequiresLocationValidation,
                            RequiresDeviceValidation = x.RequiresDeviceValidation,

                            IsPaid = x.IsPaid,
                            IsCompleted = x.IsCompleted,
                            AttendanceId = x.AttendanceId
                        })
                    .ToList()
            };
        }




    }
}
