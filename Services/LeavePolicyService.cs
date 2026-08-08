using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.LeaveType;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class LeavePolicyService : ILeavePolicyService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;
        private readonly IUserContext _userContext;
        private readonly ILogger<LeavePolicyService> _logger;

        public LeavePolicyService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IUserAccessService userAccessService,
        IUserContext userContext,
        ILogger<LeavePolicyService> logger)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
            _userContext = userContext;
            _logger = logger;
        }


        public async Task<List<LeavePolicyDto>>
               GetOrganisationPoliciesAsync(
                   CancellationToken cancellationToken = default)
        {
            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await
            (
                from policy in db.LeavePolicies.AsNoTracking()

                join definition in db.LeaveDefinitions.AsNoTracking()
                    on policy.LeaveDefinitionId equals
                       definition.LeaveDefinitionId

                where
                    policy.OrganisationId ==
                        organisationId

                orderby
                    policy.IsActive descending,
                    definition.Name,
                    policy.EffectiveFrom descending

                select new LeavePolicyDto
                {
                    LeavePolicyId =
                        policy.LeavePolicyId,

                    LeaveDefinitionId =
                        policy.LeaveDefinitionId,

                    LeaveTypeCode =
                        definition.Code,

                    LeaveTypeName =
                        definition.Name,

                    OrganisationId =
                        policy.OrganisationId,

                    Name =
                        policy.Name,

                    DefaultEntitlementDays =
                        policy.DefaultEntitlementDays,

                    IncludeHolidays =
                        policy.IncludeHolidays,

                    IncludePay =
                        policy.IncludePay,

                    PayPercentage =
                        policy.PayPercentage,

                    IsLocationRequired =
                        policy.IsLocationRequired,

                    IsStaffWideAvailable =
                        policy.IsStaffWideAvailable,

                    MinimumServiceMonths =
                        policy.MinimumServiceMonths,

                    RequestTypeId = policy.RequestTypeId,

                    EffectiveFrom =
                        policy.EffectiveFrom,

                    EffectiveTo =
                        policy.EffectiveTo,

                    IsActive =
                        policy.IsActive
                }
            )
            .ToListAsync(cancellationToken);
        }

        public async Task<LeavePolicyDto?> GetByIdAsync(
            int policyId,
            CancellationToken cancellationToken = default)
        {
            if (policyId <= 0)
            {
                return null;
            }

            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await
            (
                from policy in db.LeavePolicies.AsNoTracking()

                join definition in db.LeaveDefinitions.AsNoTracking()
                    on policy.LeaveDefinitionId equals
                       definition.LeaveDefinitionId

                where
                    policy.LeavePolicyId ==
                        policyId &&
                    policy.OrganisationId ==
                        organisationId

                select new LeavePolicyDto
                {
                    LeavePolicyId =
                        policy.LeavePolicyId,

                    LeaveDefinitionId =
                        policy.LeaveDefinitionId,

                    LeaveTypeCode =
                        definition.Code,

                    LeaveTypeName =
                        definition.Name,

                    OrganisationId =
                        policy.OrganisationId,

                    Name =
                        policy.Name,

                    DefaultEntitlementDays =
                        policy.DefaultEntitlementDays,

                    IncludeHolidays =
                        policy.IncludeHolidays,

                    IncludePay =
                        policy.IncludePay,

                    PayPercentage =
                        policy.PayPercentage,

                    IsLocationRequired =
                        policy.IsLocationRequired,

                    IsStaffWideAvailable =
                        policy.IsStaffWideAvailable,

                    MinimumServiceMonths =
                        policy.MinimumServiceMonths,

                    RequestTypeId =
                        policy.RequestTypeId,

                    EffectiveFrom =
                        policy.EffectiveFrom,

                    EffectiveTo =
                        policy.EffectiveTo,

                    IsActive =
                        policy.IsActive
                }
            )
            .FirstOrDefaultAsync(
                cancellationToken);
        }

        public async Task<ServiceResult<int>> CreateAsync(
            LeavePolicySaveRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation =
                Validate(request);

            if (validation is not null)
            {
                return ServiceResult<int>.Failure(
                    validation);
            }

            try
            {
                var organisationId =
                    await ResolveCurrentOrganisationAsync(
                        cancellationToken);

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);

                var definition =
                    await db.LeaveDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.LeaveDefinitionId ==
                                    request.LeaveDefinitionId &&
                                x.IsActive &&
                                (
                                    x.IsGlobal ||
                                    x.OwnerOrganisationId ==
                                        organisationId
                                ),
                            cancellationToken);

                if (definition is null)
                {
                    return ServiceResult<int>.Failure(
                        "The selected leave definition is not available for this organisation.");
                }

                var overlapExists =
                    await db.LeavePolicies
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.OrganisationId ==
                                    organisationId &&
                                x.LeaveDefinitionId ==
                                    request.LeaveDefinitionId &&
                                x.IsActive &&
                                (
                                    !x.EffectiveTo.HasValue ||
                                    x.EffectiveTo.Value.Date >=
                                        request.EffectiveFrom.Date
                                ) &&
                                (
                                    !request.EffectiveTo.HasValue ||
                                    x.EffectiveFrom.Date <=
                                        request.EffectiveTo.Value.Date
                                ),
                            cancellationToken);

                if (overlapExists)
                {
                    return ServiceResult<int>.Failure(
                        "An active leave policy already exists for this leave type during the selected effective period.");
                }

                var policy =
                    new LeavePolicy
                    {
                        LeaveDefinitionId =
                            request.LeaveDefinitionId,

                        OrganisationId =
                            organisationId,

                        Name =
                            Clean(request.Name),

                        DefaultEntitlementDays =
                            request.DefaultEntitlementDays,

                        IncludeHolidays =
                            request.IncludeHolidays,

                        IncludePay =
                            request.IncludePay,

                        PayPercentage =
                            request.IncludePay
                                ? request.PayPercentage
                                : null,

                        IsLocationRequired =
                            request.IsLocationRequired,

                        IsStaffWideAvailable =
                            request.IsStaffWideAvailable,

                        MinimumServiceMonths =
                            request.MinimumServiceMonths,

                        RequestTypeId = request.RequestTypeId,

                        EffectiveFrom =
                            request.EffectiveFrom.Date,

                        EffectiveTo =
                            request.EffectiveTo?.Date,

                        IsActive =
                            request.IsActive,

                        CreatedDate =
                            DateTime.Now
                    };

                db.LeavePolicies.Add(
                    policy);

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    policy.LeavePolicyId,
                    "Leave policy created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to create leave policy.");

                return ServiceResult<int>.Failure(
                    "The leave policy could not be created.");
            }
        }

        public async Task<ServiceResult<int>> UpdateAsync(
            LeavePolicySaveRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!request.LeavePolicyId.HasValue ||
                request.LeavePolicyId.Value <= 0)
            {
                return ServiceResult<int>.Failure(
                    "A valid leave policy ID is required.");
            }

            var validation =
                Validate(request);

            if (validation is not null)
            {
                return ServiceResult<int>.Failure(
                    validation);
            }

            try
            {
                var organisationId =
                    await ResolveCurrentOrganisationAsync(
                        cancellationToken);

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);

                var policy =
                    await db.LeavePolicies
                        .FirstOrDefaultAsync(
                            x =>
                                x.LeavePolicyId ==
                                    request.LeavePolicyId.Value &&
                                x.OrganisationId ==
                                    organisationId,
                            cancellationToken);

                if (policy is null)
                {
                    return ServiceResult<int>.Failure(
                        "The leave policy could not be found.");
                }

                var overlapExists =
                    await db.LeavePolicies
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.LeavePolicyId !=
                                    policy.LeavePolicyId &&
                                x.OrganisationId ==
                                    organisationId &&
                                x.LeaveDefinitionId ==
                                    request.LeaveDefinitionId &&
                                x.IsActive &&
                                (
                                    !x.EffectiveTo.HasValue ||
                                    x.EffectiveTo.Value.Date >=
                                        request.EffectiveFrom.Date
                                ) &&
                                (
                                    !request.EffectiveTo.HasValue ||
                                    x.EffectiveFrom.Date <=
                                        request.EffectiveTo.Value.Date
                                ),
                            cancellationToken);

                if (overlapExists)
                {
                    return ServiceResult<int>.Failure(
                        "Another active policy overlaps this effective period.");
                }

                policy.LeaveDefinitionId =
                    request.LeaveDefinitionId;

                policy.Name =
                    Clean(request.Name);

                policy.DefaultEntitlementDays =
                    request.DefaultEntitlementDays;

                policy.IncludeHolidays =
                    request.IncludeHolidays;

                policy.IncludePay =
                    request.IncludePay;

                policy.PayPercentage =
                    request.IncludePay
                        ? request.PayPercentage
                        : null;

                policy.IsLocationRequired =
                    request.IsLocationRequired;

                policy.IsStaffWideAvailable =
                    request.IsStaffWideAvailable;

                policy.MinimumServiceMonths =
                    request.MinimumServiceMonths;

                policy.RequestTypeId =
                    request.RequestTypeId;

                policy.EffectiveFrom =
                    request.EffectiveFrom.Date;

                policy.EffectiveTo =
                    request.EffectiveTo?.Date;

                policy.IsActive =
                    request.IsActive;

                policy.UpdatedDate =
                    DateTime.Now;

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    policy.LeavePolicyId,
                    "Leave policy updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to update leave policy {LeavePolicyId}.",
                    request.LeavePolicyId);

                return ServiceResult<int>.Failure(
                    "The leave policy could not be updated.");
            }
        }

        public async Task<ServiceResult<int>> DeactivateAsync(
            int policyId,
            CancellationToken cancellationToken = default)
        {
            if (policyId <= 0)
            {
                return ServiceResult<int>.Failure(
                    "A valid leave policy ID is required.");
            }

            try
            {
                var organisationId =
                    await ResolveCurrentOrganisationAsync(
                        cancellationToken);

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);

                var policy =
                    await db.LeavePolicies
                        .FirstOrDefaultAsync(
                            x =>
                                x.LeavePolicyId ==
                                    policyId &&
                                x.OrganisationId ==
                                    organisationId,
                            cancellationToken);

                if (policy is null)
                {
                    return ServiceResult<int>.Failure(
                        "The leave policy could not be found.");
                }

                policy.IsActive =
                    false;

                policy.EffectiveTo ??=
                    DateTime.Today;

                policy.UpdatedDate =
                    DateTime.Now;

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    policy.LeavePolicyId,
                    "Leave policy deactivated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to deactivate leave policy {LeavePolicyId}.",
                    policyId);

                return ServiceResult<int>.Failure(
                    "The leave policy could not be deactivated.");
            }
        }

        private static string? Validate(
            LeavePolicySaveRequest request)
        {
            if (request.LeaveDefinitionId <= 0)
            {
                return "A leave definition is required.";
            }

            if (request.DefaultEntitlementDays.HasValue &&
                request.DefaultEntitlementDays.Value < 0)
            {
                return "Default entitlement cannot be negative.";
            }

            if (request.MinimumServiceMonths.HasValue &&
                request.MinimumServiceMonths.Value < 0)
            {
                return "Minimum service months cannot be negative.";
            }

            if (request.IncludePay &&
                request.PayPercentage.HasValue &&
                (
                    request.PayPercentage.Value < 0 ||
                    request.PayPercentage.Value > 100
                ))
            {
                return "Pay percentage must be between 0 and 100.";
            }

            if (request.EffectiveTo.HasValue &&
                request.EffectiveTo.Value.Date <
                    request.EffectiveFrom.Date)
            {
                return "Effective To cannot be earlier than Effective From.";
            }

            return null;
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
