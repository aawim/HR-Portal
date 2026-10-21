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
            if (plan?.IsFinalized == true) return MapToDto(plan);

            var selections = new List<(WorkTemplate Template, TimeOnly? Start, int Priority, string Purpose)>();
            var attached = await db.JobWorkTemplateAssignments.AsNoTracking()
                .Include(x => x.WorkTemplate).ThenInclude(x => x.WorkTemplateSegments)
                .Where(x => x.JobID == jobId && x.IsActive && x.EffectiveFrom <= date &&
                    (x.EffectiveTo == null || x.EffectiveTo >= date) && x.WorkTemplate.IsActive &&
                    (x.WorkTemplate.IsGlobal || x.WorkTemplate.OrganisationBusinessEntityId == organisationBusinessEntityId) &&
                    (!x.WorkTemplate.EffectiveFrom.HasValue || x.WorkTemplate.EffectiveFrom <= date) &&
                    (!x.WorkTemplate.EffectiveTo.HasValue || x.WorkTemplate.EffectiveTo >= date))
                .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.JobWorkTemplateAssignmentID).ToListAsync(cancellationToken);
            foreach (var item in attached.GroupBy(x => x.SchedulePurpose).Select(x => x.First()))
            {
                var templateId = HRM.Helpers.StaffScheduleCalendar.TemplateOn(item, date);
                if (templateId == 0) continue;
                var template = templateId == item.WorkTemplateID ? item.WorkTemplate : await db.WorkTemplates.AsNoTracking()
                    .Include(x => x.WorkTemplateSegments).SingleOrDefaultAsync(x => x.WorkTemplateId == templateId && x.IsActive &&
                        (x.IsGlobal || x.OrganisationBusinessEntityId == organisationBusinessEntityId) &&
                        (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= date) && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date), cancellationToken);
                if (template == null) throw new InvalidOperationException("The rotating shift is no longer available. Ask HR to update the schedule.");
                selections.Add((template, item.ScheduleMode == "Rotation" ? null : item.ScheduledStartTime, 0, item.SchedulePurpose));
            }
            if (attached.Count == 0)
            {
                // Preserve the older weekday-based assignment path for staff not yet migrated.
                var legacy = await db.JobWorkTemplates.AsNoTracking().Include(x => x.WorkTemplate).ThenInclude(x => x.WorkTemplateSegments)
                    .Where(x => x.JobId == jobId && x.IsActive && x.EffectiveFrom <= date &&
                        (x.EffectiveTo == null || x.EffectiveTo >= date) && x.WorkTemplate.IsActive &&
                        (x.WorkTemplate.IsGlobal || x.WorkTemplate.OrganisationBusinessEntityId == organisationBusinessEntityId))
                    .OrderByDescending(x => x.Priority).ThenByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken);
                foreach (var item in legacy.Where(x => AppliesOnDay(x, date.DayOfWeek)).GroupBy(x => x.WorkTemplateId).Select(x => x.First()))
                    selections.Add((item.WorkTemplate, null, item.Priority, "Official"));
            }
            if (selections.Count == 0) return plan == null || !plan.IsManual ? null : MapToDto(plan);
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
                if (selection.Purpose == "Duty" && plan.WorkAssignments.Any(x => x.WorkTemplateId == selection.Template.WorkTemplateId &&
                    x.SchedulePurpose == "Duty" && x.CancellationReason != null && x.CancellationReason.StartsWith(HRM.WorkPlanning.Services.DutyRosterService.MovementMarker))) continue;
                if (plan.WorkAssignments.Any(x => x.IsValid && x.WorkTemplateId == selection.Template.WorkTemplateId && x.SchedulePurpose == selection.Purpose)) continue;
                AddTemplate(plan, selection.Template, selection.Start, selection.Priority, state, now, workDate, selection.Purpose);
            }
            var ids = plan.WorkAssignments.Where(x => x.IsValid).Select(x => x.WorkTemplateId).Distinct().ToList();
            plan.WorkTemplateId = ids.Count == 1 ? ids[0] : null;
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return MapToDto(plan);
        }

        private static bool AppliesOnDay(JobWorkTemplate x, DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => x.Monday, DayOfWeek.Tuesday => x.Tuesday, DayOfWeek.Wednesday => x.Wednesday,
            DayOfWeek.Thursday => x.Thursday, DayOfWeek.Friday => x.Friday, DayOfWeek.Saturday => x.Saturday,
            DayOfWeek.Sunday => x.Sunday, _ => false
        };

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
