using HRM.DTOs.LeaveTypePolicy;
using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.Policy;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public sealed class LeavePolicyAccrualRuleService : ILeavePolicyAccrualRuleService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;
        private readonly IUserContext _userContext;
        private readonly ILogger<LeavePolicyAccrualRuleService> _logger;

        public LeavePolicyAccrualRuleService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IUserAccessService userAccessService,
            IUserContext userContext,
            ILogger<LeavePolicyAccrualRuleService> logger)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
            _userContext = userContext;
            _logger = logger;
        }
        // ============================================================
        // GET ALL RULES FOR POLICY
        // ============================================================

        public async Task<List<LeavePolicyAccrualRuleDto>>
            GetByPolicyIdAsync(
                int leavePolicyId,
                CancellationToken cancellationToken = default)
        {
            if (leavePolicyId <= 0)
            {
                return [];
            }

            var organisationId =
                await ResolveCurrentOrganisationAsync(
                    cancellationToken);

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var policyExists =
                await db.LeavePolicies
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.LeavePolicyId == leavePolicyId &&
                            x.OrganisationId == organisationId,
                        cancellationToken);

            if (!policyExists)
            {
                return [];
            }

            return await db.LeavePolicyAccrualRules
                .AsNoTracking()
                .Where(x =>
                    x.LeavePolicyId == leavePolicyId)
                .OrderByDescending(x =>
                    x.IsActive)
                .ThenByDescending(x =>
                    x.LeavePolicyAccrualRuleId)
                .Select(x =>
                    new LeavePolicyAccrualRuleDto
                    {
                        LeavePolicyAccrualRuleId =
                            x.LeavePolicyAccrualRuleId,

                        LeavePolicyId =
                            x.LeavePolicyId,

                        AccrualType =
                            x.AccrualType,

                        AccrualAmount =
                            x.AccrualAmount,

                        AccrualIntervalMonths =
                            x.AccrualIntervalMonths,

                        AccrualStartMonth =
                            x.AccrualStartMonth,

                        MaximumBalance =
                            x.MaximumBalance,

                        CarryForwardAllowed =
                            x.CarryForwardAllowed,

                        MaximumCarryForward =
                            x.MaximumCarryForward,

                        ExpiresAfterMonths =
                            x.ExpiresAfterMonths,

                        IsActive =
                            x.IsActive
                    })
                .ToListAsync(
                    cancellationToken);
        }

        // ============================================================
        // GET ACTIVE RULE
        // ============================================================

        public async Task<LeavePolicyAccrualRuleDto?>
            GetActiveRuleAsync(
                int leavePolicyId,
                CancellationToken cancellationToken = default)
        {
            if (leavePolicyId <= 0)
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
                from rule in
                    db.LeavePolicyAccrualRules.AsNoTracking()

                join policy in
                    db.LeavePolicies.AsNoTracking()
                    on rule.LeavePolicyId equals
                       policy.LeavePolicyId

                where
                    rule.LeavePolicyId == leavePolicyId &&
                    policy.OrganisationId == organisationId &&
                    rule.IsActive

                orderby
                    rule.LeavePolicyAccrualRuleId descending

                select new LeavePolicyAccrualRuleDto
                {
                    LeavePolicyAccrualRuleId =
                        rule.LeavePolicyAccrualRuleId,

                    LeavePolicyId =
                        rule.LeavePolicyId,

                    AccrualType =
                        rule.AccrualType,

                    AccrualAmount =
                        rule.AccrualAmount,

                    AccrualIntervalMonths =
                        rule.AccrualIntervalMonths,

                    AccrualStartMonth =
                        rule.AccrualStartMonth,

                    MaximumBalance =
                        rule.MaximumBalance,

                    CarryForwardAllowed =
                        rule.CarryForwardAllowed,

                    MaximumCarryForward =
                        rule.MaximumCarryForward,

                    ExpiresAfterMonths =
                        rule.ExpiresAfterMonths,

                    IsActive =
                        rule.IsActive
                }
            )
            .FirstOrDefaultAsync(
                cancellationToken);
        }

        // ============================================================
        // GET BY ID
        // ============================================================

        public async Task<LeavePolicyAccrualRuleDto?> GetByIdAsync(
            int leavePolicyAccrualRuleId,
            CancellationToken cancellationToken = default)
        {
            if (leavePolicyAccrualRuleId <= 0)
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
                from rule in
                    db.LeavePolicyAccrualRules.AsNoTracking()

                join policy in
                    db.LeavePolicies.AsNoTracking()
                    on rule.LeavePolicyId equals
                       policy.LeavePolicyId

                where
                    rule.LeavePolicyAccrualRuleId ==
                        leavePolicyAccrualRuleId &&

                    policy.OrganisationId ==
                        organisationId

                select new LeavePolicyAccrualRuleDto
                {
                    LeavePolicyAccrualRuleId =
                        rule.LeavePolicyAccrualRuleId,

                    LeavePolicyId =
                        rule.LeavePolicyId,

                    AccrualType =
                        rule.AccrualType,

                    AccrualAmount =
                        rule.AccrualAmount,

                    AccrualIntervalMonths =
                        rule.AccrualIntervalMonths,

                    AccrualStartMonth =
                        rule.AccrualStartMonth,

                    MaximumBalance =
                        rule.MaximumBalance,

                    CarryForwardAllowed =
                        rule.CarryForwardAllowed,

                    MaximumCarryForward =
                        rule.MaximumCarryForward,

                    ExpiresAfterMonths =
                        rule.ExpiresAfterMonths,

                    IsActive =
                        rule.IsActive
                }
            )
            .FirstOrDefaultAsync(
                cancellationToken);
        }

        // ============================================================
        // CREATE
        // ============================================================

        public async Task<ServiceResult<int>> CreateAsync(
            LeavePolicyAccrualRuleSaveRequest request,
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

                // Make sure the policy belongs to this organisation.
                var policyExists =
                    await db.LeavePolicies
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.LeavePolicyId ==
                                    request.LeavePolicyId &&

                                x.OrganisationId ==
                                    organisationId,
                            cancellationToken);

                if (!policyExists)
                {
                    return ServiceResult<int>.Failure(
                        "The selected leave policy could not be found.");
                }

                // Current design:
                // one active accrual rule per policy.
                if (request.IsActive)
                {
                    var activeRuleExists =
                        await db.LeavePolicyAccrualRules
                            .AsNoTracking()
                            .AnyAsync(
                                x =>
                                    x.LeavePolicyId ==
                                        request.LeavePolicyId &&

                                    x.IsActive,
                                cancellationToken);

                    if (activeRuleExists)
                    {
                        return ServiceResult<int>.Failure(
                            "This leave policy already has an active accrual rule.");
                    }
                }

                var rule =
                    new LeavePolicyAccrualRule
                    {
                        LeavePolicyId =
                            request.LeavePolicyId,

                        AccrualType =
                            request.AccrualType,

                        AccrualAmount =
                            request.AccrualAmount,

                        AccrualIntervalMonths =
                            request.AccrualIntervalMonths,

                        AccrualStartMonth =
                            request.AccrualStartMonth,

                        MaximumBalance =
                            request.MaximumBalance,

                        CarryForwardAllowed =
                            request.CarryForwardAllowed,

                        MaximumCarryForward =
                            request.CarryForwardAllowed
                                ? request.MaximumCarryForward
                                : null,

                        ExpiresAfterMonths =
                            request.ExpiresAfterMonths,

                        IsActive =
                            request.IsActive
                    };

                db.LeavePolicyAccrualRules.Add(
                    rule);

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    rule.LeavePolicyAccrualRuleId,
                    "Accrual rule created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to create accrual rule for leave policy {LeavePolicyId}.",
                    request.LeavePolicyId);

                return ServiceResult<int>.Failure(
                    "The accrual rule could not be created.");
            }
        }

        // ============================================================
        // UPDATE
        // ============================================================

        public async Task<ServiceResult<int>> UpdateAsync(
            LeavePolicyAccrualRuleSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!request.LeavePolicyAccrualRuleId.HasValue ||
                request.LeavePolicyAccrualRuleId.Value <= 0)
            {
                return ServiceResult<int>.Failure(
                    "A valid accrual rule ID is required.");
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

                var rule =
                    await
                    (
                        from accrualRule in
                            db.LeavePolicyAccrualRules

                        join policy in
                            db.LeavePolicies
                            on accrualRule.LeavePolicyId equals
                               policy.LeavePolicyId

                        where
                            accrualRule.LeavePolicyAccrualRuleId ==
                                request.LeavePolicyAccrualRuleId.Value &&

                            policy.OrganisationId ==
                                organisationId

                        select accrualRule
                    )
                    .FirstOrDefaultAsync(
                        cancellationToken);

                if (rule is null)
                {
                    return ServiceResult<int>.Failure(
                        "The accrual rule could not be found.");
                }

                /*
                 * Don't allow the caller to move an existing
                 * rule from one policy to another.
                 */
                if (rule.LeavePolicyId !=
                    request.LeavePolicyId)
                {
                    return ServiceResult<int>.Failure(
                        "The accrual rule does not belong to the selected leave policy.");
                }

                if (request.IsActive)
                {
                    var anotherActiveRuleExists =
                        await db.LeavePolicyAccrualRules
                            .AsNoTracking()
                            .AnyAsync(
                                x =>
                                    x.LeavePolicyId ==
                                        rule.LeavePolicyId &&

                                    x.LeavePolicyAccrualRuleId !=
                                        rule.LeavePolicyAccrualRuleId &&

                                    x.IsActive,
                                cancellationToken);

                    if (anotherActiveRuleExists)
                    {
                        return ServiceResult<int>.Failure(
                            "Another active accrual rule already exists for this leave policy.");
                    }
                }

                rule.AccrualType =
                    request.AccrualType;

                rule.AccrualAmount =
                    request.AccrualAmount;

                rule.AccrualIntervalMonths =
                    request.AccrualIntervalMonths;

                rule.AccrualStartMonth =
                    request.AccrualStartMonth;

                rule.MaximumBalance =
                    request.MaximumBalance;

                rule.CarryForwardAllowed =
                    request.CarryForwardAllowed;

                rule.MaximumCarryForward =
                    request.CarryForwardAllowed
                        ? request.MaximumCarryForward
                        : null;

                rule.ExpiresAfterMonths =
                    request.ExpiresAfterMonths;

                rule.IsActive =
                    request.IsActive;

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    rule.LeavePolicyAccrualRuleId,
                    "Accrual rule updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to update accrual rule {AccrualRuleId}.",
                    request.LeavePolicyAccrualRuleId);

                return ServiceResult<int>.Failure(
                    "The accrual rule could not be updated.");
            }
        }

        // ============================================================
        // DEACTIVATE
        // ============================================================

        public async Task<ServiceResult<int>> DeactivateAsync(
            int leavePolicyAccrualRuleId,
            CancellationToken cancellationToken = default)
        {
            if (leavePolicyAccrualRuleId <= 0)
            {
                return ServiceResult<int>.Failure(
                    "A valid accrual rule ID is required.");
            }

            try
            {
                var organisationId =
                    await ResolveCurrentOrganisationAsync(
                        cancellationToken);

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);

                var rule =
                    await
                    (
                        from accrualRule in
                            db.LeavePolicyAccrualRules

                        join policy in
                            db.LeavePolicies
                            on accrualRule.LeavePolicyId equals
                               policy.LeavePolicyId

                        where
                            accrualRule.LeavePolicyAccrualRuleId ==
                                leavePolicyAccrualRuleId &&

                            policy.OrganisationId ==
                                organisationId

                        select accrualRule
                    )
                    .FirstOrDefaultAsync(
                        cancellationToken);

                if (rule is null)
                {
                    return ServiceResult<int>.Failure(
                        "The accrual rule could not be found.");
                }

                rule.IsActive =
                    false;

                await db.SaveChangesAsync(
                    cancellationToken);

                return ServiceResult<int>.Successful(
                    rule.LeavePolicyAccrualRuleId,
                    "Accrual rule deactivated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to deactivate accrual rule {AccrualRuleId}.",
                    leavePolicyAccrualRuleId);

                return ServiceResult<int>.Failure(
                    "The accrual rule could not be deactivated.");
            }
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        private static string? Validate(
            LeavePolicyAccrualRuleSaveRequest request)
        {
            if (request.LeavePolicyId <= 0)
            {
                return "A valid leave policy is required.";
            }

            if (request.AccrualAmount.HasValue &&
                request.AccrualAmount.Value < 0)
            {
                return "Accrual amount cannot be negative.";
            }

            if (request.AccrualIntervalMonths.HasValue &&
                request.AccrualIntervalMonths.Value <= 0)
            {
                return "Accrual interval must be greater than zero.";
            }

            if (request.AccrualStartMonth.HasValue &&
                (
                    request.AccrualStartMonth.Value < 1 ||
                    request.AccrualStartMonth.Value > 12
                ))
            {
                return "Accrual start month must be between 1 and 12.";
            }

            if (request.MaximumBalance.HasValue &&
                request.MaximumBalance.Value < 0)
            {
                return "Maximum balance cannot be negative.";
            }

            if (request.MaximumCarryForward.HasValue &&
                request.MaximumCarryForward.Value < 0)
            {
                return "Maximum carry forward cannot be negative.";
            }

            if (request.ExpiresAfterMonths.HasValue &&
                request.ExpiresAfterMonths.Value <= 0)
            {
                return "Expiry period must be greater than zero.";
            }

            return null;
        }

        // ============================================================
        // ORGANISATION RESOLUTION
        // ============================================================

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
             * session.OrganisationId is UserOrganisationID.
             *
             * BusinessEntityID is the actual OrganisationID
             * used by Jobs, LeavePolicies, etc.
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

    }
}
