using HRM.Components.Shared;
using HRM.DTOs.StaffSchedule;
using HRM.Models;
using HRM.WorkPlanning.Abstractions.Schedules;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services.Schedules
{
    public sealed class StaffScheduleService : IStaffScheduleService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public StaffScheduleService(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<StaffScheduleResult> GetScheduleAsync(
        int individualId,
        CancellationToken cancellationToken = default)
        {
            if (individualId <= 0)
            {
                return StaffScheduleResult.Failure(
                    "A valid staff ID is required.");
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var today = DateTime.Today;

            var activeJob = await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.IndividualID == individualId &&
                    x.JobStateId ==
                        SharedConfig.JobStates.APPROVED &&
                    x.TerminatedDate == null)
                .OrderByDescending(x =>
                    x.JoinedDate)
                .Select(x => new
                {
                    x.JobId,
                    IndividualId =
                        x.IndividualID,
                    OrganisationId =
                        x.OrganisationID,

                    EmployeeName =
                        (
                            (x.Individual.FirstNameEnglish ?? string.Empty) +
                            " " +
                            (x.Individual.MiddleNameEnglish ?? string.Empty) +
                            " " +
                            (x.Individual.LastNameEnglish ?? string.Empty)
                        ).Trim(),

                    OrganisationName =
                        x.Organisation.OrganisationName,

                    PositionName =
                        x.JobPositions
                            .Where(position =>
                                position.FromDate <= today &&
                                (
                                    position.ToDate == null ||
                                    position.ToDate >= today
                                ))
                            .OrderByDescending(position =>
                                position.FromDate)
                            .Select(position =>
                                position.Position.Name)
                            .FirstOrDefault()
                })
                .FirstOrDefaultAsync(
                    cancellationToken);

            if (activeJob is null)
            {
                return StaffScheduleResult.Failure(
                    "This staff member does not have an active job.");
            }

            var schedule = new StaffScheduleDto
            {
                IndividualId =
                    activeJob.IndividualId,

                JobId =
                    activeJob.JobId,

                OrganisationBusinessEntityId =
                    activeJob.OrganisationId,

                EmployeeName =
                    activeJob.EmployeeName,

                PositionName =
                    activeJob.PositionName ??
                    "No active position",

                OrganisationName =
                    activeJob.OrganisationName ??
                    string.Empty,

                HasActiveJob =
                    true
            };

            schedule.CurrentTemplate =
                await GetCurrentTemplateAsync(
                    db,
                    activeJob.JobId,
                    today,
                    cancellationToken);

            schedule.TemplateHistory =
                await GetTemplateHistoryAsync(
                    db,
                    activeJob.JobId,
                    today,
                    cancellationToken);

            schedule.CurrentShift =
                await GetCurrentShiftAsync(
                    db,
                    activeJob.JobId,
                    today,
                    cancellationToken);

            schedule.ShiftHistory =
                await GetShiftHistoryAsync(
                    db,
                    activeJob.JobId,
                    today,
                    cancellationToken);

            schedule.ManualAssignments =
                await GetManualAssignmentsAsync(
                    db,
                    activeJob.IndividualId,
                    activeJob.JobId,
                    cancellationToken);

            return StaffScheduleResult.Ok(schedule);
        }


        private static async Task<CurrentWorkTemplateAssignmentDto?>
            GetCurrentTemplateAsync(
                HrmTeContext db,
                int jobId,
                DateTime effectiveDate,
                CancellationToken cancellationToken)
        {
            var date = effectiveDate.Date;
            var nextDate = date.AddDays(1);

            return await db.JobWorkTemplateAssignments
                .AsNoTracking()
                .Where(x =>
                    x.JobID == jobId &&
                    x.IsActive &&
                    x.EffectiveFrom < nextDate &&
                    (
                        x.EffectiveTo == null ||
                        x.EffectiveTo >= date
                    ) &&
                    x.WorkTemplate.IsActive)
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x =>
                    x.JobWorkTemplateAssignmentID)
                .Select(x => new CurrentWorkTemplateAssignmentDto
                {
                    JobWorkTemplateAssignmentId =
                        x.JobWorkTemplateAssignmentID,

                    JobId =
                        x.JobID,

                    WorkTemplateId =
                        x.WorkTemplateID,

                    TemplateName =
                        x.WorkTemplate.Name,

                    TemplateCode =
                        x.WorkTemplate.Code,

                    TemplateTypeName =
                        x.WorkTemplate.WorkTemplateType.Name,

                    EffectiveFrom =
                        x.EffectiveFrom,

                    EffectiveTo =
                        x.EffectiveTo,

                    IsActive =
                        x.IsActive,

                    DefaultStartTime = x.WorkTemplate.DefaultStartTime.HasValue
                    ? DateTime.MinValue.Add(x.WorkTemplate.DefaultStartTime.Value.ToTimeSpan())
                    : null,

                    DefaultEndTime = x.WorkTemplate.DefaultEndTime.HasValue
                    ? DateTime.MinValue.Add(x.WorkTemplate.DefaultEndTime.Value.ToTimeSpan())
                    : null
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static async Task<List<JobWorkTemplateAssignmentHistoryDto>>
            GetTemplateHistoryAsync(
                HrmTeContext db,
                int jobId,
                DateTime effectiveDate,
                CancellationToken cancellationToken)
        {
            return await db.JobWorkTemplateAssignments
                .AsNoTracking()
                .Where(x =>
                    x.JobID == jobId)
                .OrderByDescending(x =>
                    x.EffectiveFrom)
                .Select(x =>
                    new JobWorkTemplateAssignmentHistoryDto
                    {
                        JobWorkTemplateAssignmentId =
                            x.JobWorkTemplateAssignmentID,

                        WorkTemplateId =
                            x.WorkTemplateID,

                        TemplateName =
                            x.WorkTemplate.Name,

                        EffectiveFrom =
                            x.EffectiveFrom,

                        EffectiveTo =
                            x.EffectiveTo,

                        IsActive =
                            x.IsActive,

                        IsCurrent =
                            x.IsActive &&
                            x.EffectiveFrom <= effectiveDate &&
                            (
                                x.EffectiveTo == null ||
                                x.EffectiveTo >= effectiveDate
                            )
                    })
                .ToListAsync(cancellationToken);
        }

        private static async Task<List<ManualWorkAssignmentListDto>>
            GetManualAssignmentsAsync(
                HrmTeContext db,
                int individualId,
                int jobId,
                CancellationToken cancellationToken)
        {
            var fromDate =
                DateTime.Today.AddDays(-30);

            var toDate =
                DateTime.Today.AddDays(90);

            return await db.WorkAssignments
                .AsNoTracking()
                .Where(x =>
                    x.IsValid &&
                    x.WorkPlan.IndividualId == individualId &&
                    x.WorkPlan.JobId == jobId &&
                    x.WorkPlan.IsManual &&
                    x.WorkPlan.WorkDate >= fromDate &&
                    x.WorkPlan.WorkDate <= toDate)
                .OrderBy(x =>
                    x.StartDateTime)
                .Select(x =>
                    new ManualWorkAssignmentListDto
                    {
                        WorkPlanId =
                            x.WorkPlanId,

                        WorkAssignmentId =
                            x.WorkAssignmentId,

                        WorkDate =
                            x.WorkPlan.WorkDate,

                        AssignmentName =
                            x.Name,

                        WorkTemplateName =
                            x.WorkTemplate != null
                                ? x.WorkTemplate.Name
                                : "Manual Assignment",

                        StartDateTime =
                            x.StartDateTime,

                        EndDateTime =
                            x.EndDateTime,

                        IsFinalized =
                            x.WorkPlan.IsFinalized,

                        IsValid =
                            x.IsValid,

                        Remarks =
                            x.WorkPlan.Remarks
                    })
                .ToListAsync(cancellationToken);
        }

        private static Task<CurrentShiftAssignmentDto?>
            GetCurrentShiftAsync(
                HrmTeContext db,
                int jobId,
                DateTime effectiveDate,
                CancellationToken cancellationToken)
        {
            /*
             * Replace this with the existing legacy shift assignment query.
             */
            return Task.FromResult<CurrentShiftAssignmentDto?>(
                null);
        }

        private static Task<List<ShiftAssignmentHistoryDto>>
            GetShiftHistoryAsync(
                HrmTeContext db,
                int jobId,
                DateTime effectiveDate,
                CancellationToken cancellationToken)
        {
            /*
             * Replace this with the existing legacy shift history query.
             */
            return Task.FromResult(
                new List<ShiftAssignmentHistoryDto>());
        }
    }
}
