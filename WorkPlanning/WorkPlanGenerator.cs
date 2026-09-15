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
            int individualId,
            int jobId,
            int organisationBusinessEntityId,
            DateOnly workDate,
            CancellationToken cancellationToken = default)
      {
            var provider = await _providerResolver.ResolveAsync(
                organisationBusinessEntityId,
                workDate,
                cancellationToken);

            if (!provider.UsesWorkPlanning)
            {
                return null;
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            var workDateTime = workDate.ToDateTime(TimeOnly.MinValue);

            // A manually created plan always takes precedence.
   

            var nextWorkDate =
                workDateTime.AddDays(1);

            var existingPlan = await db.WorkPlans
                .Include(x => x.WorkPlanSegments)
                .Include(x => x.WorkAssignments)
                .Where(x =>
                    x.JobId == jobId &&
                    x.WorkDate >= workDateTime &&
                    x.WorkDate < nextWorkDate &&
                    x.IsValid)
                .OrderByDescending(x => x.IsManual)
                .ThenByDescending(x => x.Version)
                .ThenByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);



            //var nextWorkDate = workDateTime.AddDays(1);
            //var existingPlan = await db.WorkPlans
            // .Include(x => x.WorkPlanSegments)
            // .Include(x => x.WorkAssignments)
            // .Where(x =>
            //     x.JobId == jobId &&
            //     x.WorkDate >= workDateTime &&
            //     x.WorkDate < nextWorkDate)
            // .FirstOrDefaultAsync(cancellationToken);
            if (existingPlan != null)
            {
                if (existingPlan.IndividualId != individualId)
                {
                    throw new InvalidOperationException(
                        $"WorkPlan {existingPlan.WorkPlanId} belongs to " +
                        $"Individual {existingPlan.IndividualId}, but Job {jobId} " +
                        $"was requested for Individual {individualId}.");
                }

                if (existingPlan.OrganisationBusinessEntityId !=
                    organisationBusinessEntityId)
                {
                    throw new InvalidOperationException(
                        $"WorkPlan {existingPlan.WorkPlanId} belongs to Organisation " +
                        $"{existingPlan.OrganisationBusinessEntityId}, but Organisation " +
                        $"{organisationBusinessEntityId} was requested.");
                }

                return MapToDto(existingPlan);
            }
            //if (existingPlan != null)
            //{
            //    return MapToDto(existingPlan);
            //}




            //if (existingPlan != null)
            //{
            //    return MapToDto(existingPlan);
            //}

            var assignmentsQuery = db.JobWorkTemplates
                 .AsNoTracking()
                 .Include(x => x.WorkTemplate)
                     .ThenInclude(x => x.WorkTemplateSegments)
                 .Where(x =>
                     x.JobId == jobId &&
                     x.IsActive &&
                     x.EffectiveFrom.Date <= workDateTime.Date &&
                     (
                         !x.EffectiveTo.HasValue ||
                         x.EffectiveTo.Value.Date >= workDateTime.Date
                     ) &&
                     x.WorkTemplate.IsActive);

            assignmentsQuery = workDateTime.DayOfWeek switch
            {
                DayOfWeek.Monday => assignmentsQuery.Where(x => x.Monday),
                DayOfWeek.Tuesday => assignmentsQuery.Where(x => x.Tuesday),
                DayOfWeek.Wednesday => assignmentsQuery.Where(x => x.Wednesday),
                DayOfWeek.Thursday => assignmentsQuery.Where(x => x.Thursday),
                DayOfWeek.Friday => assignmentsQuery.Where(x => x.Friday),
                DayOfWeek.Saturday => assignmentsQuery.Where(x => x.Saturday),
                DayOfWeek.Sunday => assignmentsQuery.Where(x => x.Sunday),
                _ => assignmentsQuery.Where(x => false)
            };

            var assignment = await assignmentsQuery
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            // No assignment normally means a non-working day.
            if (assignment == null)
            {
                return null;

                //throw new InvalidOperationException(
                //  $"No JobWorkTemplate found. " +
                //  $"JobId={jobId}, " +
                //  $"OrganisationId={organisationBusinessEntityId}, " +
                //  $"WorkDate={workDateTime:yyyy-MM-dd}, " +
                //  $"Day={workDateTime.DayOfWeek}.");
            }

            var template = assignment.WorkTemplate;

            if (!template.DefaultStartTime.HasValue)
            {
                throw new InvalidOperationException(
                    $"Work template '{template.Name}' has no DefaultStartTime.");
            }

            var templateSegments = template.WorkTemplateSegments
                .Where(x => x.IsActive)
                .OrderBy(x => x.SequenceNumber)
                .ToList();

            if (templateSegments.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Work template '{template.Name}' has no active segments.");
            }

            var plannedStateId = await db.WorkAssignmentStates
                .Where(x =>
                    x.IsActive &&
                    x.Code == PlannedAssignmentStateCode)
                .Select(x => (int?)x.WorkAssignmentStateId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!plannedStateId.HasValue)
            {
                throw new InvalidOperationException(
                    $"An active work-assignment state with code " +
                    $"'{PlannedAssignmentStateCode}' was not found.");
            }

            var generatedAt = DateTime.UtcNow;

            var plan = new WorkPlan
            {
                IndividualId = individualId,
                JobId = jobId,
                OrganisationBusinessEntityId = organisationBusinessEntityId,
                PlanningProviderId = provider.PlanningProviderId,
                WorkTemplateId = template.WorkTemplateId,
                WorkDate = workDateTime,

                GenerationSource = WorkPlanGenerationSource.Template,
                GeneratedDate = generatedAt,
                GeneratedByUserId = null,

                IsFinalized = false,
                IsValid = true,
                CreatedDate = generatedAt,
                PlanGuid = Guid.NewGuid(),
                Version = 1,

                IsGenerated = true,
                IsManual = false
            };

            var planStart = workDate.ToDateTime(template.DefaultStartTime.Value);

            foreach (var templateSegment in templateSegments)
            {
                var segmentStart = planStart.AddMinutes(
                    templateSegment.OffsetMinutes);

                var segmentEnd = segmentStart.AddMinutes(
                    templateSegment.DurationMinutes);

                plan.WorkPlanSegments.Add(new WorkPlanSegment
                {
                    WorkTemplateSegmentId =
                        templateSegment.WorkTemplateSegmentId,

                    WorkSegmentTypeId =
                        templateSegment.WorkSegmentTypeId,

                    Name = templateSegment.Name,
                    Description = templateSegment.Description,
                    SequenceNumber = templateSegment.SequenceNumber,

                    StartDateTime = segmentStart,
                    EndDateTime = segmentEnd,

                    GraceBeforeMinutes =
                        templateSegment.GraceBeforeMinutes,

                    GraceAfterMinutes =
                        templateSegment.GraceAfterMinutes,

                    IsMandatory = templateSegment.IsMandatory,
                    RequiresAttendance =
                        templateSegment.RequiresAttendance,

                    RequiresLocationValidation =
                        templateSegment.RequiresLocationValidation,

                    RequiresDeviceValidation =
                        templateSegment.RequiresDeviceValidation,

                    IsPaid = templateSegment.IsPaid,
                    IsCompleted = false,
                    IsValid = true,

                    // Reuse the source segment audit reference.
                    OperationLogId = templateSegment.OperationLogId,

                    CreatedDate = generatedAt
                });
            }

            var assignmentStart = plan.WorkPlanSegments
                .Min(x => x.StartDateTime);

            var assignmentEnd = plan.WorkPlanSegments
                .Max(x => x.EndDateTime);

            //plan.WorkAssignments.Add(new WorkAssignment
            //{
            //    WorkTemplateId = template.WorkTemplateId,
            //    WorkTemplateTypeId = template.WorkTemplateTypeId,
            //    WorkAssignmentStateId = plannedStateId.Value,

            //    Name = template.Name,
            //    Code = template.Code,
            //    Description = template.Description,

            //    StartDateTime = assignmentStart,
            //    EndDateTime = assignmentEnd,

            //    GraceMinutes = template.DefaultGraceMinutes,
            //    RequiresAttendance = template.RequiresAttendance,
            //    RequiresCheckOut = template.RequiresCheckOut,

            //    Priority = assignment.Priority,
            //    AssignmentSource = WorkAssignmentSource.Template,

            //    IsValid = true,
            //    CreatedDate = generatedAt
            //});
            var workAssignment = new WorkAssignment
            {
                WorkTemplateId = template.WorkTemplateId,
                WorkTemplateTypeId = template.WorkTemplateTypeId,
                WorkAssignmentStateId = plannedStateId.Value,

                Name = template.Name,
                Code = template.Code,
                Description = template.Description,

                StartDateTime = assignmentStart,
                EndDateTime = assignmentEnd,

                GraceMinutes = template.DefaultGraceMinutes,
                RequiresAttendance = template.RequiresAttendance,
                RequiresCheckOut = template.RequiresCheckOut,

                Priority = assignment.Priority,
                AssignmentSource = WorkAssignmentSource.Template,

                IsValid = true,
                CreatedDate = generatedAt
            };

            workAssignment.WorkAssignmentOwners.Add(
               new WorkAssignmentOwner
               {
                   IndividualId = individualId,
                   JobId = jobId,

                   OwnershipType = WorkOwnershipType.Original,

                   AssignedDate = generatedAt,

                   EffectiveFrom = assignmentStart,

                   // Must be NULL while this is the current owner.
                   EffectiveTo = null,

                   RelievedDate = null,
                   RelievedByUserId = null,
                   ReliefReason = null,

                   IsCurrentOwner = true,
                   IsValid = true
               });

            plan.WorkAssignments.Add(workAssignment);




            db.WorkPlans.Add(plan);

            await db.SaveChangesAsync(cancellationToken);

            return MapToDto(plan);
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

                Segments = plan.WorkPlanSegments
                    .OrderBy(x => x.SequenceNumber)
                    .Select(x => new AttendanceWorkSegmentDto
                    {
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

                        RequiresLocationValidation =
                            x.RequiresLocationValidation,

                        RequiresDeviceValidation =
                            x.RequiresDeviceValidation,

                        IsPaid = x.IsPaid,
                        IsCompleted = x.IsCompleted,
                        AttendanceId = x.AttendanceId
                    })
                    .ToList()
            };
        }
    }
}
