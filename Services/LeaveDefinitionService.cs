using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.LeaveType;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class LeaveDefinitionService : ILeaveDefinitionService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;
        private readonly IUserContext _userContext;

        public LeaveDefinitionService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IUserAccessService userAccessService,
            IUserContext userContext)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
            _userContext = userContext;
        }

        public async Task<List<LeaveDefinitionDto>> GetAvailableAsync(
       CancellationToken cancellationToken = default)
        {
            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.LeaveDefinitions
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    (
                        x.IsGlobal ||
                        x.OwnerOrganisationId ==
                            organisationId
                    ))
                .OrderByDescending(x =>
                    x.IsSystemType)
                .ThenBy(x =>
                    x.Name)
                .Select(x =>
                    new LeaveDefinitionDto
                    {
                        LeaveDefinitionId =
                            x.LeaveDefinitionId,

                        Code =
                            x.Code,

                        Name =
                            x.Name,

                        NameDhivehi =
                            x.NameDhivehi,

                        Description =
                            x.Description,

                        OwnerOrganisationId =
                            x.OwnerOrganisationId,

                        IsSystemType =
                            x.IsSystemType,

                        IsGlobal =
                            x.IsGlobal,

                        IsActive =
                            x.IsActive,

                        IsLegacyMapped =
                            x.LeaveTypeMappings.Any(m =>
                                m.IsActive),

                        LegacyLeaveTypeId =
                            x.LeaveTypeMappings
                                .Where(m =>
                                    m.IsActive)
                                .Select(m =>
                                    (int?)m.LegacyLeaveTypeId)
                                .FirstOrDefault()
                    })
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<LeaveDefinitionDto?> GetByIdAsync(
            int leaveDefinitionId,
            CancellationToken cancellationToken = default)
        {
            if (leaveDefinitionId <= 0)
            {
                return null;
            }

            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.LeaveDefinitions
                .AsNoTracking()
                .Where(x =>
                    x.LeaveDefinitionId ==
                        leaveDefinitionId &&
                    (
                        x.IsGlobal ||
                        x.OwnerOrganisationId ==
                            organisationId
                    ))
                .Select(x =>
                    new LeaveDefinitionDto
                    {
                        LeaveDefinitionId =
                            x.LeaveDefinitionId,

                        Code =
                            x.Code,

                        Name =
                            x.Name,

                        NameDhivehi =
                            x.NameDhivehi,

                        Description =
                            x.Description,

                        OwnerOrganisationId =
                            x.OwnerOrganisationId,

                        IsSystemType =
                            x.IsSystemType,

                        IsGlobal =
                            x.IsGlobal,

                        IsActive =
                            x.IsActive
                    })
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(
            LeaveDefinitionSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            var context =
                await _userAccessService.GetContextAsync();

            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var code =
                request.Code.Trim()
                    .ToUpperInvariant();

            var exists =
                await db.LeaveDefinitions
                    .AnyAsync(
                        x => x.Code == code,
                        cancellationToken);

            if (exists)
            {
                return ServiceResult<int>.Failure(
                    "A leave definition with this code already exists.");
            }

            /*
             * Only Super Admin may create global/system definitions.
             */
            var isSuperAdmin =
                context?.IsSuperAdministrator == true;

            var definition =
                new LeaveDefinition
                {
                    Code =
                        code,

                    Name =
                        request.Name.Trim(),

                    NameDhivehi =
                        Clean(request.NameDhivehi),

                    Description =
                        Clean(request.Description),

                    OwnerOrganisationId =
                        request.IsGlobal
                            ? null
                            : organisationId,

                    IsGlobal =
                        isSuperAdmin &&
                        request.IsGlobal,

                    IsSystemType =
                        isSuperAdmin &&
                        request.IsSystemType,

                    IsActive =
                        request.IsActive,

                    CreatedDate =
                        DateTime.Now
                };

            db.LeaveDefinitions.Add(
                definition);

            await db.SaveChangesAsync(
                cancellationToken);

            return ServiceResult<int>.Successful(
                definition.LeaveDefinitionId,
                "Leave definition created successfully.");
        }

        public async Task<ServiceResult<int>> UpdateAsync(
            LeaveDefinitionSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!request.LeaveDefinitionId.HasValue)
            {
                return ServiceResult<int>.Failure(
                    "Leave definition ID is required.");
            }

            var context =
                await _userAccessService.GetContextAsync();

            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var definition =
                await db.LeaveDefinitions
                    .FirstOrDefaultAsync(
                        x =>
                            x.LeaveDefinitionId ==
                                request.LeaveDefinitionId.Value,
                        cancellationToken);

            if (definition is null)
            {
                return ServiceResult<int>.Failure(
                    "Leave definition was not found.");
            }

            var isSuperAdmin =
                context?.IsSuperAdministrator == true;

            if (!isSuperAdmin &&
                definition.OwnerOrganisationId !=
                    organisationId)
            {
                return ServiceResult<int>.Failure(
                    "You cannot edit this leave definition.");
            }

            if (!isSuperAdmin &&
                (
                    definition.IsGlobal ||
                    definition.IsSystemType
                ))
            {
                return ServiceResult<int>.Failure(
                    "Global/system leave definitions can only be edited by a Super Administrator.");
            }

            definition.Name =
                request.Name.Trim();

            definition.NameDhivehi =
                Clean(request.NameDhivehi);

            definition.Description =
                Clean(request.Description);

            definition.IsActive =
                request.IsActive;

            if (isSuperAdmin)
            {
                definition.IsGlobal =
                    request.IsGlobal;

                definition.IsSystemType =
                    request.IsSystemType;
            }

            definition.UpdatedDate =
                DateTime.Now;

            await db.SaveChangesAsync(
                cancellationToken);

            return ServiceResult<int>.Successful(
                definition.LeaveDefinitionId,
                "Leave definition updated successfully.");
        }

        private async Task<int> ResolveCurrentOrganisationAsync(
            CancellationToken cancellationToken)
        {
            var context =
                await _userAccessService.GetContextAsync();

            var session =
                await _userContext.GetSessionAsync();

            if (session is null ||
                session.OrganisationId <= 0)
            {
                throw new InvalidOperationException(
                    "Current portal organisation could not be resolved.");
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            /*
             * Portal session OrganisationId currently represents
             * UserOrganisationID.
             */
            var organisationId =
                await db.UserOrganisations
                    .AsNoTracking()
                    .Where(x =>
                        x.UserOrganisationID ==
                            session.OrganisationId)
                    .Select(x =>
                        x.BusinessEntityID)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (organisationId <= 0)
            {
                throw new InvalidOperationException(
                    "Current organisation could not be resolved.");
            }

            if (context?.IsSuperAdministrator == true)
            {
                return organisationId;
            }

            if (context?.ActiveJob is null ||
                context.ActiveJob.OrganisationId !=
                    organisationId)
            {
                throw new UnauthorizedAccessException(
                    "The selected organisation does not match the current user's active organisation.");
            }

            return organisationId;
        }

        private static string? Clean(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }


    }
}
