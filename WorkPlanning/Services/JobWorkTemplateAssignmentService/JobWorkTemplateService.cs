using HRM.DTOs.Job.JobWorkTemplateAssignment;
using HRM.DTOs.Job;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment;
using Microsoft.EntityFrameworkCore;
using HRM.DTOs;

namespace HRM.WorkPlanning.Services.JobWorkTemplateAssignmentService
{
    public class JobWorkTemplateService : IJobWorkTemplateService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;

        public JobWorkTemplateService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IUserAccessService userAccessService)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
        }

        public async Task<ServiceResult> AssignAsync(
            AssignJobWorkTemplateDto dto)
        {
            if (dto.JobId <= 0)
            {
                return ServiceResult.Failed("A valid job is required.");
            }

            if (dto.WorkTemplateId <= 0)
            {
                return ServiceResult.Failed("Please select a work template.");
            }

            if (dto.EffectiveTo.HasValue &&
                dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date)
            {
                return ServiceResult.Failed(
                    "Effective To date cannot be before Effective From date.");
            }

            var hasAtLeastOneDay =
                dto.Monday ||
                dto.Tuesday ||
                dto.Wednesday ||
                dto.Thursday ||
                dto.Friday ||
                dto.Saturday ||
                dto.Sunday;

            if (!hasAtLeastOneDay)
            {
                return ServiceResult.Failed(
                    "Select at least one working day.");
            }

            var context = await _userAccessService.RequireContextAsync();

            var tenantOrganisationId = context.ActiveJob?.OrganisationId;

            if (!tenantOrganisationId.HasValue)
            {
                return ServiceResult.Failed(
                    "Your current organisation could not be determined.");
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var job = await db.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.JobId == dto.JobId &&
                    x.OrganisationID == tenantOrganisationId.Value);

            if (job == null)
            {
                return ServiceResult.Failed(
                    "The selected job was not found in your organisation.");
            }

            var template = await db.WorkTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.WorkTemplateId == dto.WorkTemplateId &&
                    x.IsActive &&
                    (
                        x.IsGlobal ||
                        x.OrganisationBusinessEntityId == tenantOrganisationId.Value
                    ));

            if (template == null)
            {
                return ServiceResult.Failed(
                    "The selected work template is not available.");
            }

            var duplicateExists = await db.JobWorkTemplates
                .AnyAsync(x =>
                    x.JobId == dto.JobId &&
                    x.WorkTemplateId == dto.WorkTemplateId &&
                    x.IsActive &&
                    x.EffectiveFrom == dto.EffectiveFrom.Date &&
                    x.EffectiveTo == dto.EffectiveTo);

            if (duplicateExists)
            {
                return ServiceResult.Failed(
                    "This work template is already assigned for the selected period.");
            }

            var entity = new JobWorkTemplate
            {
                JobId = dto.JobId,
                WorkTemplateId = dto.WorkTemplateId,

                EffectiveFrom = dto.EffectiveFrom.Date,
                EffectiveTo = dto.EffectiveTo?.Date,

                Monday = dto.Monday,
                Tuesday = dto.Tuesday,
                Wednesday = dto.Wednesday,
                Thursday = dto.Thursday,
                Friday = dto.Friday,
                Saturday = dto.Saturday,
                Sunday = dto.Sunday,

                Priority = dto.Priority,
                IsActive = true,
                OperationLogId = dto.OperationLogId
            };

            db.JobWorkTemplates.Add(entity);

            await db.SaveChangesAsync();

            return ServiceResult.Ok(
                "Work template assigned to the job successfully.");
        }

        public async Task<List<JobWorkTemplateDto>> GetByJobAsync(int jobId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.JobWorkTemplates
                .AsNoTracking()
                .Where(x => x.JobId == jobId)
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.EffectiveFrom)
                .Select(x => new JobWorkTemplateDto
                {
                    JobWorkTemplateId = x.JobWorkTemplateId,
                    JobId = x.JobId,
                    WorkTemplateId = x.WorkTemplateId,
                    WorkTemplateName = x.WorkTemplate.Name,
                    WorkTemplateCode = x.WorkTemplate.Code,

                    EffectiveFrom = x.EffectiveFrom,
                    EffectiveTo = x.EffectiveTo,

                    Monday = x.Monday,
                    Tuesday = x.Tuesday,
                    Wednesday = x.Wednesday,
                    Thursday = x.Thursday,
                    Friday = x.Friday,
                    Saturday = x.Saturday,
                    Sunday = x.Sunday,

                    Priority = x.Priority,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<JobWorkTemplate?> GetEffectiveAssignmentAsync(
            int jobId,
            DateTime workDate)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var date = workDate.Date;

            var assignments = await db.JobWorkTemplates
                .AsNoTracking()
                .Include(x => x.WorkTemplate)
                .Where(x =>
                    x.JobId == jobId &&
                    x.IsActive &&
                    x.EffectiveFrom <= date &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= date) &&
                    x.WorkTemplate.IsActive)
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            return assignments.FirstOrDefault(x =>
                AppliesOnDay(x, date.DayOfWeek));
        }

        private static bool AppliesOnDay(
            JobWorkTemplate assignment,
            DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => assignment.Monday,
                DayOfWeek.Tuesday => assignment.Tuesday,
                DayOfWeek.Wednesday => assignment.Wednesday,
                DayOfWeek.Thursday => assignment.Thursday,
                DayOfWeek.Friday => assignment.Friday,
                DayOfWeek.Saturday => assignment.Saturday,
                DayOfWeek.Sunday => assignment.Sunday,
                _ => false
            };
        }

    }
}
