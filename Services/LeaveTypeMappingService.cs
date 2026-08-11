using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class LeaveTypeMappingService : ILeaveTypeMappingService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;
        private readonly IUserContext _userContext;
        private readonly ILogger<LeaveTypeMappingService> _logger;

        public LeaveTypeMappingService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IUserAccessService userAccessService,
        IUserContext userContext,
        ILogger<LeaveTypeMappingService> logger)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<LeaveTypeMappingDto?>GetByLegacyLeaveTypeAsync(int? legacyLeaveTypeId,
           CancellationToken cancellationToken = default)
        {
            if (legacyLeaveTypeId <= 0)
            {
                return null;
            }

            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var today =
                DateTime.Today;

            return await
            (
                from mapping in
                    db.LeaveTypeMappings.AsNoTracking()

                join legacy in
                    db.LeaveTypes.AsNoTracking()
                    on mapping.LegacyLeaveTypeId equals
                       legacy.LeaveTypeId

                join definition in
                    db.LeaveDefinitions.AsNoTracking()
                    on mapping.LeaveDefinitionId equals
                       definition.LeaveDefinitionId

                where
                    mapping.LegacyLeaveTypeId ==
                        legacyLeaveTypeId &&

                    (
                        mapping.OrganisationId ==
                            organisationId ||
                        mapping.OrganisationId == null
                    ) &&

                    mapping.IsActive &&

                    (
                        mapping.EffectiveFrom == null ||
                        mapping.EffectiveFrom <= today
                    ) &&

                    (
                        mapping.EffectiveTo == null ||
                        mapping.EffectiveTo >= today
                    )

                orderby
                    mapping.OrganisationId.HasValue descending,
                    mapping.EffectiveFrom descending,
                    mapping.LeaveTypeMappingId descending

                select new LeaveTypeMappingDto
                {
                    LeaveTypeMappingId =
                        mapping.LeaveTypeMappingId,

                    LegacyLeaveTypeId =
                        mapping.LegacyLeaveTypeId,

                    LegacyLeaveTypeName =
                        legacy.Name ?? string.Empty,

                    LeaveDefinitionId =
                        mapping.LeaveDefinitionId,

                    LeaveDefinitionCode =
                        definition.Code ?? string.Empty,

                    LeaveDefinitionName =
                        definition.Name ?? string.Empty,

                    OrganisationId =
                        mapping.OrganisationId,

                    EffectiveFrom =
                        mapping.EffectiveFrom,

                    EffectiveTo =
                        mapping.EffectiveTo,

                    IsActive =
                        mapping.IsActive
                }
            )
            .FirstOrDefaultAsync(
                cancellationToken);
        }

        public async Task<ServiceResult<int>>
            SetMappingAsync(
                LeaveTypeMappingSaveRequest request,
                CancellationToken cancellationToken = default)
        {
            if (request.LegacyLeaveTypeId <= 0)
            {
                return ServiceResult<int>.Failure(
                    "A valid legacy leave type is required.");
            }

            if (!request.LeaveDefinitionId.HasValue ||
                request.LeaveDefinitionId.Value <= 0)
            {
                return ServiceResult<int>.Failure(
                    "A valid leave definition is required.");
            }

            if (request.EffectiveTo.HasValue &&
                request.EffectiveFrom.HasValue &&
                request.EffectiveTo.Value.Date <
                request.EffectiveFrom.Value.Date)
            {
                return ServiceResult<int>.Failure(
                    "Effective To cannot be earlier than Effective From.");
            }

            try
            {
                var organisationId =
                    await ResolveCurrentOrganisationAsync(
                        cancellationToken);

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);

                var legacyExists =
                    await db.LeaveTypes
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.LeaveTypeId ==
                                    request.LegacyLeaveTypeId,
                            cancellationToken);

                if (!legacyExists)
                {
                    return ServiceResult<int>.Failure(
                        "The selected legacy leave type could not be found.");
                }

                var definitionExists =
                    await db.LeaveDefinitions
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.LeaveDefinitionId ==
                                    request.LeaveDefinitionId.Value &&
                                x.IsActive &&
                                (
                                    x.IsGlobal ||
                                    x.OwnerOrganisationId ==
                                        organisationId
                                ),
                            cancellationToken);

                if (!definitionExists)
                {
                    return ServiceResult<int>.Failure(
                        "The selected leave definition is not available for this organisation.");
                }

                var effectiveFrom =
                    request.EffectiveFrom?.Date ??
                    DateTime.Today;

                var currentMapping =
                    await db.LeaveTypeMappings
                        .Where(x =>
                            x.LegacyLeaveTypeId ==
                                request.LegacyLeaveTypeId &&
                            x.OrganisationId ==
                                organisationId &&
                            x.IsActive)
                        .OrderByDescending(x =>
                            x.EffectiveFrom)
                        .ThenByDescending(x =>
                            x.LeaveTypeMappingId)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                /*
                 * Same mapping:
                 * just update effective dates.
                 */
                if (currentMapping is not null &&
                    currentMapping.LeaveDefinitionId ==
                        request.LeaveDefinitionId.Value)
                {
                    currentMapping.EffectiveFrom =
                        effectiveFrom;

                    currentMapping.EffectiveTo =
                        request.EffectiveTo?.Date;

                    currentMapping.IsActive =
                        true;

                    await db.SaveChangesAsync(
                        cancellationToken);

                    return ServiceResult<int>.Successful(
                        currentMapping.LeaveTypeMappingId,
                        "Leave type mapping updated successfully.");
                }

                /*
                 * Different existing mapping:
                 * close the old one instead of overwriting it.
                 */
                if (currentMapping is not null)
                {
                    currentMapping.IsActive =
                        false;

                    currentMapping.EffectiveTo =
                        effectiveFrom.AddDays(-1);
                }

                var newMapping =
                    new LeaveTypeMapping
                    {
                        LegacyLeaveTypeId =
                            request.LegacyLeaveTypeId,

                        LeaveDefinitionId =
                            request.LeaveDefinitionId.Value,

                        OrganisationId =
                            organisationId,

                        EffectiveFrom =
                            effectiveFrom,

                        EffectiveTo =
                            request.EffectiveTo?.Date,

                        IsActive =
                            true
                    };

                db.LeaveTypeMappings.Add(
                    newMapping);

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    newMapping.LeaveTypeMappingId,
                    currentMapping is null
                        ? "Leave type mapping created successfully."
                        : "Leave type mapping changed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to set leave type mapping for legacy LeaveTypeID {LeaveTypeId}.",
                    request.LegacyLeaveTypeId);

                return ServiceResult<int>.Failure(
                    "The leave type mapping could not be saved.");
            }
        }

        public async Task<ServiceResult<int>>
            RemoveMappingAsync(
                int? legacyLeaveTypeId,
                CancellationToken cancellationToken = default)
        {
            if (legacyLeaveTypeId <= 0)
            {
                return ServiceResult<int>.Failure(
                    "A valid legacy leave type is required.");
            }

            try
            {
                var organisationId = await ResolveCurrentOrganisationAsync(cancellationToken);

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);

                var mapping =
                    await db.LeaveTypeMappings
                        .Where(x =>
                            x.LegacyLeaveTypeId ==
                                legacyLeaveTypeId &&
                            x.OrganisationId ==
                                organisationId &&
                            x.IsActive)
                        .OrderByDescending(x =>
                            x.EffectiveFrom)
                        .ThenByDescending(x =>
                            x.LeaveTypeMappingId)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                if (mapping is null)
                {
                    return ServiceResult<int>.Failure(
                        "No active mapping was found for this legacy leave type.");
                }

                mapping.IsActive =
                    false;

                mapping.EffectiveTo ??=
                    DateTime.Today;

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    mapping.LeaveTypeMappingId,
                    "Leave type mapping removed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to remove leave type mapping for LeaveTypeID {LeaveTypeId}.",
                    legacyLeaveTypeId);

                return ServiceResult<int>.Failure(
                    "The leave type mapping could not be removed.");
            }
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

            /*
             * Super Admin can operate in the currently
             * selected portal organisation.
             */
            if (context?.IsSuperAdministrator == true)
            {
                return organisationId;
            }

            /*
             * Normal users must belong to the selected
             * organisation through their active job.
             */
            if (context?.ActiveJob is null ||
                context.ActiveJob.OrganisationId !=
                    organisationId)
            {
                throw new UnauthorizedAccessException(
                    "The selected organisation does not match the current user's active organisation.");
            }

            return organisationId;
        }

    }
}
