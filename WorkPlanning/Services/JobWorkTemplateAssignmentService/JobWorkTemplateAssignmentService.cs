using HRM.Components.Shared;
using HRM.DTOs.Job.JobWorkTemplateAssignment;
using HRM.DTOs.StaffSchedule;
using HRM.DTOs;
using HRM.Models;
using HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment;
using Microsoft.EntityFrameworkCore;
using HRM.Models.WorkPlanning;
using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Services.JobWorkTemplateAssignmentService
{
    public class JobWorkTemplateAssignmentService : IJobWorkTemplateAssignmentService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly ILogger<JobWorkTemplateAssignmentService> _logger;



        public JobWorkTemplateAssignmentService(
            IDbContextFactory<HrmTeContext> dbFactory,
            ILogger<JobWorkTemplateAssignmentService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }


        public async Task<CurrentWorkTemplateAssignmentDto?> GetCurrentAsync(
       int jobId,
       DateTime effectiveDate,
       CancellationToken cancellationToken = default)
        {
            if (jobId <= 0)
            {
                return null;
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var date =
                effectiveDate.Date;

            return await db.JobWorkTemplateAssignments
                .AsNoTracking()
                .Where(x =>
                    x.JobID == jobId &&
                    x.IsActive &&
                    x.EffectiveFrom <= date &&
                    (
                        x.EffectiveTo == null ||
                        x.EffectiveTo >= date
                    ) &&
                    x.WorkTemplate.IsActive)
                .OrderByDescending(x =>
                    x.EffectiveFrom)
                .ThenByDescending(x =>
                    x.JobWorkTemplateAssignmentID)
                .Select(x =>
                    new CurrentWorkTemplateAssignmentDto
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
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        public async Task<List<JobWorkTemplateAssignmentHistoryDto>>
            GetHistoryAsync(
                int jobId,
                CancellationToken cancellationToken = default)
        {
            if (jobId <= 0)
            {
                return [];
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var today =
                DateTime.Today;

            return await db.JobWorkTemplateAssignments
                .AsNoTracking()
                .Where(x =>
                    x.JobID == jobId)
                .OrderByDescending(x =>
                    x.EffectiveFrom)
                .ThenByDescending(x =>
                    x.JobWorkTemplateAssignmentID)
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
                            x.EffectiveFrom <= today &&
                            (
                                x.EffectiveTo == null ||
                                x.EffectiveTo >= today
                            )
                    })
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<List<WorkTemplateLookupDto>>
            SearchTemplatesAsync(
                int organisationBusinessEntityId,
                string? searchText,
                CancellationToken cancellationToken = default)
        {
            if (organisationBusinessEntityId <= 0)
            {
                return [];
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var query =
                db.WorkTemplates
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        (
                            x.IsGlobal ||
                            x.OrganisationBusinessEntityId ==
                                organisationBusinessEntityId
                        ));

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var term =
                    searchText.Trim();

                query = query.Where(x =>
                    x.Name.Contains(term) ||
                    (
                        x.Code != null &&
                        x.Code.Contains(term)
                    ) ||
                    (
                        x.Description != null &&
                        x.Description.Contains(term)
                    ));
            }

            return await query
                .OrderBy(x =>
                    x.Name)
                .Select(x =>
                    new WorkTemplateLookupDto
                    {
                        WorkTemplateId = x.WorkTemplateId,

                        Name = x.Name,

                        Code = x.Code,

                        TemplateTypeName = x.WorkTemplateType.Name,

                        DefaultStartTime = x.DefaultStartTime,

                        DefaultEndTime = x.DefaultEndTime,

                        EndsNextDay = x.EndsNextDay
                    })
                .Take(50)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<ServiceResult> AssignAsync(
            AssignJobWorkTemplateDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto is null)
            {
                return ServiceResult.Failed(
                    "The template-assignment request is required.");
            }

            var validationMessage =
                ValidateAssignmentRequest(dto);

            if (validationMessage is not null)
            {
                return ServiceResult.Failed(
                    validationMessage);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var effectiveFrom =
                dto.EffectiveFrom.Date;

            var effectiveTo =
                dto.EffectiveTo?.Date;

            /*
             * Validate the employee's active job and organisation.
             */
            var job = await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.JobId == dto.JobId &&
                    x.IndividualID == dto.IndividualId &&
                    x.OrganisationID ==
                        dto.OrganisationBusinessEntityId &&
                    x.JobStateId ==
                        SharedConfig.JobStates.APPROVED &&
                    x.TerminatedDate == null)
                .Select(x => new
                {
                    x.JobId,
                    x.IndividualID,
                    x.OrganisationID
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

            if (job is null)
            {
                return ServiceResult.Failed(
                    "The selected staff member does not have the specified active job.");
            }

            /*
             * Global templates may be used by any organisation.
             * Tenant templates must belong to this organisation.
             */
            var template = await db.WorkTemplates
                .AsNoTracking()
                .Where(x =>
                    x.WorkTemplateId ==
                        dto.WorkTemplateId &&
                    x.IsActive &&
                    (
                        x.IsGlobal ||
                        x.OrganisationBusinessEntityId ==
                            dto.OrganisationBusinessEntityId
                    ))
                .Select(x => new
                {
                    x.WorkTemplateId,
                    x.Name,
                    x.EffectiveFrom,
                    x.EffectiveTo
                })
                .SingleOrDefaultAsync(
                    cancellationToken);

            if (template is null)
            {
                return ServiceResult.Failed(
                    "The selected work template was not found, is inactive, or belongs to another organisation.");
            }

            if (template.EffectiveFrom.HasValue &&
                effectiveFrom <
                template.EffectiveFrom.Value.Date)
            {
                return ServiceResult.Failed(
                    $"The work template becomes effective on " +
                    $"{template.EffectiveFrom.Value:dd MMM yyyy}.");
            }

            if (template.EffectiveTo.HasValue &&
                effectiveFrom >
                template.EffectiveTo.Value.Date)
            {
                return ServiceResult.Failed(
                    "The selected effective date is after the work template's expiry date.");
            }

            if (effectiveTo.HasValue &&
                template.EffectiveTo.HasValue &&
                effectiveTo.Value >
                template.EffectiveTo.Value.Date)
            {
                return ServiceResult.Failed(
                    "The assignment cannot end after the work template's expiry date.");
            }

            /*
             * Prevent duplicate assignment of the same template
             * with the same effective start date.
             */
            var duplicateExists =
                    await db.JobWorkTemplateAssignments
                        .AsNoTracking()
                        .Where(x =>
                            x.JobID == dto.JobId &&
                            x.WorkTemplateID == dto.WorkTemplateId &&
                            x.IsActive &&
                            x.EffectiveFrom >= effectiveFrom &&
                            x.EffectiveFrom < effectiveFrom.AddDays(1))
                        .Select(x => new
                        {
                            x.JobWorkTemplateAssignmentID,
                            x.EffectiveFrom,
                            x.EffectiveTo
                        })
                        .FirstOrDefaultAsync(cancellationToken);

            if (duplicateExists is not null)
            {
                var periodText =
                    duplicateExists.EffectiveTo.HasValue
                        ? $"{duplicateExists.EffectiveFrom:dd MMM yyyy} – " +
                          $"{duplicateExists.EffectiveTo.Value:dd MMM yyyy}"
                        : $"{duplicateExists.EffectiveFrom:dd MMM yyyy} onwards";

                return ServiceResult.Failed(
                    $"This template already has assignment ID " +
                    $"{duplicateExists.JobWorkTemplateAssignmentID} " +
                    $"for the period {periodText}.");
            }

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                /*
                 * Find an assignment that overlaps the new effective date.
                 */
                var currentAssignment =
                    await db.JobWorkTemplateAssignments
                        .Where(x =>
                            x.JobID == dto.JobId &&
                            x.IsActive &&
                            x.EffectiveFrom <= effectiveFrom &&
                            (
                                x.EffectiveTo == null ||
                                x.EffectiveTo >= effectiveFrom
                            ))
                        .OrderByDescending(x =>
                            x.EffectiveFrom)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                if (currentAssignment is not null)
                {
                    /*
                     * If the new assignment begins after the old assignment,
                     * retain the old row as historical data.
                     */
                    if (effectiveFrom >
                        currentAssignment.EffectiveFrom.Date)
                    {
                        currentAssignment.EffectiveTo =
                            effectiveFrom.AddDays(-1);

                        /*
                         * Leave IsActive = true because this remains a valid
                         * historical assignment.
                         */
                    }
                    else
                    {
                        /*
                         * The replacement begins on or before the old start,
                         * so the old record is no longer a valid assignment.
                         */
                        currentAssignment.IsActive =
                            false;

                        currentAssignment.EffectiveTo =
                            effectiveFrom.AddDays(-1);
                    }
                }

                /*
                 * Close future assignments that would overlap the new range.
                 */
                var futureOverlaps =
                    await db.JobWorkTemplateAssignments
                        .Where(x =>
                            x.JobID == dto.JobId &&
                            x.IsActive &&
                            x.EffectiveFrom > effectiveFrom &&
                            (
                                !effectiveTo.HasValue ||
                                x.EffectiveFrom <=
                                    effectiveTo.Value
                            ))
                        .ToListAsync(
                            cancellationToken);

                foreach (var futureAssignment in
                         futureOverlaps)
                {
                    futureAssignment.IsActive =
                        false;
                }

                var assignment = new JobWorkTemplateAssignment
                {
                        JobID =
                            dto.JobId,

                        WorkTemplateID =
                            dto.WorkTemplateId,

                        EffectiveFrom =
                            effectiveFrom,

                        EffectiveTo =
                            effectiveTo,

                        IsActive =
                            true,

                        CreatedDate =
                            DateTime.UtcNow,

                        ScheduledStartTime = null,
                };

                db.JobWorkTemplateAssignments.Add(
                    assignment);

                await db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    """
                Work template assigned to job.
                JobId: {JobId}
                IndividualId: {IndividualId}
                WorkTemplateId: {WorkTemplateId}
                EffectiveFrom: {EffectiveFrom}
                EffectiveTo: {EffectiveTo}
                """,
                    dto.JobId,
                    dto.IndividualId,
                    dto.WorkTemplateId,
                    effectiveFrom,
                    effectiveTo);

                return ServiceResult.Ok(
                    $"Work template '{template.Name}' was assigned successfully.");
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                _logger.LogError(
                    exception,
                    """
                Failed to assign a work template to a job.
                JobId: {JobId}
                IndividualId: {IndividualId}
                WorkTemplateId: {WorkTemplateId}
                """,
                    dto.JobId,
                    dto.IndividualId,
                    dto.WorkTemplateId);

                return ServiceResult.Failed(
                    "The work template could not be assigned.");
            }
        }

        public async Task<ServiceResult> RemoveAsync(
            RemoveJobWorkTemplateDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto is null ||
                dto.JobWorkTemplateAssignmentId <= 0 ||
                dto.JobId <= 0)
            {
                return ServiceResult.Failed(
                    "A valid template assignment is required.");
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var assignment =
                await db.JobWorkTemplateAssignments
                    .SingleOrDefaultAsync(
                        x =>
                            x.JobWorkTemplateAssignmentID ==
                                dto.JobWorkTemplateAssignmentId &&
                            x.JobID == dto.JobId &&
                            x.IsActive,
                        cancellationToken);

            if (assignment is null)
            {
                return ServiceResult.Failed(
                    "The active work-template assignment was not found.");
            }

            var removalDate =
                dto.EffectiveDate.Date;

            if (removalDate <
                assignment.EffectiveFrom.Date)
            {
                return ServiceResult.Failed(
                    "The removal date cannot be before the assignment's effective date.");
            }

            if (removalDate ==
                assignment.EffectiveFrom.Date)
            {
                /*
                 * The assignment never becomes effective.
                 */
                assignment.IsActive =
                    false;

                assignment.EffectiveTo =
                    removalDate.AddDays(-1);
            }
            else
            {
                /*
                 * Preserve the assignment as valid history up to
                 * the day before removal.
                 */
                assignment.EffectiveTo =
                    removalDate.AddDays(-1);
            }

            await db.SaveChangesAsync(
                cancellationToken);

            return ServiceResult.Ok(
                "The work-template assignment was removed successfully.");
        }

        private static string? ValidateAssignmentRequest(
            AssignJobWorkTemplateDto dto)
        {
            if (dto.JobId <= 0)
            {
                return "A valid job is required.";
            }

            if (dto.IndividualId <= 0)
            {
                return "A valid staff member is required.";
            }

            if (dto.OrganisationBusinessEntityId <= 0)
            {
                return "A valid organisation is required.";
            }

            if (dto.WorkTemplateId <= 0)
            {
                return "A valid work template is required.";
            }

            if (dto.EffectiveFrom == default)
            {
                return "An effective-from date is required.";
            }

            if (dto.EffectiveTo.HasValue &&
                dto.EffectiveTo.Value.Date <
                dto.EffectiveFrom.Date)
            {
                return "The effective-to date cannot be before the effective-from date.";
            }

            return null;
        }




    }
}
