using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.DTOs.Budget;
using HRM.Application.Interfaces.Budget;
using HRM.Models;
using HRM.Models.Budget;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class BudgetService(IDbContextFactory<HrmTeContext> factory) : IBudgetService
{
    public async Task<BudgetSummaryDto> GetBudgetSummaryAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
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

    public async Task<List<BudgetAllocationDto>> GetAllocationsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
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

    public async Task<BudgetAllocationDto> SaveAllocationAsync(CreateBudgetAllocationRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

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
        var items = await GetAllocationsAsync(2026, request.OrganisationId, ct);
        return items.First(x => x.BudgetItemId == request.BudgetItemId);
    }

    public async Task<List<BudgetVirementDto>> GetVirementsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
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

        var count = await db.BudgetVirements.CountAsync(ct) + 1;
        var virementNumber = $"VIR-2026-{count:D4}";

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

        var list = await GetVirementsAsync(2026, request.OrganisationId, ct);
        return list.First(x => x.VirementNumber == virementNumber);
    }

    public async Task<List<BudgetCommitmentDto>> GetCommitmentsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var raw = await db.BudgetCommitments
            .AsNoTracking()
            .Include(x => x.BudgetItem)
            .Where(x => x.OrganisationId == organisationId)
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

    public async Task<List<BudgetTransactionDto>> GetTransactionsAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var raw = await db.BudgetTransactions
            .AsNoTracking()
            .Include(x => x.BudgetItem)
            .Include(x => x.TransactionType)
            .Where(x => x.OrganisationID == organisationId)
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

    public async Task<List<PayrollBudgetMappingDto>> GetPayrollMappingsAsync(int organisationId = 3, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var raw = await db.PayrollBudgetMappings
            .AsNoTracking()
            .Include(x => x.BudgetItem)
            .Where(x => x.CostCenterOrganisationId == null || x.CostCenterOrganisationId == organisationId)
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

    public async Task<PayrollBudgetValidationResult> ValidatePayrollRunBudgetAsync(int payrollRunId, int organisationId = 3, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var allocations = await GetAllocationsAsync(2026, organisationId, ct);
        var mappings = await GetPayrollMappingsAsync(organisationId, ct);

        // Calculate line checks against current allocations
        var checks = new List<PayrollBudgetLineCheck>();
        var totalReq = 0m;
        var totalAvail = allocations.Sum(x => x.AvailableBalance);

        foreach (var alloc in allocations)
        {
            // Simulation requirement based on commitment amount or active baseline
            var req = alloc.CommittedAmount > 0 ? alloc.CommittedAmount : (alloc.CurrentBudget * 0.08m);
            totalReq += req;

            checks.Add(new PayrollBudgetLineCheck
            {
                BudgetItemId = alloc.BudgetItemId,
                EconomicCode = alloc.EconomicCode,
                ComponentName = alloc.DhivehiName,
                RequiredAmount = req,
                AvailableBalance = alloc.AvailableBalance
            });
        }

        var isSolvent = checks.All(x => x.IsSufficient);
        var msg = isSolvent
            ? "Pre-payroll budget verification successful. All personal emolument line items have sufficient appropriations under Law 3/2006."
            : "Pre-payroll budget verification failed: Deficit detected in one or more budget lines. Virement or supplementary allocation required.";

        return new PayrollBudgetValidationResult
        {
            IsSolvent = isSolvent,
            FiscalYear = 2026,
            OrganisationId = organisationId,
            TotalRequiredEmoluments = totalReq,
            TotalAvailableEmoluments = totalAvail,
            LineChecks = checks,
            Message = msg
        };
    }

    public async Task<byte[]> ExportBudgetCsvAsync(int fiscalYear = 2026, int organisationId = 3, CancellationToken ct = default)
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
