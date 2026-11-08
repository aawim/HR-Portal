using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.DTOs.Budget;
using HRM.Application.Interfaces.Budget;
using HRM.Models;
using HRM.Models.Budget;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class BudgetService(IDbContextFactory<HrmTeContext> factory) : IBudgetService
{
    public async Task<BudgetSummaryDto> GetBudgetSummaryAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var fy = await db.FiscalYears.AsNoTracking().FirstOrDefaultAsync(x => x.YearNumber == fiscalYear, ct);
        var org = await db.Organisations.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessEntityID == organisationId, ct);
        var orgName = org?.OrganisationName ?? "National Centre for Information Technology";

        var allocations = await GetAllocationsAsync(fiscalYear, organisationId, ct);

        var totalApproved = allocations.Sum(x => x.ApprovedAmount);
        var totalVirementsIn = allocations.Sum(x => x.VirementInAmount);
        var totalVirementsOut = allocations.Sum(x => x.VirementOutAmount);
        var totalCommitted = allocations.Sum(x => x.CommittedAmount);
        var totalExpended = allocations.Sum(x => x.ExpendedAmount);
        var totalRevised = totalApproved + (totalVirementsIn - totalVirementsOut);
        var totalAvailable = totalRevised - totalCommitted - totalExpended;
        var overallBurnRate = totalRevised > 0 ? Math.Round((totalExpended / totalRevised) * 100m, 2) : 0;

        return new BudgetSummaryDto
        {
            FiscalYear = fiscalYear,
            OrganisationName = orgName,
            TotalApprovedBudget = totalApproved,
            TotalRevisedBudget = totalRevised,
            TotalVirementsIn = totalVirementsIn,
            TotalVirementsOut = totalVirementsOut,
            TotalCommitted = totalCommitted,
            TotalExpended = totalExpended,
            TotalAvailableBalance = totalAvailable,
            OverallBurnRatePercentage = overallBurnRate,
            Allocations = allocations
        };
    }

    public async Task<List<BudgetAllocationDto>> GetAllocationsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var fy = await db.FiscalYears.AsNoTracking().FirstOrDefaultAsync(x => x.YearNumber == fiscalYear, ct);
        if (fy == null) return new List<BudgetAllocationDto>();

        var raw = await db.BudgetAllocations
            .AsNoTracking()
            .Include(x => x.BudgetItem)
            .Where(x => x.FiscalYearId == fy.FiscalYearId && x.OrganisationId == organisationId)
            .ToListAsync(ct);

        var org = await db.Organisations.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessEntityID == organisationId, ct);
        var orgName = org?.OrganisationName ?? "Organisation";

        return raw.Select(x => new BudgetAllocationDto
        {
            AllocationId = x.AllocationId,
            FiscalYearId = x.FiscalYearId,
            YearNumber = fiscalYear,
            OrganisationId = x.OrganisationId,
            OrganisationName = orgName,
            BudgetItemId = x.BudgetItemId,
            EconomicCode = x.BudgetItem?.EconomicCode ?? (x.BudgetItem?.Code?.ToString() ?? string.Empty),
            DhivehiName = x.BudgetItem?.DhivehiName ?? string.Empty,
            EnglishName = x.BudgetItem?.EnglishName ?? string.Empty,
            ChapterCode = x.BudgetItem?.ChapterCode ?? "21",
            ApprovedAmount = x.ApprovedAmount,
            RevisedAmount = x.RevisedAmount > 0 ? x.RevisedAmount : x.ApprovedAmount,
            VirementInAmount = x.VirementInAmount,
            VirementOutAmount = x.VirementOutAmount,
            CommittedAmount = x.CommittedAmount,
            ExpendedAmount = x.ExpendedAmount,
            MoFApprovalReference = x.MoFApprovalReference,
            Status = x.Status
        }).OrderBy(x => x.EconomicCode).ToList();
    }

    public async Task<List<BudgetItemDto>> GetBudgetItemsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var raw = await db.BudgetItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        return raw.Select(x => new BudgetItemDto
        {
            BudgetItemId = x.BudgetItemID,
            EconomicCode = x.EconomicCode ?? (x.Code?.ToString() ?? string.Empty),
            DhivehiName = x.DhivehiName,
            EnglishName = x.EnglishName ?? string.Empty,
            ChapterCode = x.ChapterCode
        }).ToList();
    }

    public async Task<BudgetItemDto> SaveBudgetItemAsync(CreateBudgetItemRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (string.IsNullOrWhiteSpace(request.EconomicCode) || string.IsNullOrWhiteSpace(request.DhivehiName))
            throw new InvalidOperationException("Economic code and item name are required.");
        if (await db.BudgetItems.AnyAsync(x => x.EconomicCode == request.EconomicCode.Trim(), ct))
            throw new InvalidOperationException("This economic code already exists.");
        int.TryParse(request.EconomicCode, out var codeInt);
        var item = new BudgetItem
        {
            Code = codeInt > 0 ? codeInt : null,
            EconomicCode = request.EconomicCode.Trim(),
            DhivehiName = request.DhivehiName.Trim(),
            EnglishName = request.EnglishName.Trim(),
            ChapterCode = request.ChapterCode,
            AccountType = request.AccountType,
            ParentBudgetItemID = request.ParentBudgetItemId,
            IsActive = true,
            BudgetYear = DateTime.UtcNow.Year
        };
        db.BudgetItems.Add(item);
        await db.SaveChangesAsync(ct);
        return new BudgetItemDto
        {
            BudgetItemId = item.BudgetItemID,
            EconomicCode = item.EconomicCode,
            DhivehiName = item.DhivehiName,
            EnglishName = item.EnglishName,
            ChapterCode = item.ChapterCode
        };
    }

    public async Task<List<FiscalYearDto>> GetFiscalYearsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var raw = await db.FiscalYears.AsNoTracking().OrderByDescending(x => x.YearNumber).ToListAsync(ct);
        return raw.Select(x => new FiscalYearDto
        {
            FiscalYearId = x.FiscalYearId,
            YearNumber = x.YearNumber,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            TotalApprovedBudget = x.TotalApprovedBudget,
            TotalEmolumentsBudget = x.TotalEmolumentsBudget,
            Status = x.Status
        }).ToList();
    }

    public async Task<FiscalYearDto> SaveFiscalYearAsync(CreateFiscalYearRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (request.YearNumber < 2000 || request.YearNumber > 2100 || request.EndDate < request.StartDate ||
            request.TotalApprovedBudget < 0 || request.TotalEmolumentsBudget < 0 ||
            request.TotalEmolumentsBudget > request.TotalApprovedBudget)
            throw new InvalidOperationException("Enter a valid fiscal year, date range and non-negative budget totals.");
        if (await db.FiscalYears.AnyAsync(x => x.YearNumber == request.YearNumber, ct))
            throw new InvalidOperationException("This fiscal year already exists.");
        var fy = new FiscalYear
        {
            YearNumber = request.YearNumber,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalApprovedBudget = request.TotalApprovedBudget,
            TotalEmolumentsBudget = request.TotalEmolumentsBudget,
            Status = "Active",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.FiscalYears.Add(fy);
        await db.SaveChangesAsync(ct);
        return new FiscalYearDto
        {
            FiscalYearId = fy.FiscalYearId,
            YearNumber = fy.YearNumber,
            StartDate = fy.StartDate,
            EndDate = fy.EndDate,
            TotalApprovedBudget = fy.TotalApprovedBudget,
            TotalEmolumentsBudget = fy.TotalEmolumentsBudget,
            Status = fy.Status
        };
    }

    public async Task<PayrollBudgetMappingDto> SavePayrollMappingAsync(CreatePayrollMappingRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (request.BudgetItemId <= 0 || string.IsNullOrWhiteSpace(request.ComponentName) ||
            (request.SalaryComponentId.HasValue && request.AllowanceTypeId.HasValue))
            throw new InvalidOperationException("Enter a component name and select no more than one salary component or allowance type.");
        if (!await db.BudgetItems.AnyAsync(x => x.BudgetItemID == request.BudgetItemId && x.IsActive, ct))
            throw new InvalidOperationException("The selected budget item is unavailable.");
        if (await db.PayrollBudgetMappings.AnyAsync(x => x.IsActive && x.BudgetItemId == request.BudgetItemId &&
            x.SalaryComponentId == request.SalaryComponentId && x.AllowanceTypeId == request.AllowanceTypeId &&
            x.CostCenterOrganisationId == request.CostCenterOrganisationId, ct))
            throw new InvalidOperationException("This payroll budget mapping already exists.");
        var mapping = new PayrollBudgetMapping
        {
            BudgetItemId = request.BudgetItemId,
            ComponentName = request.ComponentName.Trim(),
            SalaryComponentId = request.SalaryComponentId,
            AllowanceTypeId = request.AllowanceTypeId,
            CostCenterOrganisationId = request.CostCenterOrganisationId,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.PayrollBudgetMappings.Add(mapping);
        await db.SaveChangesAsync(ct);
        var item = await db.BudgetItems.FindAsync(new object[] { request.BudgetItemId }, ct);
        return new PayrollBudgetMappingDto
        {
            MappingId = mapping.MappingId,
            BudgetItemId = mapping.BudgetItemId,
            EconomicCode = item?.EconomicCode ?? item?.Code?.ToString() ?? "",
            BudgetItemName = item?.DhivehiName ?? "",
            ComponentName = mapping.ComponentName,
            SalaryComponentId = mapping.SalaryComponentId,
            AllowanceTypeId = mapping.AllowanceTypeId,
            CostCenterOrganisationId = mapping.CostCenterOrganisationId,
            IsActive = true
        };
    }

    public async Task<BudgetTransactionDto> RecordTransactionAsync(CreateBudgetTransactionRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (request.Amount <= 0) throw new InvalidOperationException("Transaction amount must be greater than zero.");
        var type = await db.BudgetTransactionTypes.FindAsync(new object[] { request.TransactionTypeId }, ct)
            ?? throw new InvalidOperationException("Transaction type is unavailable.");
        if (type.TypeName != "ActualExpenditure")
            throw new InvalidOperationException("Manual posting is limited to actual expenditure transactions.");
        var alloc = await db.BudgetAllocations.FirstOrDefaultAsync(x => x.BudgetItemId == request.BudgetItemId && x.FiscalYearId == request.FiscalYearId && x.OrganisationId == request.OrganisationId, ct)
            ?? throw new InvalidOperationException("A matching budget allocation is required.");
        if (alloc.AvailableBalance < request.Amount)
            throw new InvalidOperationException("The transaction exceeds the available budget balance.");
        var txn = new BudgetTransaction
        {
            BudgetItemID = request.BudgetItemId,
            FiscalYearID = request.FiscalYearId,
            OrganisationID = request.OrganisationId,
            TransactionTypeID = request.TransactionTypeId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? $"TXN-{DateTime.UtcNow.Ticks % 1000000:D6}" : request.ReferenceNumber,
            BandeyriReferenceNumber = request.BandeyriReferenceNumber,
            Details = request.Details,
            Amount = request.Amount,
            SourceDocumentType = "ManualEntry",
            Status = "Posted",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.BudgetTransactions.Add(txn);

        alloc.ExpendedAmount += request.Amount;
        alloc.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        var item = await db.BudgetItems.FindAsync(new object[] { request.BudgetItemId }, ct);
        return new BudgetTransactionDto
        {
            BudgetTransactionID = txn.BudgetTransactionID,
            BudgetItemID = txn.BudgetItemID,
            EconomicCode = item?.EconomicCode ?? item?.Code?.ToString() ?? "",
            ItemName = item?.DhivehiName ?? "",
            OrganisationID = txn.OrganisationID,
            TransactionTypeID = txn.TransactionTypeID,
            TransactionTypeName = type?.DhivehiTypeName ?? type?.TypeName ?? "Transaction",
            Date = txn.Date,
            ReferenceNumber = txn.ReferenceNumber,
            BandeyriReferenceNumber = txn.BandeyriReferenceNumber,
            Details = txn.Details,
            Amount = txn.Amount,
            SourceDocumentType = txn.SourceDocumentType,
            Status = txn.Status
        };
    }

    public async Task<BudgetAllocationDto> SaveAllocationAsync(CreateBudgetAllocationRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (request.ApprovedAmount < 0) throw new InvalidOperationException("Approved amount cannot be negative.");
        var fiscalYear = await db.FiscalYears.AsNoTracking().SingleOrDefaultAsync(x => x.FiscalYearId == request.FiscalYearId, ct)
            ?? throw new InvalidOperationException("Fiscal year not found.");
        if (!await db.BudgetItems.AnyAsync(x => x.BudgetItemID == request.BudgetItemId && x.IsActive, ct))
            throw new InvalidOperationException("Budget item not found.");

        var existing = await db.BudgetAllocations.FirstOrDefaultAsync(x =>
            x.FiscalYearId == request.FiscalYearId &&
            x.OrganisationId == request.OrganisationId &&
            x.BudgetItemId == request.BudgetItemId, ct);

        if (existing != null)
        {
            existing.ApprovedAmount = request.ApprovedAmount;
            existing.RevisedAmount = request.ApprovedAmount + (existing.VirementInAmount - existing.VirementOutAmount);
            existing.MoFApprovalReference = request.MoFApprovalReference;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            existing = new BudgetAllocation
            {
                FiscalYearId = request.FiscalYearId,
                OrganisationId = request.OrganisationId,
                BudgetItemId = request.BudgetItemId,
                ApprovedAmount = request.ApprovedAmount,
                RevisedAmount = request.ApprovedAmount,
                MoFApprovalReference = request.MoFApprovalReference,
                Status = "Approved",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            db.BudgetAllocations.Add(existing);
        }

        await db.SaveChangesAsync(ct);
        var items = await GetAllocationsAsync(fiscalYear.YearNumber, request.OrganisationId, ct);
        return items.First(x => x.BudgetItemId == request.BudgetItemId);
    }

    public async Task<List<BudgetVirementDto>> GetVirementsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var fy = await db.FiscalYears.AsNoTracking().FirstOrDefaultAsync(x => x.YearNumber == fiscalYear, ct);
        if (fy == null) return new List<BudgetVirementDto>();

        var raw = await db.BudgetVirements
            .AsNoTracking()
            .Include(x => x.FromBudgetItem)
            .Include(x => x.ToBudgetItem)
            .Where(x => x.FiscalYearId == fy.FiscalYearId && x.OrganisationId == organisationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        return raw.Select(x => new BudgetVirementDto
        {
            VirementId = x.VirementId,
            VirementNumber = x.VirementNumber,
            FiscalYearId = x.FiscalYearId,
            YearNumber = fiscalYear,
            OrganisationId = x.OrganisationId,
            FromBudgetItemId = x.FromBudgetItemId,
            FromEconomicCode = x.FromBudgetItem?.EconomicCode ?? (x.FromBudgetItem?.Code?.ToString() ?? string.Empty),
            FromItemName = x.FromBudgetItem?.DhivehiName ?? string.Empty,
            ToBudgetItemId = x.ToBudgetItemId,
            ToEconomicCode = x.ToBudgetItem?.EconomicCode ?? (x.ToBudgetItem?.Code?.ToString() ?? string.Empty),
            ToItemName = x.ToBudgetItem?.DhivehiName ?? string.Empty,
            Amount = x.Amount,
            Justification = x.Justification,
            LegalBasis = x.LegalBasis,
            MoFReferenceNumber = x.MoFReferenceNumber,
            MoFApprovedDate = x.MoFApprovedDate,
            Status = x.Status,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();
    }

    public async Task<BudgetVirementDto> RequestVirementAsync(CreateBudgetVirementRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (request.Amount <= 0) throw new InvalidOperationException("Virement amount must be greater than zero.");
        if (request.FromBudgetItemId == request.ToBudgetItemId) throw new InvalidOperationException("Source and target budget items must be different.");
        if (string.IsNullOrWhiteSpace(request.Justification) || string.IsNullOrWhiteSpace(request.LegalBasis))
            throw new InvalidOperationException("Justification and legal basis are required.");
        var fiscalYear = await db.FiscalYears.AsNoTracking().SingleOrDefaultAsync(x => x.FiscalYearId == request.FiscalYearId, ct)
            ?? throw new InvalidOperationException("Fiscal year not found.");

        // Law 3/2006 Rule: Validate from allocation has enough available balance
        var fromAlloc = await db.BudgetAllocations.FirstOrDefaultAsync(x =>
            x.FiscalYearId == request.FiscalYearId &&
            x.OrganisationId == request.OrganisationId &&
            x.BudgetItemId == request.FromBudgetItemId, ct);

        if (fromAlloc == null)
            throw new InvalidOperationException("Source budget allocation does not exist.");

        if (fromAlloc.AvailableBalance < request.Amount)
            throw new InvalidOperationException($"Insufficient balance in source budget vote. Available: MVR {fromAlloc.AvailableBalance:N2}, Requested: MVR {request.Amount:N2}");

        var toAlloc = await db.BudgetAllocations.FirstOrDefaultAsync(x =>
            x.FiscalYearId == request.FiscalYearId &&
            x.OrganisationId == request.OrganisationId &&
            x.BudgetItemId == request.ToBudgetItemId, ct);

        if (toAlloc == null)
            throw new InvalidOperationException("Target budget allocation does not exist.");

        var count = await db.BudgetVirements.CountAsync(x => x.FiscalYearId == request.FiscalYearId, ct) + 1;
        var virementNumber = $"VIR-{fiscalYear.YearNumber}-{count:D4}";

        var virement = new BudgetVirement
        {
            FiscalYearId = request.FiscalYearId,
            OrganisationId = request.OrganisationId,
            VirementNumber = virementNumber,
            FromBudgetItemId = request.FromBudgetItemId,
            ToBudgetItemId = request.ToBudgetItemId,
            Amount = request.Amount,
            Justification = request.Justification,
            LegalBasis = request.LegalBasis,
            MoFReferenceNumber = request.MoFReferenceNumber,
            MoFApprovedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Approved",
            ApprovedAt = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.BudgetVirements.Add(virement);

        // Update allocations
        fromAlloc.VirementOutAmount += request.Amount;
        fromAlloc.RevisedAmount = fromAlloc.ApprovedAmount + (fromAlloc.VirementInAmount - fromAlloc.VirementOutAmount);
        fromAlloc.UpdatedAtUtc = DateTime.UtcNow;

        toAlloc.VirementInAmount += request.Amount;
        toAlloc.RevisedAmount = toAlloc.ApprovedAmount + (toAlloc.VirementInAmount - toAlloc.VirementOutAmount);
        toAlloc.UpdatedAtUtc = DateTime.UtcNow;

        // Record transactions
        var dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
        db.BudgetTransactions.Add(new BudgetTransaction
        {
            BudgetItemID = request.FromBudgetItemId,
            FiscalYearID = request.FiscalYearId,
            OrganisationID = request.OrganisationId,
            TransactionTypeID = 3, // VirementOut
            Date = dateNow,
            ReferenceNumber = virementNumber,
            BandeyriReferenceNumber = request.MoFReferenceNumber,
            Details = $"Virement Out to Item #{request.ToBudgetItemId}: {request.Justification}",
            Amount = request.Amount,
            SourceDocumentType = "Virement",
            Status = "Posted",
            CreatedAtUtc = DateTime.UtcNow
        });

        db.BudgetTransactions.Add(new BudgetTransaction
        {
            BudgetItemID = request.ToBudgetItemId,
            FiscalYearID = request.FiscalYearId,
            OrganisationID = request.OrganisationId,
            TransactionTypeID = 2, // VirementIn
            Date = dateNow,
            ReferenceNumber = virementNumber,
            BandeyriReferenceNumber = request.MoFReferenceNumber,
            Details = $"Virement In from Item #{request.FromBudgetItemId}: {request.Justification}",
            Amount = request.Amount,
            SourceDocumentType = "Virement",
            Status = "Posted",
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var list = await GetVirementsAsync(fiscalYear.YearNumber, request.OrganisationId, ct);
        return list.First(x => x.VirementNumber == virementNumber);
    }

    public async Task<List<BudgetCommitmentDto>> GetCommitmentsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var fiscalYearId = await db.FiscalYears.AsNoTracking().Where(x => x.YearNumber == fiscalYear).Select(x => (int?)x.FiscalYearId).SingleOrDefaultAsync(ct);
        if (!fiscalYearId.HasValue) return [];

        var raw = await db.BudgetCommitments
            .AsNoTracking()
            .Include(x => x.BudgetItem)
            .Where(x => x.OrganisationId == organisationId && x.FiscalYearId == fiscalYearId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        return raw.Select(x => new BudgetCommitmentDto
        {
            CommitmentId = x.CommitmentId,
            CommitmentNumber = x.CommitmentNumber,
            FiscalYearId = x.FiscalYearId,
            OrganisationId = x.OrganisationId,
            BudgetItemId = x.BudgetItemId,
            EconomicCode = x.BudgetItem?.EconomicCode ?? (x.BudgetItem?.Code?.ToString() ?? string.Empty),
            ItemName = x.BudgetItem?.DhivehiName ?? string.Empty,
            CommitmentType = x.CommitmentType,
            SourceReferenceId = x.SourceReferenceId,
            Amount = x.Amount,
            Status = x.Status,
            ExpiresAt = x.ExpiresAt,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();
    }

    public async Task<List<BudgetTransactionDto>> GetTransactionsAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var fiscalYearId = await db.FiscalYears.AsNoTracking().Where(x => x.YearNumber == fiscalYear).Select(x => (int?)x.FiscalYearId).SingleOrDefaultAsync(ct);
        if (!fiscalYearId.HasValue) return [];

        var raw = await db.BudgetTransactions
            .AsNoTracking()
            .Include(x => x.BudgetItem)
            .Include(x => x.TransactionType)
            .Where(x => x.OrganisationID == organisationId && x.FiscalYearID == fiscalYearId.Value)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.BudgetTransactionID)
            .ToListAsync(ct);

        return raw.Select(x => new BudgetTransactionDto
        {
            BudgetTransactionID = x.BudgetTransactionID,
            BudgetItemID = x.BudgetItemID,
            EconomicCode = x.BudgetItem?.EconomicCode ?? (x.BudgetItem?.Code?.ToString() ?? string.Empty),
            ItemName = x.BudgetItem?.DhivehiName ?? string.Empty,
            OrganisationID = x.OrganisationID,
            TransactionTypeID = x.TransactionTypeID,
            TransactionTypeName = x.TransactionType?.DhivehiTypeName ?? (x.TransactionType?.TypeName ?? "Transaction"),
            Date = x.Date,
            ReferenceNumber = x.ReferenceNumber,
            BandeyriReferenceNumber = x.BandeyriReferenceNumber,
            Details = x.Details,
            Amount = x.Amount,
            SourceDocumentType = x.SourceDocumentType,
            Status = x.Status
        }).ToList();
    }

    public async Task<List<PayrollBudgetMappingDto>> GetPayrollMappingsAsync(int organisationId = 0, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var raw = await db.PayrollBudgetMappings
            .AsNoTracking()
            .Include(x => x.BudgetItem)
            .Where(x => x.IsActive && (x.CostCenterOrganisationId == null || x.CostCenterOrganisationId == organisationId))
            .ToListAsync(ct);

        return raw.Select(x => new PayrollBudgetMappingDto
        {
            MappingId = x.MappingId,
            SalaryComponentId = x.SalaryComponentId,
            AllowanceTypeId = x.AllowanceTypeId,
            BudgetItemId = x.BudgetItemId,
            EconomicCode = x.BudgetItem?.EconomicCode ?? (x.BudgetItem?.Code?.ToString() ?? string.Empty),
            BudgetItemName = x.BudgetItem?.DhivehiName ?? string.Empty,
            CostCenterOrganisationId = x.CostCenterOrganisationId,
            ComponentName = x.ComponentName,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<PayrollBudgetValidationResult> ValidatePayrollRunBudgetAsync(int payrollRunId, int organisationId = 0, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var run = await db.PayrollRuns.AsNoTracking().Include(x => x.PayrollPeriod)
            .SingleOrDefaultAsync(x => x.PayrollRunId == payrollRunId && x.OrganisationId == organisationId, ct)
            ?? throw new InvalidOperationException("Payroll run not found in this organisation.");
        var fiscalYear = run.PayrollPeriod?.PeriodYear
            ?? throw new InvalidOperationException("Payroll run has no payroll period.");
        var allocations = await GetAllocationsAsync(fiscalYear, organisationId, ct);
        var allocationByItem = allocations.ToDictionary(x => x.BudgetItemId);
        var mappings = await GetPayrollMappingsAsync(organisationId, ct);
        var salaryComponentCodes = await db.SalaryComponents.AsNoTracking()
            .Where(x => x.OrganisationId == organisationId && x.IsActive)
            .ToDictionaryAsync(x => x.SalaryComponentId, x => x.ComponentCode, ct);
        var payrollLines = await db.PayrollLines.AsNoTracking()
            .Where(x => x.PayrollRunId == payrollRunId && x.CalculatedAmount > 0)
            .ToListAsync(ct);

        static bool IsEmployerCost(SalaryComponentType type) => type is
            SalaryComponentType.BasicSalary or SalaryComponentType.FixedAllowance or SalaryComponentType.VariableAllowance or
            SalaryComponentType.DutyAllowance or SalaryComponentType.ShiftAllowance or SalaryComponentType.Overtime or
            SalaryComponentType.Bonus or SalaryComponentType.Reimbursement or SalaryComponentType.EmployerPension or
            SalaryComponentType.EmployerStatutory;

        var mappedAmounts = new Dictionary<int, decimal>();
        var mappedNames = new Dictionary<int, HashSet<string>>();
        var unmapped = new List<PayrollLine>();
        foreach (var line in payrollLines.Where(x => IsEmployerCost(x.ComponentType)))
        {
            var mapping = mappings.FirstOrDefault(m =>
                (m.SalaryComponentId.HasValue && salaryComponentCodes.GetValueOrDefault(m.SalaryComponentId.Value) == line.ComponentCode) ||
                (m.AllowanceTypeId.HasValue && line.ComponentCode == $"ALLW_{m.AllowanceTypeId.Value}") ||
                (!m.SalaryComponentId.HasValue && !m.AllowanceTypeId.HasValue &&
                 (string.Equals(m.ComponentName, line.ComponentCode, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(m.ComponentName, line.ComponentName, StringComparison.OrdinalIgnoreCase))));
            if (mapping == null)
            {
                unmapped.Add(line);
                continue;
            }
            mappedAmounts[mapping.BudgetItemId] = mappedAmounts.GetValueOrDefault(mapping.BudgetItemId) + line.CalculatedAmount;
            if (!mappedNames.TryGetValue(mapping.BudgetItemId, out var names)) mappedNames[mapping.BudgetItemId] = names = [];
            names.Add(line.ComponentName);
        }

        var checks = mappedAmounts.Select(pair =>
        {
            allocationByItem.TryGetValue(pair.Key, out var allocation);
            return new PayrollBudgetLineCheck
            {
                BudgetItemId = pair.Key,
                EconomicCode = allocation?.EconomicCode ?? "UNALLOCATED",
                ComponentName = string.Join(", ", mappedNames[pair.Key].OrderBy(x => x)),
                RequiredAmount = pair.Value,
                AvailableBalance = allocation?.AvailableBalance ?? 0m
            };
        }).ToList();
        checks.AddRange(unmapped.GroupBy(x => new { x.ComponentCode, x.ComponentName }).Select(group => new PayrollBudgetLineCheck
        {
            BudgetItemId = 0,
            EconomicCode = "UNMAPPED",
            ComponentName = $"{group.Key.ComponentCode} - {group.Key.ComponentName}",
            RequiredAmount = group.Sum(x => x.CalculatedAmount),
            AvailableBalance = 0m
        }));

        var totalReq = checks.Sum(x => x.RequiredAmount);
        var totalAvail = checks.Where(x => x.BudgetItemId > 0).Sum(x => x.AvailableBalance);
        var isSolvent = checks.Count > 0 && checks.All(x => x.BudgetItemId > 0 && x.IsSufficient);
        var msg = checks.Count == 0
            ? "Payroll has no calculated employer-cost lines. Calculate the run before budget validation."
            : unmapped.Count > 0
                ? "Pre-payroll budget verification failed: one or more payroll components have no active budget mapping."
                : isSolvent
                    ? "Pre-payroll budget verification successful. Actual calculated payroll costs are fully covered by available appropriations."
                    : "Pre-payroll budget verification failed: actual calculated payroll costs exceed one or more available budget balances.";

        return new PayrollBudgetValidationResult
        {
            IsSolvent = isSolvent,
            FiscalYear = fiscalYear,
            OrganisationId = organisationId,
            TotalRequiredEmoluments = totalReq,
            TotalAvailableEmoluments = totalAvail,
            LineChecks = checks,
            Message = msg
        };
    }

    public async Task<byte[]> ExportBudgetCsvAsync(int fiscalYear = 2026, int organisationId = 0, CancellationToken ct = default)
    {
        var allocations = await GetAllocationsAsync(fiscalYear, organisationId, ct);
        var sb = new StringBuilder();

        // UTF-8 BOM for Excel / SAP compatibility
        sb.Append("\uFEFF");
        sb.AppendLine("FiscalYear,OrganisationID,OrganisationName,MoFEconomicCode,DescriptionDhivehi,DescriptionEnglish,ChapterCode,ApprovedMVR,NetVirementsMVR,RevisedBudgetMVR,CommittedMVR,ExpendedMVR,AvailableBalanceMVR,UtilizationPercent,MoFReference");

        foreach (var item in allocations)
        {
            sb.AppendLine(string.Join(",",
                item.YearNumber,
                item.OrganisationId,
                $"\"{EscapeCsv(item.OrganisationName)}\"",
                $"\"{EscapeCsv(item.EconomicCode)}\"",
                $"\"{EscapeCsv(item.DhivehiName)}\"",
                $"\"{EscapeCsv(item.EnglishName)}\"",
                $"\"{EscapeCsv(item.ChapterCode)}\"",
                item.ApprovedAmount.ToString("F2"),
                item.NetVirement.ToString("F2"),
                item.CurrentBudget.ToString("F2"),
                item.CommittedAmount.ToString("F2"),
                item.ExpendedAmount.ToString("F2"),
                item.AvailableBalance.ToString("F2"),
                item.UtilizationPercentage.ToString("F2") + "%",
                $"\"{EscapeCsv(item.MoFApprovalReference ?? "")}\""
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string val)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Replace("\"", "\"\"");
    }
}
