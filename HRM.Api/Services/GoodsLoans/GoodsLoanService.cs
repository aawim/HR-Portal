using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.Contracts.Pages;
using HRM.Application.Features.GoodsLoans;
using HRM.Components.Shared;
using HRM.Constants;
using HRM.DTOs;
using HRM.DTOs.GoodsLoans;
using HRM.Models;
using HRM.Models.GoodsLoans;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.GoodsLoans;

public sealed class GoodsLoanService : IGoodsLoanService
{
    private readonly IDbContextFactory<HrmTeContext> _dbFactory;
    private readonly IGoodsLoanScheduleCalculator _calculator;
    private readonly IUserAccessService _access;
    private readonly ILogger<GoodsLoanService> _logger;

    public GoodsLoanService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IGoodsLoanScheduleCalculator calculator,
        IUserAccessService access,
        ILogger<GoodsLoanService> logger)
    {
        _dbFactory = dbFactory;
        _calculator = calculator;
        _access = access;
        _logger = logger;
    }

    #region Stores

    public async Task<List<GoodsLoanStoreDto>> GetStoresAsync(int? organisationId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var org = organisationId ?? await _access.GetCurrentOrganisationIdAsync();

        var stores = await db.GoodsLoanStores
            .AsNoTracking()
            .OrderBy(s => s.StoreName)
            .Select(s => new GoodsLoanStoreDto
            {
                StoreID = s.StoreID,
                StoreCode = s.StoreCode,
                StoreName = s.StoreName,
                RegistrationNumber = s.RegistrationNumber,
                ContactPerson = s.ContactPerson,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                Address = s.Address,
                IsActive = s.IsActive,
                CreatedDate = s.CreatedDate,
                ActiveAgreementsCount = s.Agreements.Count(a => a.IsActive && (org <= 0 || a.OrganisationBusinessEntityID == org)),
                ActiveLoansCount = s.Loans.Count(l => l.Status == GoodsLoanStatus.Active && (org <= 0 || l.OrganisationBusinessEntityID == org))
            })
            .ToListAsync(ct);

        return stores;
    }

    public async Task<GoodsLoanStoreDto?> GetStoreByIdAsync(int storeId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var s = await db.GoodsLoanStores.AsNoTracking().FirstOrDefaultAsync(x => x.StoreID == storeId, ct);
        if (s == null) return null;

        return new GoodsLoanStoreDto
        {
            StoreID = s.StoreID,
            StoreCode = s.StoreCode,
            StoreName = s.StoreName,
            RegistrationNumber = s.RegistrationNumber,
            ContactPerson = s.ContactPerson,
            PhoneNumber = s.PhoneNumber,
            Email = s.Email,
            Address = s.Address,
            IsActive = s.IsActive,
            CreatedDate = s.CreatedDate
        };
    }

    public async Task<ServiceResult> SaveStoreAsync(SaveGoodsLoanStoreRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.StoreCode) || string.IsNullOrWhiteSpace(request.StoreName))
        {
            return ServiceResult.Failed("Store code and store name are required.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var userCtx = await _access.RequireContextAsync();
        var userId = userCtx.UserId > 0 ? userCtx.UserId : userCtx.IndividualId;

        if (request.StoreID <= 0)
        {
            var exists = await db.GoodsLoanStores.AnyAsync(s => s.StoreCode == request.StoreCode.Trim(), ct);
            if (exists) return ServiceResult.Failed($"Store with code '{request.StoreCode}' already exists.");

            var store = new GoodsLoanStore
            {
                StoreCode = request.StoreCode.Trim().ToUpperInvariant(),
                StoreName = request.StoreName.Trim(),
                RegistrationNumber = request.RegistrationNumber.Trim(),
                ContactPerson = request.ContactPerson.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Email = request.Email.Trim(),
                Address = request.Address.Trim(),
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                CreatedByUserID = userId
            };

            db.GoodsLoanStores.Add(store);
            await db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
        else
        {
            var store = await db.GoodsLoanStores.FirstOrDefaultAsync(s => s.StoreID == request.StoreID, ct);
            if (store == null) return ServiceResult.Failed("Store not found.");

            var exists = await db.GoodsLoanStores.AnyAsync(s => s.StoreID != request.StoreID && s.StoreCode == request.StoreCode.Trim(), ct);
            if (exists) return ServiceResult.Failed($"Another store with code '{request.StoreCode}' already exists.");

            store.StoreCode = request.StoreCode.Trim().ToUpperInvariant();
            store.StoreName = request.StoreName.Trim();
            store.RegistrationNumber = request.RegistrationNumber.Trim();
            store.ContactPerson = request.ContactPerson.Trim();
            store.PhoneNumber = request.PhoneNumber.Trim();
            store.Email = request.Email.Trim();
            store.Address = request.Address.Trim();
            store.IsActive = request.IsActive;

            await db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
    }

    public async Task<ServiceResult> ToggleStoreActiveAsync(int storeId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var store = await db.GoodsLoanStores.FirstOrDefaultAsync(s => s.StoreID == storeId, ct);
        if (store == null) return ServiceResult.Failed("Store not found.");

        store.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    #endregion

    #region Store Agreements

    public async Task<List<GoodsLoanStoreAgreementDto>> GetAgreementsAsync(int? organisationId = null, int? storeId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var org = organisationId ?? await _access.GetCurrentOrganisationIdAsync();

        var query = db.GoodsLoanStoreAgreements.AsNoTracking().AsQueryable();

        if (org > 0)
        {
            query = query.Where(a => a.OrganisationBusinessEntityID == org);
        }

        if (storeId.HasValue && storeId.Value > 0)
        {
            query = query.Where(a => a.StoreID == storeId.Value);
        }

        var list = await query
            .OrderByDescending(a => a.CreatedDate)
            .Select(a => new GoodsLoanStoreAgreementDto
            {
                AgreementID = a.AgreementID,
                StoreID = a.StoreID,
                StoreName = a.Store != null ? a.Store.StoreName : "",
                StoreCode = a.Store != null ? a.Store.StoreCode : "",
                OrganisationBusinessEntityID = a.OrganisationBusinessEntityID,
                OrganisationName = "",
                AgreementNumber = a.AgreementNumber,
                EffectiveFrom = a.EffectiveFrom,
                EffectiveTo = a.EffectiveTo,
                MinimumMonths = a.MinimumMonths,
                MaximumMonths = a.MaximumMonths,
                MaximumLoanAmount = a.MaximumLoanAmount,
                InterestRate = a.InterestRate,
                SettlementMode = a.SettlementMode,
                IsActive = a.IsActive,
                CreatedDate = a.CreatedDate
            })
            .ToListAsync(ct);

        return list;
    }

    public async Task<GoodsLoanStoreAgreementDto?> GetAgreementByIdAsync(int agreementId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var a = await db.GoodsLoanStoreAgreements
            .AsNoTracking()
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.AgreementID == agreementId, ct);

        if (a == null) return null;

        return new GoodsLoanStoreAgreementDto
        {
            AgreementID = a.AgreementID,
            StoreID = a.StoreID,
            StoreName = a.Store?.StoreName ?? "",
            StoreCode = a.Store?.StoreCode ?? "",
            OrganisationBusinessEntityID = a.OrganisationBusinessEntityID,
            AgreementNumber = a.AgreementNumber,
            EffectiveFrom = a.EffectiveFrom,
            EffectiveTo = a.EffectiveTo,
            MinimumMonths = a.MinimumMonths,
            MaximumMonths = a.MaximumMonths,
            MaximumLoanAmount = a.MaximumLoanAmount,
            InterestRate = a.InterestRate,
            SettlementMode = a.SettlementMode,
            IsActive = a.IsActive,
            CreatedDate = a.CreatedDate
        };
    }

    public async Task<ServiceResult> SaveAgreementAsync(SaveGoodsLoanStoreAgreementRequest request, CancellationToken ct = default)
    {
        if (request.StoreID <= 0) return ServiceResult.Failed("Store selection is required.");
        if (request.OrganisationBusinessEntityID <= 0)
        {
            request.OrganisationBusinessEntityID = await _access.GetCurrentOrganisationIdAsync();
        }
        if (request.OrganisationBusinessEntityID <= 0) return ServiceResult.Failed("Organisation is required.");
        if (string.IsNullOrWhiteSpace(request.AgreementNumber)) return ServiceResult.Failed("Agreement number is required.");
        if (request.MinimumMonths <= 0 || request.MaximumMonths < request.MinimumMonths)
        {
            return ServiceResult.Failed("Invalid repayment duration range.");
        }
        if (request.MaximumLoanAmount <= 0) return ServiceResult.Failed("Maximum loan amount must be positive.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (request.AgreementID <= 0)
        {
            var exists = await db.GoodsLoanStoreAgreements.AnyAsync(a =>
                a.OrganisationBusinessEntityID == request.OrganisationBusinessEntityID &&
                a.AgreementNumber == request.AgreementNumber.Trim(), ct);

            if (exists) return ServiceResult.Failed($"Agreement '{request.AgreementNumber}' already exists for this organisation.");

            var agreement = new GoodsLoanStoreAgreement
            {
                StoreID = request.StoreID,
                OrganisationBusinessEntityID = request.OrganisationBusinessEntityID,
                AgreementNumber = request.AgreementNumber.Trim(),
                EffectiveFrom = request.EffectiveFrom.Date,
                EffectiveTo = request.EffectiveTo?.Date,
                MinimumMonths = request.MinimumMonths,
                MaximumMonths = request.MaximumMonths,
                MaximumLoanAmount = request.MaximumLoanAmount,
                InterestRate = request.InterestRate,
                SettlementMode = string.Equals(request.SettlementMode, GoodsLoanSettlementMode.Upfront, StringComparison.OrdinalIgnoreCase)
                    ? GoodsLoanSettlementMode.Upfront
                    : GoodsLoanSettlementMode.Monthly,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            db.GoodsLoanStoreAgreements.Add(agreement);
            await db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
        else
        {
            var agreement = await db.GoodsLoanStoreAgreements.FirstOrDefaultAsync(a => a.AgreementID == request.AgreementID, ct);
            if (agreement == null) return ServiceResult.Failed("Agreement not found.");

            agreement.StoreID = request.StoreID;
            agreement.AgreementNumber = request.AgreementNumber.Trim();
            agreement.EffectiveFrom = request.EffectiveFrom.Date;
            agreement.EffectiveTo = request.EffectiveTo?.Date;
            agreement.MinimumMonths = request.MinimumMonths;
            agreement.MaximumMonths = request.MaximumMonths;
            agreement.MaximumLoanAmount = request.MaximumLoanAmount;
            agreement.InterestRate = request.InterestRate;
            agreement.SettlementMode = string.Equals(request.SettlementMode, GoodsLoanSettlementMode.Upfront, StringComparison.OrdinalIgnoreCase)
                ? GoodsLoanSettlementMode.Upfront
                : GoodsLoanSettlementMode.Monthly;
            agreement.IsActive = request.IsActive;

            await db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
    }

    public async Task<ServiceResult> ToggleAgreementActiveAsync(int agreementId, bool isActive, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var a = await db.GoodsLoanStoreAgreements.FirstOrDefaultAsync(x => x.AgreementID == agreementId, ct);
        if (a == null) return ServiceResult.Failed("Agreement not found.");

        a.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    #endregion

    #region Deduction Policies

    public async Task<List<GoodsLoanDeductionPolicyDto>> GetPoliciesAsync(int? organisationId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var org = organisationId ?? await _access.GetCurrentOrganisationIdAsync();

        var query = db.GoodsLoanDeductionPolicies.AsNoTracking().AsQueryable();
        if (org > 0)
        {
            query = query.Where(p => p.OrganisationBusinessEntityID == org);
        }

        var list = await query
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => new GoodsLoanDeductionPolicyDto
            {
                PolicyID = p.PolicyID,
                OrganisationBusinessEntityID = p.OrganisationBusinessEntityID,
                PolicyName = p.PolicyName,
                MaximumSalaryPercentage = p.MaximumSalaryPercentage,
                MinimumNetSalary = p.MinimumNetSalary,
                MaximumActiveLoans = p.MaximumActiveLoans,
                MaximumOutstandingAmount = p.MaximumOutstandingAmount,
                SalaryBasis = p.SalaryBasis,
                EffectiveFrom = p.EffectiveFrom,
                EffectiveTo = p.EffectiveTo,
                IsActive = p.IsActive
            })
            .ToListAsync(ct);

        return list;
    }

    public async Task<GoodsLoanDeductionPolicyDto?> GetActivePolicyAsync(int organisationId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var today = DateTime.Today;

        var p = await db.GoodsLoanDeductionPolicies
            .AsNoTracking()
            .Where(x => x.OrganisationBusinessEntityID == organisationId && x.IsActive && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (p == null)
        {
            // Default fallback policy
            return new GoodsLoanDeductionPolicyDto
            {
                PolicyID = 0,
                OrganisationBusinessEntityID = organisationId,
                PolicyName = "Default Organization Policy",
                MaximumSalaryPercentage = 20.0m,
                MinimumNetSalary = 0.0m,
                MaximumActiveLoans = null,
                MaximumOutstandingAmount = null,
                SalaryBasis = SalaryCalculationBasis.BasicSalary,
                EffectiveFrom = today.AddYears(-1),
                EffectiveTo = null,
                IsActive = true
            };
        }

        return new GoodsLoanDeductionPolicyDto
        {
            PolicyID = p.PolicyID,
            OrganisationBusinessEntityID = p.OrganisationBusinessEntityID,
            PolicyName = p.PolicyName,
            MaximumSalaryPercentage = p.MaximumSalaryPercentage,
            MinimumNetSalary = p.MinimumNetSalary,
            MaximumActiveLoans = p.MaximumActiveLoans,
            MaximumOutstandingAmount = p.MaximumOutstandingAmount,
            SalaryBasis = p.SalaryBasis,
            EffectiveFrom = p.EffectiveFrom,
            EffectiveTo = p.EffectiveTo,
            IsActive = p.IsActive
        };
    }

    public async Task<ServiceResult> SavePolicyAsync(SaveGoodsLoanDeductionPolicyRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PolicyName)) return ServiceResult.Failed("Policy name is required.");
        if (request.MaximumSalaryPercentage <= 0 || request.MaximumSalaryPercentage > 100)
        {
            return ServiceResult.Failed("Maximum salary percentage must be between 1% and 100%.");
        }

        if (request.OrganisationBusinessEntityID <= 0)
        {
            request.OrganisationBusinessEntityID = await _access.GetCurrentOrganisationIdAsync();
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (request.PolicyID <= 0)
        {
            var policy = new GoodsLoanDeductionPolicy
            {
                OrganisationBusinessEntityID = request.OrganisationBusinessEntityID,
                PolicyName = request.PolicyName.Trim(),
                MaximumSalaryPercentage = request.MaximumSalaryPercentage,
                MinimumNetSalary = request.MinimumNetSalary,
                MaximumActiveLoans = request.MaximumActiveLoans,
                MaximumOutstandingAmount = request.MaximumOutstandingAmount,
                SalaryBasis = request.SalaryBasis,
                EffectiveFrom = request.EffectiveFrom.Date,
                EffectiveTo = request.EffectiveTo?.Date,
                IsActive = request.IsActive
            };

            db.GoodsLoanDeductionPolicies.Add(policy);
            await db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
        else
        {
            var policy = await db.GoodsLoanDeductionPolicies.FirstOrDefaultAsync(p => p.PolicyID == request.PolicyID, ct);
            if (policy == null) return ServiceResult.Failed("Policy not found.");

            policy.PolicyName = request.PolicyName.Trim();
            policy.MaximumSalaryPercentage = request.MaximumSalaryPercentage;
            policy.MinimumNetSalary = request.MinimumNetSalary;
            policy.MaximumActiveLoans = request.MaximumActiveLoans;
            policy.MaximumOutstandingAmount = request.MaximumOutstandingAmount;
            policy.SalaryBasis = request.SalaryBasis;
            policy.EffectiveFrom = request.EffectiveFrom.Date;
            policy.EffectiveTo = request.EffectiveTo?.Date;
            policy.IsActive = request.IsActive;

            await db.SaveChangesAsync(ct);
            return ServiceResult.Ok();
        }
    }

    #endregion

    #region Eligibility & Schedule Preview

    public async Task<GoodsLoanEligibilityResult> CheckEligibilityAsync(GoodsLoanEligibilityCheckRequest request, CancellationToken ct = default)
    {
        var result = new GoodsLoanEligibilityResult();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1. Validate employee and active job
        var job = await db.Jobs
            .AsNoTracking()
            .Include(j => j.JobState)
            .FirstOrDefaultAsync(j => j.JobId == request.JobID && j.IndividualID == request.IndividualID, ct);

        if (job == null || job.OrganisationID != request.OrganisationBusinessEntityID)
        {
            result.ValidationErrors.Add("Employee does not have an active employment job in this organisation.");
            result.IsEligible = false;
            return result;
        }

        if (job.JobStateId != SharedConfig.JobStates.APPROVED)
        {
            result.ValidationErrors.Add($"Employee job is not active (status: {job.JobState?.StateName ?? job.JobStateId.ToString()}).");
        }

        // 2. Validate store agreement
        var agreement = await db.GoodsLoanStoreAgreements
            .AsNoTracking()
            .Include(a => a.Store)
            .FirstOrDefaultAsync(a => a.AgreementID == request.AgreementID && a.OrganisationBusinessEntityID == request.OrganisationBusinessEntityID, ct);

        if (agreement == null || !agreement.IsActive)
        {
            result.ValidationErrors.Add("Selected store agreement is not active or does not belong to your organisation.");
            result.IsEligible = false;
            return result;
        }

        if (agreement.Store?.IsActive != true)
        {
            result.ValidationErrors.Add("The participating store is currently deactivated.");
        }

        if (request.RequestedMonths < agreement.MinimumMonths || request.RequestedMonths > agreement.MaximumMonths)
        {
            result.ValidationErrors.Add($"Requested duration ({request.RequestedMonths} months) must be between {agreement.MinimumMonths} and {agreement.MaximumMonths} months.");
        }

        if (request.RequestedAmount > agreement.MaximumLoanAmount)
        {
            result.ValidationErrors.Add($"Requested loan amount (MVR {request.RequestedAmount:N2}) exceeds agreement maximum limit (MVR {agreement.MaximumLoanAmount:N2}).");
        }

        // 3. Load active deduction policy
        var policy = await GetActivePolicyAsync(request.OrganisationBusinessEntityID, ct);
        result.MaximumAllowedPercentage = policy!.MaximumSalaryPercentage;
        result.SalaryBasis = policy.SalaryBasis;
        result.MinimumTakeHomeSalary = policy.MinimumNetSalary;
        result.MaximumAllowedActiveLoans = policy.MaximumActiveLoans;
        result.MaximumAllowedOutstanding = policy.MaximumOutstandingAmount;

        // 4. Resolve Employee Salary
        decimal baseSalary = 0m;
        var comp = await db.EmployeeCompensations
            .AsNoTracking()
            .Where(c => c.JobId == request.JobID && c.IsActive)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (comp != null && comp.BasicSalary > 0)
        {
            baseSalary = comp.BasicSalary;
        }
        else
        {
            baseSalary = job.BasicSalary ?? 0m;
        }

        decimal effectiveSalary = baseSalary;
        if (policy.SalaryBasis == SalaryCalculationBasis.GrossSalary)
        {
            // Add active recurring allowances
            var allowances = await db.StaffAllowances
                .AsNoTracking()
                .Where(sa => sa.JobId == request.JobID && (sa.ToDate == null || sa.ToDate >= DateTime.Today))
                .SumAsync(sa => sa.Amount, ct);
            effectiveSalary += allowances;
        }

        result.EmployeeSalary = effectiveSalary;
        if (effectiveSalary <= 0)
        {
            result.ValidationErrors.Add("No active salary information found for this employee.");
        }

        // Maximum allowed monthly commitment across all goods loans
        result.MaximumAllowedMonthlyDeduction = decimal.Round(effectiveSalary * (policy.MaximumSalaryPercentage / 100m), 2, MidpointRounding.AwayFromZero);

        // Proposed monthly instalment
        decimal proposedInstalment = request.RequestedMonths > 0
            ? decimal.Round(request.RequestedAmount / request.RequestedMonths, 2, MidpointRounding.AwayFromZero)
            : request.RequestedAmount;
        result.ProposedNewMonthlyDeduction = proposedInstalment;

        // 5. Query existing active goods loans and current commitments
        var activeLoans = await db.GoodsLoans
            .AsNoTracking()
            .Include(l => l.Installments)
            .Where(l => l.IndividualID == request.IndividualID && (l.Status == GoodsLoanStatus.Active || l.Status == GoodsLoanStatus.Approved))
            .ToListAsync(ct);

        result.CurrentActiveLoansCount = activeLoans.Count;
        if (policy.MaximumActiveLoans.HasValue && (activeLoans.Count + 1) > policy.MaximumActiveLoans.Value)
        {
            result.ValidationErrors.Add($"Request exceeds maximum active goods loans limit of {policy.MaximumActiveLoans.Value}. Current active loans: {activeLoans.Count}.");
        }

        result.CurrentTotalOutstanding = activeLoans.Sum(l => l.Installments.Where(i => i.Status != GoodsLoanInstallmentStatus.Cancelled).Sum(i => i.ScheduledAmount - i.PaidAmount));
        if (policy.MaximumOutstandingAmount.HasValue && (result.CurrentTotalOutstanding + request.RequestedAmount) > policy.MaximumOutstandingAmount.Value)
        {
            result.ValidationErrors.Add($"Request would bring total outstanding loan balance to MVR {(result.CurrentTotalOutstanding + request.RequestedAmount):N2}, exceeding the policy limit of MVR {policy.MaximumOutstandingAmount.Value:N2}.");
        }

        // 6. Multi-month forward projection across the entire proposed repayment schedule
        var startMonth = new DateTime(request.FirstDeductionMonth.Year, request.FirstDeductionMonth.Month, 1);
        var activeInstallments = activeLoans
            .SelectMany(l => l.Installments)
            .Where(i => i.Status != GoodsLoanInstallmentStatus.Cancelled && i.Status != GoodsLoanInstallmentStatus.Paid)
            .ToList();

        // Also query standard payroll deduction agreements
        var otherAgreements = await db.DeductionAgreements
            .AsNoTracking()
            .Where(a => a.IndividualId == request.IndividualID && a.OrganisationId == request.OrganisationBusinessEntityID)
            .Join(db.DeductionInstalments.Where(di => !di.IsSuperseded),
                  a => a.Id,
                  di => di.AgreementId,
                  (a, di) => di)
            .ToListAsync(ct);

        decimal maxMonthlyDeductionInWindow = 0m;
        bool capacityExceededAnyMonth = false;

        for (int m = 0; m < request.RequestedMonths; m++)
        {
            var monthDate = startMonth.AddMonths(m);
            var existingGoodsLoanDeduction = activeInstallments
                .Where(i => i.DueMonth.Year == monthDate.Year && i.DueMonth.Month == monthDate.Month)
                .Sum(i => i.ScheduledAmount - i.PaidAmount);

            var existingOtherDeduction = otherAgreements
                .Where(i => i.DueDate.Year == monthDate.Year && i.DueDate.Month == monthDate.Month)
                .Sum(i => i.Amount);

            var totalExisting = existingGoodsLoanDeduction;
            if (m == 0)
            {
                result.CurrentMonthlyGoodsLoanDeductions = existingGoodsLoanDeduction;
                result.CurrentOtherMonthlyDeductions = existingOtherDeduction;
            }

            var detail = new MonthlyCommitmentDetail
            {
                Month = monthDate,
                ExistingDeductions = totalExisting,
                ProposedDeduction = proposedInstalment,
                CapacityLimit = result.MaximumAllowedMonthlyDeduction
            };

            if (detail.ExceedsCapacity)
            {
                capacityExceededAnyMonth = true;
            }

            result.FutureMonthsBreakdown.Add(detail);
            maxMonthlyDeductionInWindow = Math.Max(maxMonthlyDeductionInWindow, detail.TotalDeduction);
        }

        if (capacityExceededAnyMonth)
        {
            result.ValidationErrors.Add($"Combined monthly goods-loan deduction exceeds permitted capacity (MVR {result.MaximumAllowedMonthlyDeduction:N2}) in one or more future repayment months.");
        }

        // 7. Check minimum take-home pay
        result.ProjectedNetSalary = effectiveSalary - maxMonthlyDeductionInWindow - result.CurrentOtherMonthlyDeductions;
        if (result.MinimumTakeHomeSalary > 0 && result.ProjectedNetSalary < result.MinimumTakeHomeSalary)
        {
            result.ValidationErrors.Add($"Projected net take-home salary (MVR {result.ProjectedNetSalary:N2}) would fall below the organisation's required minimum take-home salary (MVR {result.MinimumTakeHomeSalary:N2}).");
        }

        result.IsEligible = result.ValidationErrors.Count == 0;
        return result;
    }

    public Task<List<RepaymentScheduleInstalmentPlan>> CalculateSchedulePreviewAsync(RepaymentScheduleCalculationRequest request, CancellationToken ct = default)
    {
        var schedule = _calculator.CalculateSchedule(request.TotalAmount, request.RepaymentMonths, request.FirstDeductionMonth);
        return Task.FromResult(schedule);
    }

    #endregion

    #region Loans & Applications

    public async Task<GoodsLoanStaffWorkspaceSummary> GetStaffWorkspaceSummaryAsync(int individualId, int? organisationId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var org = organisationId ?? await _access.GetCurrentOrganisationIdAsync();

        var loans = await db.GoodsLoans
            .AsNoTracking()
            .Include(l => l.Store)
            .Include(l => l.Agreement)
            .Include(l => l.Items)
            .Include(l => l.Installments)
            .Where(l => l.IndividualID == individualId && (org <= 0 || l.OrganisationBusinessEntityID == org))
            .OrderByDescending(l => l.CreatedDate)
            .ToListAsync(ct);

        var activeLoans = loans.Where(l => l.Status == GoodsLoanStatus.Active || l.Status == GoodsLoanStatus.Approved).ToList();
        var pendingLoans = loans.Where(l => l.Status == GoodsLoanStatus.PendingApproval || l.Status == GoodsLoanStatus.Draft).ToList();
        var completedLoans = loans.Where(l => l.Status == GoodsLoanStatus.Completed).ToList();

        var activeInstallments = activeLoans
            .SelectMany(l => l.Installments)
            .Where(i => i.Status != GoodsLoanInstallmentStatus.Cancelled && i.Status != GoodsLoanInstallmentStatus.Paid)
            .OrderBy(i => i.DueMonth)
            .ToList();

        var nextInstalment = activeInstallments.FirstOrDefault();

        var policy = await GetActivePolicyAsync(org > 0 ? org : 1, ct);
        decimal employeeSalary = 0m;
        var comp = await db.EmployeeCompensations
            .AsNoTracking()
            .Where(c => c.IndividualId == individualId && c.IsActive)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (comp != null) employeeSalary = comp.BasicSalary;

        decimal maxAllowed = policy != null && employeeSalary > 0
            ? decimal.Round(employeeSalary * (policy.MaximumSalaryPercentage / 100m), 2, MidpointRounding.AwayFromZero)
            : 0m;

        decimal currentMonthly = activeInstallments
            .Where(i => i.DueMonth.Year == DateTime.Today.Year && i.DueMonth.Month == DateTime.Today.Month)
            .Sum(i => i.ScheduledAmount - i.PaidAmount);

        var summary = new GoodsLoanStaffWorkspaceSummary
        {
            ActiveLoansCount = activeLoans.Count,
            TotalOutstandingBalance = activeInstallments.Sum(i => i.ScheduledAmount - i.PaidAmount),
            CurrentMonthlyDeduction = currentMonthly,
            MaximumAllowedDeduction = maxAllowed,
            AvailableMonthlyCapacity = Math.Max(0m, maxAllowed - currentMonthly),
            NextInstalmentDate = nextInstalment?.DueMonth,
            NextInstalmentAmount = nextInstalment != null ? nextInstalment.ScheduledAmount - nextInstalment.PaidAmount : null,
            CompletedLoansCount = completedLoans.Count,
            PendingRequestsCount = pendingLoans.Count,
            ActiveLoans = activeLoans.Select(MapToDto).ToList(),
            AllLoans = loans.Select(MapToDto).ToList()
        };

        return summary;
    }

    public async Task<List<GoodsLoanDto>> GetLoansAsync(GoodsLoanFilterRequest filter, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var org = filter.OrganisationBusinessEntityID ?? await _access.GetCurrentOrganisationIdAsync();

        var query = db.GoodsLoans
            .AsNoTracking()
            .Include(l => l.Store)
            .Include(l => l.Agreement)
            .Include(l => l.Items)
            .Include(l => l.Installments)
            .AsQueryable();

        if (org > 0)
        {
            query = query.Where(l => l.OrganisationBusinessEntityID == org);
        }

        if (filter.IndividualID.HasValue && filter.IndividualID.Value > 0)
        {
            query = query.Where(l => l.IndividualID == filter.IndividualID.Value);
        }

        if (filter.JobID.HasValue && filter.JobID.Value > 0)
        {
            query = query.Where(l => l.JobID == filter.JobID.Value);
        }

        if (filter.StoreID.HasValue && filter.StoreID.Value > 0)
        {
            query = query.Where(l => l.StoreID == filter.StoreID.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(l => l.Status == filter.Status);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.PurchaseDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(l => l.PurchaseDate <= filter.ToDate.Value);
        }

        var list = await query
            .OrderByDescending(l => l.CreatedDate)
            .ToListAsync(ct);

        if (filter.OnlyOutstanding)
        {
            list = list.Where(l => l.Installments.Any(i => i.Status != GoodsLoanInstallmentStatus.Paid && i.Status != GoodsLoanInstallmentStatus.Cancelled && (i.ScheduledAmount - i.PaidAmount) > 0)).ToList();
        }

        return list.Select(MapToDto).ToList();
    }

    public async Task<GoodsLoanDto?> GetLoanByIdAsync(int loanId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var loan = await db.GoodsLoans
            .AsNoTracking()
            .Include(l => l.Store)
            .Include(l => l.Agreement)
            .Include(l => l.Items)
            .Include(l => l.Installments)
            .Include(l => l.StatusHistory)
            .Include(l => l.Documents)
            .FirstOrDefaultAsync(l => l.LoanID == loanId, ct);

        if (loan == null) return null;

        var dto = MapToDto(loan);

        // Fetch payroll deduction records linked to this loan's installments
        var instIds = loan.Installments.Select(i => i.InstallmentID).ToList();
        if (instIds.Count > 0)
        {
            var deds = await db.GoodsLoanPayrollDeductions
                .AsNoTracking()
                .Where(d => instIds.Contains(d.InstallmentID))
                .OrderByDescending(d => d.DeductionDate)
                .Select(d => new GoodsLoanPayrollDeductionDto
                {
                    DeductionID = d.DeductionID,
                    InstallmentID = d.InstallmentID,
                    PayrollRunID = d.PayrollRunID,
                    PayrollItemID = d.PayrollItemID,
                    TransactionType = d.TransactionType,
                    Amount = d.Amount,
                    Status = d.Status,
                    DeductionDate = d.DeductionDate,
                    ReversesDeductionID = d.ReversesDeductionID,
                    CreatedDate = d.CreatedDate
                })
                .ToListAsync(ct);

            dto.PayrollDeductions = deds;
        }

        return dto;
    }

    public async Task<ServiceResult> ApplyForLoanAsync(CreateGoodsLoanApplicationRequest request, CancellationToken ct = default)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return ServiceResult.Failed("At least one purchased item must be specified.");
        }

        decimal principalAmount = request.Items.Sum(x => x.TotalAmount);
        if (principalAmount <= 0)
        {
            return ServiceResult.Failed("Total loan amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
        {
            return ServiceResult.Failed("Store invoice or purchase receipt number is required.");
        }

        if (request.OrganisationBusinessEntityID <= 0)
        {
            request.OrganisationBusinessEntityID = await _access.GetCurrentOrganisationIdAsync();
        }

        // Run comprehensive eligibility check
        var eligibility = await CheckEligibilityAsync(new GoodsLoanEligibilityCheckRequest
        {
            IndividualID = request.IndividualID,
            JobID = request.JobID,
            OrganisationBusinessEntityID = request.OrganisationBusinessEntityID,
            AgreementID = request.AgreementID,
            RequestedAmount = principalAmount,
            RequestedMonths = request.RepaymentMonths,
            FirstDeductionMonth = request.FirstDeductionMonth
        }, ct);

        if (!eligibility.IsEligible)
        {
            return ServiceResult.Failed(string.Join(" ", eligibility.ValidationErrors));
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var agreement = await db.GoodsLoanStoreAgreements.AsNoTracking().FirstOrDefaultAsync(a => a.AgreementID == request.AgreementID, ct);
        if (agreement == null) return ServiceResult.Failed("Agreement not found.");

        var policy = await GetActivePolicyAsync(request.OrganisationBusinessEntityID, ct);
        var userCtx = await _access.RequireContextAsync();
        var currentUserId = userCtx.UserId > 0 ? userCtx.UserId : userCtx.IndividualId;

        // Generate Loan Number: GL-{OrgId}-{yyyyMMdd}-{Rand}
        var randomSuffix = Random.Shared.Next(1000, 9999);
        var loanNumber = $"GL-{request.OrganisationBusinessEntityID}-{DateTime.UtcNow:yyyyMMdd}-{randomSuffix}";

        // Calculate interest (if any)
        decimal totalRepayable = principalAmount + (principalAmount * (agreement.InterestRate / 100m));

        // Create standard HRM Request
        var hrmRequest = new Request
        {
            RequestTypeId = GoodsLoanConstants.GoodsLoanRequestTypeId,
            RequestStateId = SharedConfig.RequestStates.PENDING_APPROVAL,
            ApplicationDate = DateTime.UtcNow,
            ReferenceNumber = loanNumber,
            ApplicantBusinessEntityId = request.IndividualID,
            OrganisationBusinessEntityID = request.OrganisationBusinessEntityID,
            LastStateChangedByUserId = currentUserId,
            LastStateChangedDate = DateTime.UtcNow,
            OperationLogId = 1
        };

        db.Requests.Add(hrmRequest);
        await db.SaveChangesAsync(ct);

        // Create GoodsLoan
        var loan = new GoodsLoan
        {
            LoanNumber = loanNumber,
            OrganisationBusinessEntityID = request.OrganisationBusinessEntityID,
            IndividualID = request.IndividualID,
            JobID = request.JobID,
            StoreID = request.StoreID,
            AgreementID = request.AgreementID,
            PolicyID = policy?.PolicyID ?? 1,
            RequestID = hrmRequest.RequestId,
            PurchaseDate = request.PurchaseDate.Date,
            InvoiceNumber = request.InvoiceNumber.Trim(),
            PrincipalAmount = principalAmount,
            TotalRepayableAmount = totalRepayable,
            RepaymentMonths = request.RepaymentMonths,
            FirstDeductionMonth = new DateTime(request.FirstDeductionMonth.Year, request.FirstDeductionMonth.Month, 1),
            SalaryAtApproval = eligibility.EmployeeSalary,
            EligibleSalaryAtApproval = eligibility.EmployeeSalary,
            DeductionPercentageAtApproval = eligibility.MaximumAllowedPercentage,
            Status = GoodsLoanStatus.PendingApproval,
            Remarks = request.Remarks,
            CreatedByUserID = currentUserId,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            loan.Items.Add(new GoodsLoanItem
            {
                ItemDescription = item.ItemDescription.Trim(),
                Quantity = item.Quantity > 0 ? item.Quantity : 1,
                UnitPrice = item.UnitPrice,
                TotalAmount = item.TotalAmount
            });
        }

        if (request.AttachmentBytes != null && request.AttachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(request.AttachmentFileName))
        {
            loan.Documents.Add(new GoodsLoanDocument
            {
                FileName = request.AttachmentFileName,
                ContentType = request.AttachmentContentType ?? "application/pdf",
                FileSize = request.AttachmentBytes.Length,
                FileBytes = request.AttachmentBytes,
                UploadedByUserID = currentUserId,
                UploadedDate = DateTime.UtcNow
            });
        }

        loan.StatusHistory.Add(new GoodsLoanStatusHistory
        {
            PreviousStatus = GoodsLoanStatus.Draft,
            NewStatus = GoodsLoanStatus.PendingApproval,
            Reason = "Loan application submitted for review and approval.",
            ChangedByUserID = currentUserId,
            ChangedDate = DateTime.UtcNow
        });

        db.GoodsLoans.Add(loan);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Goods loan {LoanNumber} submitted successfully for employee {IndividualId}", loanNumber, request.IndividualID);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ApproveLoanAsync(int loanId, int approverUserId, string? remarks = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var loan = await db.GoodsLoans
                .Include(l => l.Installments)
                .Include(l => l.Agreement)
                .FirstOrDefaultAsync(l => l.LoanID == loanId, ct);

            if (loan == null) return ServiceResult.Failed("Loan not found.");
            if (loan.Status != GoodsLoanStatus.PendingApproval)
            {
                return ServiceResult.Failed($"Cannot approve loan in status '{loan.Status}'. Only pending loans can be approved.");
            }

            // Re-validate eligibility with transaction lock
            var eligibility = await CheckEligibilityAsync(new GoodsLoanEligibilityCheckRequest
            {
                IndividualID = loan.IndividualID,
                JobID = loan.JobID,
                OrganisationBusinessEntityID = loan.OrganisationBusinessEntityID,
                AgreementID = loan.AgreementID,
                RequestedAmount = loan.PrincipalAmount,
                RequestedMonths = loan.RepaymentMonths,
                FirstDeductionMonth = loan.FirstDeductionMonth
            }, ct);

            if (!eligibility.IsEligible)
            {
                return ServiceResult.Failed("Approval blocked: " + string.Join(" ", eligibility.ValidationErrors));
            }

            loan.Status = GoodsLoanStatus.Active;
            loan.ApprovedByUserID = approverUserId;
            loan.ApprovedDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                loan.Remarks = string.IsNullOrWhiteSpace(loan.Remarks) ? remarks : $"{loan.Remarks}\n{remarks}";
            }

            // Generate complete repayment schedule (24 instalments for 24-month loan)
            var schedule = _calculator.CalculateSchedule(loan.TotalRepayableAmount, loan.RepaymentMonths, loan.FirstDeductionMonth);

            loan.Installments.Clear();
            foreach (var inst in schedule)
            {
                loan.Installments.Add(new GoodsLoanInstallment
                {
                    InstallmentNumber = inst.InstalmentNumber,
                    DueMonth = inst.DueMonth,
                    ScheduledAmount = inst.ScheduledAmount,
                    PaidAmount = 0m,
                    Status = GoodsLoanInstallmentStatus.Pending,
                    CreatedDate = DateTime.UtcNow
                });
            }

            loan.StatusHistory.Add(new GoodsLoanStatusHistory
            {
                PreviousStatus = GoodsLoanStatus.PendingApproval,
                NewStatus = GoodsLoanStatus.Active,
                Reason = remarks ?? "Loan application approved and activated. Complete repayment schedule generated.",
                ChangedByUserID = approverUserId,
                ChangedDate = DateTime.UtcNow
            });

            // Update linked Request if present
            if (loan.RequestID.HasValue)
            {
                var req = await db.Requests.FirstOrDefaultAsync(r => r.RequestId == loan.RequestID.Value, ct);
                if (req != null)
                {
                    req.RequestStateId = SharedConfig.RequestStates.APPROVED;
                    req.LastStateChangedByUserId = approverUserId;
                    req.LastStateChangedDate = DateTime.UtcNow;
                    req.StateChangeRemarks = remarks ?? "Approved";
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Goods loan {LoanNumber} approved and activated with {Count} instalments.", loan.LoanNumber, schedule.Count);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to approve goods loan {LoanId}", loanId);
            return ServiceResult.Failed($"Approval error: {ex.Message}");
        }
    }

    public async Task<ServiceResult> RejectLoanAsync(int loanId, int rejectorUserId, string reason, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var loan = await db.GoodsLoans.FirstOrDefaultAsync(l => l.LoanID == loanId, ct);
        if (loan == null) return ServiceResult.Failed("Loan not found.");

        if (loan.Status != GoodsLoanStatus.PendingApproval)
        {
            return ServiceResult.Failed($"Cannot reject loan in status '{loan.Status}'.");
        }

        loan.Status = GoodsLoanStatus.Rejected;
        loan.StatusHistory.Add(new GoodsLoanStatusHistory
        {
            PreviousStatus = GoodsLoanStatus.PendingApproval,
            NewStatus = GoodsLoanStatus.Rejected,
            Reason = reason,
            ChangedByUserID = rejectorUserId,
            ChangedDate = DateTime.UtcNow
        });

        if (loan.RequestID.HasValue)
        {
            var req = await db.Requests.FirstOrDefaultAsync(r => r.RequestId == loan.RequestID.Value, ct);
            if (req != null)
            {
                req.RequestStateId = SharedConfig.RequestStates.REJECTED;
                req.LastStateChangedByUserId = rejectorUserId;
                req.LastStateChangedDate = DateTime.UtcNow;
                req.StateChangeRemarks = reason;
            }
        }

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelLoanAsync(int loanId, int cancellerUserId, string reason, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var loan = await db.GoodsLoans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.LoanID == loanId, ct);

        if (loan == null) return ServiceResult.Failed("Loan not found.");

        if (loan.Status == GoodsLoanStatus.Completed || loan.Status == GoodsLoanStatus.Cancelled)
        {
            return ServiceResult.Failed($"Loan is already in '{loan.Status}' status.");
        }

        // Validate no confirmed payroll payments have been processed
        bool hasConfirmedDeductions = await db.GoodsLoanPayrollDeductions
            .AnyAsync(d => loan.Installments.Select(i => i.InstallmentID).Contains(d.InstallmentID) && d.Status == GoodsLoanDeductionStatus.Confirmed, ct);

        if (hasConfirmedDeductions)
        {
            return ServiceResult.Failed("Cannot cancel loan with confirmed payroll deduction history. Please use Early Settlement instead.");
        }

        foreach (var inst in loan.Installments)
        {
            inst.Status = GoodsLoanInstallmentStatus.Cancelled;
        }

        var oldStatus = loan.Status;
        loan.Status = GoodsLoanStatus.Cancelled;
        loan.ClosedDate = DateTime.UtcNow;

        loan.StatusHistory.Add(new GoodsLoanStatusHistory
        {
            PreviousStatus = oldStatus,
            NewStatus = GoodsLoanStatus.Cancelled,
            Reason = reason,
            ChangedByUserID = cancellerUserId,
            ChangedDate = DateTime.UtcNow
        });

        if (loan.RequestID.HasValue)
        {
            var req = await db.Requests.FirstOrDefaultAsync(r => r.RequestId == loan.RequestID.Value, ct);
            if (req != null)
            {
                req.RequestStateId = SharedConfig.RequestStates.CANCELLED;
                req.LastStateChangedByUserId = cancellerUserId;
                req.LastStateChangedDate = DateTime.UtcNow;
                req.StateChangeRemarks = reason;
            }
        }

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ProcessEarlySettlementAsync(EarlyLoanSettlementRequest request, int userId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var loan = await db.GoodsLoans
            .Include(l => l.Installments)
            .FirstOrDefaultAsync(l => l.LoanID == request.LoanID, ct);

        if (loan == null) return ServiceResult.Failed("Loan not found.");
        if (loan.Status != GoodsLoanStatus.Active)
        {
            return ServiceResult.Failed($"Cannot settle loan in status '{loan.Status}'. Only active loans can be settled.");
        }

        var unpaidInstallments = loan.Installments
            .Where(i => i.Status != GoodsLoanInstallmentStatus.Paid && i.Status != GoodsLoanInstallmentStatus.Cancelled)
            .OrderBy(i => i.InstallmentNumber)
            .ToList();

        decimal totalOutstanding = unpaidInstallments.Sum(i => i.ScheduledAmount - i.PaidAmount);
        if (request.SettlementAmount <= 0) return ServiceResult.Failed("Settlement amount must be positive.");
        if (request.SettlementAmount > totalOutstanding)
        {
            return ServiceResult.Failed($"Settlement amount (MVR {request.SettlementAmount:N2}) exceeds remaining balance (MVR {totalOutstanding:N2}).");
        }

        decimal remainingToAllocate = request.SettlementAmount;
        foreach (var inst in unpaidInstallments)
        {
            if (remainingToAllocate <= 0) break;

            decimal needed = inst.ScheduledAmount - inst.PaidAmount;
            decimal allocate = Math.Min(needed, remainingToAllocate);

            inst.PaidAmount += allocate;
            inst.LastPaymentDate = DateTime.UtcNow;
            inst.Status = inst.PaidAmount >= inst.ScheduledAmount
                ? GoodsLoanInstallmentStatus.Paid
                : GoodsLoanInstallmentStatus.PartiallyPaid;

            remainingToAllocate -= allocate;
        }

        // Check if all instalments are now paid
        bool allPaid = loan.Installments.All(i => i.Status == GoodsLoanInstallmentStatus.Paid || i.Status == GoodsLoanInstallmentStatus.Cancelled);
        if (allPaid)
        {
            loan.Status = GoodsLoanStatus.Completed;
            loan.ClosedDate = DateTime.UtcNow;
        }

        loan.StatusHistory.Add(new GoodsLoanStatusHistory
        {
            PreviousStatus = GoodsLoanStatus.Active,
            NewStatus = allPaid ? GoodsLoanStatus.Completed : GoodsLoanStatus.Active,
            Reason = $"Early settlement of MVR {request.SettlementAmount:N2} processed. Ref: {request.PaymentReference}. {request.Remarks}",
            ChangedByUserID = userId,
            ChangedDate = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    #endregion

    #region Store Settlements

    public async Task<List<GoodsLoanStoreSettlementDto>> GetStoreSettlementsAsync(int organisationId, int? storeId = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.GoodsLoanStoreSettlements
            .AsNoTracking()
            .Include(s => s.Store)
            .Include(s => s.Allocations)
                .ThenInclude(a => a.Loan)
            .Where(s => s.OrganisationBusinessEntityID == organisationId);

        if (storeId.HasValue && storeId.Value > 0)
        {
            query = query.Where(s => s.StoreID == storeId.Value);
        }

        var list = await query
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(ct);

        return list.Select(s => new GoodsLoanStoreSettlementDto
        {
            SettlementID = s.SettlementID,
            OrganisationBusinessEntityID = s.OrganisationBusinessEntityID,
            StoreID = s.StoreID,
            StoreName = s.Store?.StoreName ?? "",
            SettlementNumber = s.SettlementNumber,
            PaymentDate = s.PaymentDate,
            PaymentReference = s.PaymentReference,
            TotalAmount = s.TotalAmount,
            Status = s.Status,
            CreatedDate = s.CreatedDate,
            Allocations = s.Allocations.Select(a => new GoodsLoanStoreSettlementAllocationDto
            {
                AllocationID = a.AllocationID,
                SettlementID = a.SettlementID,
                LoanID = a.LoanID,
                LoanNumber = a.Loan?.LoanNumber ?? "",
                AllocatedAmount = a.AllocatedAmount
            }).ToList()
        }).ToList();
    }

    public async Task<ServiceResult> CreateStoreSettlementBatchAsync(CreateStoreSettlementBatchRequest request, int userId, CancellationToken ct = default)
    {
        if (request.LoanIds == null || request.LoanIds.Count == 0)
        {
            return ServiceResult.Failed("Please select at least one loan to include in the settlement.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var loans = await db.GoodsLoans
            .Include(l => l.Agreement)
            .Where(l => request.LoanIds.Contains(l.LoanID) && l.StoreID == request.StoreID && l.OrganisationBusinessEntityID == request.OrganisationBusinessEntityID)
            .ToListAsync(ct);

        if (loans.Count == 0) return ServiceResult.Failed("No matching loans found for this store and organisation.");

        decimal totalSettlement = loans.Sum(l => l.PrincipalAmount);
        var settlementNumber = string.IsNullOrWhiteSpace(request.SettlementNumber)
            ? $"SETTLE-{request.StoreID}-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}"
            : request.SettlementNumber.Trim();

        var settlement = new GoodsLoanStoreSettlement
        {
            OrganisationBusinessEntityID = request.OrganisationBusinessEntityID,
            StoreID = request.StoreID,
            SettlementNumber = settlementNumber,
            PaymentDate = request.PaymentDate.Date,
            PaymentReference = request.PaymentReference.Trim(),
            TotalAmount = totalSettlement,
            Status = GoodsLoanSettlementStatus.Draft,
            CreatedByUserID = userId,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var loan in loans)
        {
            settlement.Allocations.Add(new GoodsLoanStoreSettlementAllocation
            {
                LoanID = loan.LoanID,
                AllocatedAmount = loan.PrincipalAmount
            });
        }

        db.GoodsLoanStoreSettlements.Add(settlement);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ApproveStoreSettlementAsync(int settlementID, int userId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var s = await db.GoodsLoanStoreSettlements.FirstOrDefaultAsync(x => x.SettlementID == settlementID, ct);
        if (s == null) return ServiceResult.Failed("Settlement not found.");

        if (s.Status != GoodsLoanSettlementStatus.Draft)
        {
            return ServiceResult.Failed($"Cannot approve settlement in status '{s.Status}'.");
        }

        s.Status = GoodsLoanSettlementStatus.Approved;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ConfirmStoreSettlementPaidAsync(int settlementID, int userId, string paymentReference, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var s = await db.GoodsLoanStoreSettlements.FirstOrDefaultAsync(x => x.SettlementID == settlementID, ct);
        if (s == null) return ServiceResult.Failed("Settlement not found.");

        s.Status = GoodsLoanSettlementStatus.Paid;
        s.PaymentReference = paymentReference;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    #endregion

    #region Reports

    public async Task<GoodsLoanDto?> GetEmployeeLoanStatementAsync(int loanId, CancellationToken ct = default)
    {
        return await GetLoanByIdAsync(loanId, ct);
    }

    public async Task<List<GoodsLoanDto>> GetStoreMonthlyDeductionsReportAsync(int organisationId, DateTime month, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var targetMonth = new DateTime(month.Year, month.Month, 1);

        var loans = await db.GoodsLoans
            .AsNoTracking()
            .Include(l => l.Store)
            .Include(l => l.Agreement)
            .Include(l => l.Installments)
            .Where(l => l.OrganisationBusinessEntityID == organisationId &&
                        l.Installments.Any(i => i.DueMonth.Year == targetMonth.Year && i.DueMonth.Month == targetMonth.Month))
            .OrderBy(l => l.Store != null ? l.Store.StoreName : "")
            .ThenBy(l => l.LoanNumber)
            .ToListAsync(ct);

        return loans.Select(MapToDto).ToList();
    }

    public async Task<List<GoodsLoanDto>> GetOutstandingLoansReportAsync(int organisationId, CancellationToken ct = default)
    {
        return await GetLoansAsync(new GoodsLoanFilterRequest
        {
            OrganisationBusinessEntityID = organisationId,
            OnlyOutstanding = true
        }, ct);
    }

    #endregion

    private static GoodsLoanDto MapToDto(GoodsLoan l)
    {
        var totalPaid = l.Installments.Sum(i => i.PaidAmount);
        var regularAmount = l.Installments.FirstOrDefault()?.ScheduledAmount ?? (l.RepaymentMonths > 0 ? l.TotalRepayableAmount / l.RepaymentMonths : 0m);

        return new GoodsLoanDto
        {
            LoanID = l.LoanID,
            LoanNumber = l.LoanNumber,
            OrganisationBusinessEntityID = l.OrganisationBusinessEntityID,
            IndividualID = l.IndividualID,
            JobID = l.JobID,
            StoreID = l.StoreID,
            StoreName = l.Store?.StoreName ?? "",
            AgreementID = l.AgreementID,
            AgreementNumber = l.Agreement?.AgreementNumber ?? "",
            SettlementMode = l.Agreement?.SettlementMode ?? "MONTHLY",
            PolicyID = l.PolicyID,
            RequestID = l.RequestID,
            WorkflowRequestInstanceID = l.WorkflowRequestInstanceID,
            PurchaseDate = l.PurchaseDate,
            InvoiceNumber = l.InvoiceNumber,
            PrincipalAmount = l.PrincipalAmount,
            TotalRepayableAmount = l.TotalRepayableAmount,
            RepaymentMonths = l.RepaymentMonths,
            FirstDeductionMonth = l.FirstDeductionMonth,
            MonthlyInstalmentAmount = regularAmount,
            TotalPaidAmount = totalPaid,
            SalaryAtApproval = l.SalaryAtApproval,
            EligibleSalaryAtApproval = l.EligibleSalaryAtApproval,
            DeductionPercentageAtApproval = l.DeductionPercentageAtApproval,
            Status = l.Status,
            ApprovedByUserID = l.ApprovedByUserID,
            ApprovedDate = l.ApprovedDate,
            ClosedDate = l.ClosedDate,
            Remarks = l.Remarks,
            CreatedDate = l.CreatedDate,
            Items = l.Items.Select(i => new GoodsLoanItemDto
            {
                LoanItemID = i.LoanItemID,
                LoanID = i.LoanID,
                ItemDescription = i.ItemDescription,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalAmount = i.TotalAmount
            }).ToList(),
            Installments = l.Installments.OrderBy(i => i.InstallmentNumber).Select(i => new GoodsLoanInstallmentDto
            {
                InstallmentID = i.InstallmentID,
                LoanID = i.LoanID,
                InstallmentNumber = i.InstallmentNumber,
                DueMonth = i.DueMonth,
                ScheduledAmount = i.ScheduledAmount,
                PaidAmount = i.PaidAmount,
                Status = i.Status,
                LastPaymentDate = i.LastPaymentDate
            }).ToList(),
            StatusHistory = l.StatusHistory.OrderByDescending(h => h.ChangedDate).Select(h => new GoodsLoanStatusHistoryDto
            {
                HistoryID = h.HistoryID,
                LoanID = h.LoanID,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                Reason = h.Reason,
                ChangedByUserID = h.ChangedByUserID,
                ChangedDate = h.ChangedDate
            }).ToList(),
            Documents = l.Documents.Select(d => new GoodsLoanDocumentDto
            {
                DocumentID = d.DocumentID,
                LoanID = d.LoanID,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                UploadedDate = d.UploadedDate
            }).ToList()
        };
    }
}
