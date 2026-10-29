using HRM.DTOs;
using HRM.DTOs.WorkTypes;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public sealed class WorkTypeService : IWorkTypeService
    {
        private const int NameMaxLength = 100;
        private const int LocationMaxLength = 250;

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _access;
        private readonly ILogger<WorkTypeService> _logger;

        public WorkTypeService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IUserAccessService access,
            ILogger<WorkTypeService> logger)
        {
            _dbFactory = dbFactory;
            _access = access;
            _logger = logger;
        }

        public async Task<List<WorkTypeDto>> GetAllAsync(bool includeInactive = false)
        {
            var scope = await RequireManageAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();

            var query = db.WorkTypes.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(x => x.IsValid);
            }

            return await query
                .OrderBy(x => x.Name)
                .Select(x => new WorkTypeDto
                {
                    WorkTypeId = x.WorkTypeId,
                    Name = x.Name,
                    IsValid = x.IsValid,
                    AssignmentCount = x.AssignedWorkTypes.Count(a => a.IsValid && a.OrganisationId == scope)
                })
                .ToListAsync();
        }

        public async Task<WorkTypeDto?> GetByIdAsync(int workTypeId)
        {
            var scope = await RequireManageAsync();
            if (workTypeId <= 0)
            {
                return null;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.WorkTypes
                .AsNoTracking()
                .Where(x => x.WorkTypeId == workTypeId)
                .Select(x => new WorkTypeDto
                {
                    WorkTypeId = x.WorkTypeId,
                    Name = x.Name,
                    IsValid = x.IsValid,
                    AssignmentCount = x.AssignedWorkTypes.Count(a => a.IsValid && a.OrganisationId == scope)
                })
                .SingleOrDefaultAsync();
        }

        public async Task<ServiceResult> CreateAsync(CreateWorkTypeDto dto)
        {
            await RequireCatalogueAsync();
            var name = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResult.Fail("Work type name is required.");
            }

            if (name.Length > NameMaxLength)
            {
                return ServiceResult.Fail($"Work type name cannot exceed {NameMaxLength} characters.");
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            if (await db.WorkTypes.AnyAsync(x => x.Name == name))
            {
                return ServiceResult.Fail("A work type with this name already exists.");
            }

            // The deployed WorkTypeID column is not an identity column, so the next key is allocated here.
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var nextId = (await db.WorkTypes.MaxAsync(x => (int?)x.WorkTypeId) ?? 0) + 1;

            db.WorkTypes.Add(new WorkType
            {
                WorkTypeId = nextId,
                Name = name,
                IsValid = dto.IsValid
            });

            await db.SaveChangesAsync();

            _logger.LogInformation("Work type {WorkTypeId} ({Name}) was created.", nextId, name);
            await transaction.CommitAsync();

            return ServiceResult.Ok($"Work type '{name}' was created.");
        }

        public async Task<ServiceResult> UpdateAsync(UpdateWorkTypeDto dto)
        {
            await RequireCatalogueAsync();
            var name = dto.Name?.Trim();

            if (dto.WorkTypeId <= 0)
            {
                return ServiceResult.Fail("Select a work type to update.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResult.Fail("Work type name is required.");
            }

            if (name.Length > NameMaxLength)
            {
                return ServiceResult.Fail($"Work type name cannot exceed {NameMaxLength} characters.");
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var entity = await db.WorkTypes.SingleOrDefaultAsync(x => x.WorkTypeId == dto.WorkTypeId);

            if (entity is null)
            {
                return ServiceResult.Fail("Work type was not found.");
            }

            if (await db.WorkTypes.AnyAsync(x => x.WorkTypeId != dto.WorkTypeId && x.Name == name))
            {
                return ServiceResult.Fail("A work type with this name already exists.");
            }

            entity.Name = name;
            entity.IsValid = dto.IsValid;

            await db.SaveChangesAsync();

            return ServiceResult.Ok($"Work type '{name}' was updated.");
        }

        public async Task<ServiceResult> SetActiveAsync(int workTypeId, bool isActive)
        {
            await RequireCatalogueAsync();
            if (workTypeId <= 0)
            {
                return ServiceResult.Fail("Select a work type.");
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var entity = await db.WorkTypes.SingleOrDefaultAsync(x => x.WorkTypeId == workTypeId);

            if (entity is null)
            {
                return ServiceResult.Fail("Work type was not found.");
            }

            if (!isActive && await db.AssignedWorkTypes.AnyAsync(x => x.WorkTypeId == workTypeId && x.IsValid))
            {
                return ServiceResult.Fail("This work type is still assigned to staff. Remove those assignments before deactivating it.");
            }

            entity.IsValid = isActive;

            await db.SaveChangesAsync();

            return ServiceResult.Ok(isActive
                ? $"Work type '{entity.Name}' was activated."
                : $"Work type '{entity.Name}' was deactivated.");
        }

        public async Task<List<AssignedWorkTypeDto>> GetAssignmentsAsync(
            int? jobId = null,
            int? organisationId = null,
            bool includeInactive = false)
        {
            var scope = await ResolveOrganisationScopeAsync(organisationId);

            await using var db = await _dbFactory.CreateDbContextAsync();

            var query = db.AssignedWorkTypes
                .AsNoTracking()
                .Where(x => x.OrganisationId == scope);

            if (jobId is > 0)
            {
                query = query.Where(x => x.JobId == jobId.Value);
            }

            if (!includeInactive)
            {
                query = query.Where(x => x.IsValid);
            }

            return await query
                .OrderByDescending(x => x.FromDate)
                .ThenBy(x => x.WorkType.Name)
                .Select(x => new AssignedWorkTypeDto
                {
                    AssignedWorkTypeId = x.AssignedWorkTypeId,
                    WorkTypeId = x.WorkTypeId,
                    WorkTypeName = x.WorkType.Name,
                    JobId = x.JobId,
                    EmployeeName = x.Job.Individual.FirstNameEnglish + " " + x.Job.Individual.LastNameEnglish,
                    OrganisationId = x.OrganisationId,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    Location = x.Location,
                    IsValid = x.IsValid,
                    AssignedByUserId = x.AssignedByUserId,
                    CreatedDate = x.CreatedDate
                })
                .ToListAsync();
        }

        public async Task<AssignedWorkTypeDto?> GetAssignmentByIdAsync(int assignedWorkTypeId)
        {
            if (assignedWorkTypeId <= 0)
            {
                return null;
            }

            var scope = await ResolveOrganisationScopeAsync(null);

            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.AssignedWorkTypes
                .AsNoTracking()
                .Where(x => x.AssignedWorkTypeId == assignedWorkTypeId && x.OrganisationId == scope)
                .Select(x => new AssignedWorkTypeDto
                {
                    AssignedWorkTypeId = x.AssignedWorkTypeId,
                    WorkTypeId = x.WorkTypeId,
                    WorkTypeName = x.WorkType.Name,
                    JobId = x.JobId,
                    EmployeeName = x.Job.Individual.FirstNameEnglish + " " + x.Job.Individual.LastNameEnglish,
                    OrganisationId = x.OrganisationId,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    Location = x.Location,
                    IsValid = x.IsValid,
                    AssignedByUserId = x.AssignedByUserId,
                    CreatedDate = x.CreatedDate
                })
                .SingleOrDefaultAsync();
        }

        public async Task<ServiceResult> AssignAsync(CreateAssignedWorkTypeDto dto)
        {
            var scope = await ResolveOrganisationScopeAsync(null);

            var validation = ValidatePeriod(dto.FromDate, dto.ToDate, dto.Location, out var location);

            if (validation is not null)
            {
                return validation;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var workType = await db.WorkTypes
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.WorkTypeId == dto.WorkTypeId);

            if (workType is null || !workType.IsValid)
            {
                return ServiceResult.Fail("Select an active work type.");
            }

            var job = await db.Jobs
                .AsNoTracking()
                .Where(x => x.JobId == dto.JobId && x.JobStateId == 4 && x.TerminatedDate == null && x.JoinedDate <= DateTime.UtcNow.AddHours(5).Date)
                .Select(x => new { x.JobId, x.OrganisationID })
                .SingleOrDefaultAsync();

            if (job is null || job.OrganisationID != scope)
            {
                return ServiceResult.Fail("Select a staff member in the current organisation.");
            }

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var overlaps = await db.AssignedWorkTypes.AnyAsync(x =>
                x.IsValid &&
                x.JobId == dto.JobId &&
                x.WorkTypeId == dto.WorkTypeId &&
                x.FromDate <= (dto.ToDate ?? DateOnly.MaxValue) &&
                (x.ToDate == null || x.ToDate >= dto.FromDate));

            if (overlaps)
            {
                return ServiceResult.Fail("This work type is already assigned to the staff member for the selected period.");
            }

            var actor = await _access.RequireContextAsync();

            db.AssignedWorkTypes.Add(new AssignedWorkType
            {
                WorkTypeId = dto.WorkTypeId,
                JobId = dto.JobId,
                OrganisationId = scope,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                Location = location,
                IsValid = true,
                AssignedByUserId = actor.UserId,
                CreatedDate = Today()
            });

            await db.SaveChangesAsync();

            await transaction.CommitAsync();
            return ServiceResult.Ok($"Work type '{workType.Name}' was assigned.");
        }

        public async Task<ServiceResult> UpdateAssignmentAsync(UpdateAssignedWorkTypeDto dto)
        {
            if (dto.AssignedWorkTypeId <= 0)
            {
                return ServiceResult.Fail("Select a work type assignment to update.");
            }

            var scope = await ResolveOrganisationScopeAsync(null);

            var validation = ValidatePeriod(dto.FromDate, dto.ToDate, dto.Location, out var location);

            if (validation is not null)
            {
                return validation;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            var entity = await db.AssignedWorkTypes
                .SingleOrDefaultAsync(x => x.AssignedWorkTypeId == dto.AssignedWorkTypeId && x.OrganisationId == scope);

            if (entity is null)
            {
                return ServiceResult.Fail("Work type assignment was not found.");
            }

            var workTypeExists = await db.WorkTypes.AnyAsync(x => x.WorkTypeId == dto.WorkTypeId && x.IsValid);
            if (!await db.Jobs.AnyAsync(j => j.JobId == entity.JobId && j.OrganisationID == scope && j.JobStateId == 4 &&
                    j.TerminatedDate == null && j.JoinedDate <= DateTime.UtcNow.AddHours(5).Date))
                return ServiceResult.Fail("Only current active employment can be changed.");

            if (!workTypeExists)
            {
                return ServiceResult.Fail("Select a valid work type.");
            }

            var overlaps = await db.AssignedWorkTypes.AnyAsync(x =>
                x.IsValid &&
                x.AssignedWorkTypeId != dto.AssignedWorkTypeId &&
                x.JobId == entity.JobId &&
                x.WorkTypeId == dto.WorkTypeId &&
                x.FromDate <= (dto.ToDate ?? DateOnly.MaxValue) &&
                (x.ToDate == null || x.ToDate >= dto.FromDate));

            if (overlaps && dto.IsValid)
            {
                return ServiceResult.Fail("This work type is already assigned to the staff member for the selected period.");
            }

            entity.WorkTypeId = dto.WorkTypeId;
            entity.FromDate = dto.FromDate;
            entity.ToDate = dto.ToDate;
            entity.Location = location;
            entity.IsValid = dto.IsValid;

            await db.SaveChangesAsync();

            return ServiceResult.Ok("Work type assignment was updated.");
        }

        public async Task<ServiceResult> DeactivateAssignmentAsync(int assignedWorkTypeId)
        {
            if (assignedWorkTypeId <= 0)
            {
                return ServiceResult.Fail("Select a work type assignment.");
            }

            var scope = await ResolveOrganisationScopeAsync(null);

            await using var db = await _dbFactory.CreateDbContextAsync();

            var entity = await db.AssignedWorkTypes
                .SingleOrDefaultAsync(x => x.AssignedWorkTypeId == assignedWorkTypeId && x.OrganisationId == scope);

            if (entity is null)
            {
                return ServiceResult.Fail("Work type assignment was not found.");
            }

            entity.IsValid = false;

            await db.SaveChangesAsync();

            return ServiceResult.Ok("Work type assignment was removed.");
        }

        private async Task<int> ResolveOrganisationScopeAsync(int? organisationId)
        {
            await RequireManageAsync();

            var current = await _access.GetCurrentOrganisationIdAsync();

            if (organisationId is not > 0)
            {
                if (current <= 0)
                {
                    throw new UnauthorizedAccessException("Your current organisation is unavailable.");
                }

                return current;
            }

            var requested = organisationId.Value;

            if (requested != current)
            {
                throw new UnauthorizedAccessException("You do not have access to that organisation.");
            }

            return requested;
        }

        private static ServiceResult? ValidatePeriod(
            DateOnly fromDate,
            DateOnly? toDate,
            string? location,
            out string? trimmedLocation)
        {
            trimmedLocation = string.IsNullOrWhiteSpace(location) ? null : location.Trim();

            if (fromDate == default)
            {
                return ServiceResult.Fail("From date is required.");
            }

            if (toDate is not null && toDate < fromDate)
            {
                return ServiceResult.Fail("To date cannot be earlier than the from date.");
            }

            if (trimmedLocation is { Length: > LocationMaxLength })
            {
                return ServiceResult.Fail($"Location cannot exceed {LocationMaxLength} characters.");
            }

            return null;
        }

        private static DateOnly Today() =>
            DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));

        private async Task<int> RequireManageAsync()
        {
            var actor = await _access.RequireContextAsync();
            var org = await _access.GetCurrentOrganisationIdAsync();
            if (org <= 0 || !await _access.HasOrganisationAccessAsync(org) ||
                !(actor.IsSuperAdministrator || await _access.HasPermissionAsync("WorkTypes.Manage")))
                throw new UnauthorizedAccessException("Work Type management permission is required in the current organisation.");
            return org;
        }
        private async Task RequireCatalogueAsync()
        {
            if (!(await _access.RequireContextAsync()).IsSuperAdministrator)
                throw new UnauthorizedAccessException("Only a platform administrator can change the shared Work Type catalogue.");
        }
        public async Task<bool> CanClockAsync()
        {
            var actor = await _access.RequireContextAsync();
            var org = await _access.GetCurrentOrganisationIdAsync();
            var job = await _access.GetCurrentJobIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await HasClockAssignmentAsync(db, actor.IndividualId, job, org, Today());
        }
        public async Task<List<WorkTypeStaffOption>> GetEligibleStaffAsync()
        {
            var org = await RequireManageAsync();
            var today = DateTime.UtcNow.AddHours(5).Date;
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == org && j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= today)
                .OrderBy(j => j.Individual.FirstNameEnglish).Select(j => new WorkTypeStaffOption(j.JobId,
                    (j.Individual.FirstNameEnglish + " " + (j.Individual.MiddleNameEnglish ?? "") + " " + (j.Individual.LastNameEnglish ?? "")).Trim(), j.OrganisationStructure.Name)).ToListAsync();
        }
        public static Task<bool> HasClockAssignmentAsync(HrmTeContext db, int individualId, int jobId, int organisationId, DateOnly today) =>
            db.AssignedWorkTypes.AnyAsync(a => a.JobId == jobId && a.OrganisationId == organisationId && a.IsValid && a.WorkType.IsValid &&
                a.FromDate <= today && (a.ToDate == null || a.ToDate >= today) &&
                a.Job.IndividualID == individualId && a.Job.OrganisationID == organisationId && a.Job.JobStateId == 4 &&
                a.Job.JoinedDate < today.AddDays(1).ToDateTime(TimeOnly.MinValue) &&
                (a.Job.TerminatedDate == null || a.Job.TerminatedDate > today.ToDateTime(TimeOnly.MinValue)));
    }
}
