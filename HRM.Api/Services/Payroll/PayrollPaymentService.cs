using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Payroll;

public sealed class PayrollPaymentService : IPayrollPaymentService
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly ILogger<PayrollPaymentService> _logger;

    public PayrollPaymentService(IDbContextFactory<HrmTeContext> factory, ILogger<PayrollPaymentService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<PaymentBatchDto> PreparePaymentBatchAsync(int organisationId, int payrollRunId, string bankCode, string formatCode, int userId)
    {
        if (formatCode is not ("BML_BULK" or "GENERIC_CSV"))
            throw new InvalidOperationException("This bank upload format has not been verified. Use the payment review CSV and the bank's current approved template.");
        await using var db = await _factory.CreateDbContextAsync();
        var run = await db.PayrollRuns
            .Include(r => r.Employees)
            .SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId);

        if (run == null) throw new InvalidOperationException("Payroll run not found.");
        if (run.Status != PayrollRunStatus.Finalised && run.Status != PayrollRunStatus.PaymentPrepared)
        {
            throw new InvalidOperationException($"Cannot prepare payment batch for payroll run in status '{run.Status}'. It must be Finalised.");
        }

        // Check if batch already exists
        var existing = await db.PaymentBatches.FirstOrDefaultAsync(b => b.PayrollRunId == payrollRunId);
        if (existing != null)
        {
            return new PaymentBatchDto(
                existing.PaymentBatchId,
                existing.OrganisationId,
                existing.PayrollRunId,
                existing.BatchReference,
                existing.BankCode,
                existing.FormatCode,
                existing.TotalAmount,
                existing.TotalCount,
                existing.Status,
                existing.CreatedAtUtc,
                existing.ApprovedAtUtc
            );
        }

        var employees = run.Employees.Where(e => e.NetSalary > 0).ToList();
        var individualIds = employees.Select(e => e.IndividualId).ToList();

        var frozenInputs = await db.PayrollInputSnapshots.AsNoTracking()
            .Where(s => s.PayrollRunId == payrollRunId).ToListAsync();
        var compensations = frozenInputs.Select(s => JsonSerializer.Deserialize<EmployeePayrollInputItem>(s.SnapshotJson))
            .Where(s => s != null).ToDictionary(s => s!.IndividualId, s => s!);

        var statutoryPolicy = await PayrollStatutoryPolicyLoader.LoadAsync(db, organisationId);

        // Payment routing must agree with the approved snapshot, even if the live staff record changes.
        if (employees.Count > 0)
        {
            var missingBank = employees
                .Where(emp => string.IsNullOrWhiteSpace(compensations.GetValueOrDefault(emp.IndividualId)?.BankAccountNumber))
                .Select(emp => emp.EmployeeName)
                .ToList();
            if (missingBank.Count > 0)
            {
                var sample = string.Join(", ", missingBank.Take(5));
                var extra = missingBank.Count > 5 ? $" (+{missingBank.Count - 5} more)" : string.Empty;
                throw new InvalidOperationException($"Cannot prepare payment batch: {missingBank.Count} employee(s) are missing verified bank account details ({sample}{extra}).");
            }
        }

        string batchRef = $"PAY-BATCH-{run.PayrollRunId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var batch = new PaymentBatch
        {
            OrganisationId = organisationId,
            PayrollRunId = payrollRunId,
            BatchReference = batchRef,
            BankCode = bankCode,
            FormatCode = formatCode,
            TotalAmount = employees.Sum(e => e.NetSalary),
            TotalCount = employees.Count,
            Status = PaymentBatchStatus.Prepared,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var emp in employees)
        {
            var comp = compensations.GetValueOrDefault(emp.IndividualId);
            string accNum = comp?.BankAccountNumber?.Trim() ?? string.Empty;
            string bankName = comp?.BankName.Trim() ?? "";
            string branch = comp?.BankBranch.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(comp?.AccountHolderName))
                throw new InvalidOperationException($"Verified bank name and account holder are required for {emp.EmployeeName}.");
            if (formatCode == "BML_BULK") ValidateBmlBeneficiary(comp!);

            batch.Transactions.Add(new PaymentTransaction
            {
                PayrollEmployeeId = emp.PayrollEmployeeId,
                IndividualId = emp.IndividualId,
                EmployeeName = comp!.AccountHolderName,
                BankAccountNumber = accNum,
                BankName = bankName,
                BankBranch = branch,
                Amount = emp.NetSalary,
                CurrencyTypeId = comp?.CurrencyTypeId ?? 1,
                PaymentReference = $"SAL-{run.PayrollRunId}-{emp.IndividualId}",
                Status = "Prepared",
                ProcessedAtUtc = DateTime.UtcNow
            });
        }

        db.PaymentBatches.Add(batch);
        run.Status = PayrollRunStatus.PaymentPrepared;

        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = organisationId,
            PayrollRunId = payrollRunId,
            UserId = userId,
            ActionType = "PREPARE_PAYMENT_BATCH",
            NewState = "PaymentPrepared",
            Description = $"Created payment batch {batchRef} with {batch.TotalCount} instructions totaling {batch.TotalAmount:C2}.",
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Payment batch {BatchId} created for run {RunId}", batch.PaymentBatchId, payrollRunId);

        return new PaymentBatchDto(
            batch.PaymentBatchId,
            batch.OrganisationId,
            batch.PayrollRunId,
            batch.BatchReference,
            batch.BankCode,
            batch.FormatCode,
            batch.TotalAmount,
            batch.TotalCount,
            batch.Status,
            batch.CreatedAtUtc,
            batch.ApprovedAtUtc
        );
    }

    public async Task<string> ExportBankFileAsync(int paymentBatchId, int organisationId, int userId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var batch = await db.PaymentBatches
            .Include(b => b.Transactions)
            .SingleOrDefaultAsync(b => b.PaymentBatchId == paymentBatchId && b.OrganisationId == organisationId);

        if (batch == null) throw new InvalidOperationException("Payment batch not found.");

        if (batch.FormatCode is not ("BML_BULK" or "GENERIC_CSV"))
            throw new InvalidOperationException("Unverified bank format. Use a payment review CSV and the bank's approved template.");
        if (batch.Status is PaymentBatchStatus.Reconciled or PaymentBatchStatus.Submitted or PaymentBatchStatus.PartiallyPaid)
            throw new InvalidOperationException("Submitted or reconciled payment batches cannot be exported again.");
        if (batch.Status == PaymentBatchStatus.Exported && batch.ExportedContent != null) return batch.ExportedContent;
        var frozen = await db.PayrollInputSnapshots.AsNoTracking().Where(s => s.PayrollRunId == batch.PayrollRunId).ToListAsync();
        var inputs = frozen.Select(s => JsonSerializer.Deserialize<EmployeePayrollInputItem>(s.SnapshotJson))
            .Where(s => s != null).ToDictionary(s => s!.IndividualId, s => s!);
        var sb = new StringBuilder();
        // Public BML Internet Banking Bulk Transfers template, checked 2026-11-26.
        sb.AppendLine(batch.FormatCode == "BML_BULK"
            ? "id_card,full_name,credit_account,credit_amount,bank"
            : "PaymentReference,AccountHolderName,BankAccountNumber,BankName,BankBranch,Amount,Currency,Status");
        foreach (var tx in batch.Transactions.OrderBy(t => t.IndividualId))
        {
            if (!inputs.TryGetValue(tx.IndividualId, out var input))
                throw new InvalidOperationException("Approved payroll bank details are missing.");
            if (input.FrozenPolicy?.DefaultCurrencyCode is not (null or "MVR"))
                throw new InvalidOperationException("Foreign-currency bank exports require an approved currency mapping.");
            if (batch.FormatCode == "BML_BULK")
            {
                ValidateBmlBeneficiary(input);
                if (tx.BankAccountNumber != input.BankAccountNumber.Trim())
                    throw new InvalidOperationException("Payment account differs from the approved payroll snapshot.");
                sb.AppendLine(PayrollReportValues.Csv(input.BankIdentityNumber, tx.EmployeeName, tx.BankAccountNumber, tx.Amount, "BML"));
            }
            else sb.AppendLine(PayrollReportValues.Csv(tx.PaymentReference, tx.EmployeeName, tx.BankAccountNumber,
                tx.BankName, tx.BankBranch, tx.Amount, "MVR", tx.Status));
        }

        string content = sb.ToString();
        batch.ExportedContent = content;
        batch.Status = PaymentBatchStatus.Exported;

        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = organisationId,
            PayrollRunId = batch.PayrollRunId,
            UserId = userId,
            ActionType = "EXPORT_BANK_FILE",
            Description = $"Exported bank payment file for batch {batch.BatchReference}",
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return content;
    }

    public static void ValidateBmlBeneficiary(EmployeePayrollInputItem input)
    {
        if (!(string.Equals(input.BankName.Trim(), "BML", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(input.BankName.Trim(), "Bank of Maldives", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("This BML template supports BML credit accounts. Other banks require their verified bank code/template.");
        if (input.BankAccountNumber.Trim().Length != 13 || !input.BankAccountNumber.Trim().All(char.IsAsciiDigit))
            throw new InvalidOperationException("BML account numbers must contain exactly 13 digits.");
        if (string.IsNullOrWhiteSpace(input.BankIdentityNumber) || !input.BankIdentityNumber.All(char.IsAsciiLetterOrDigit))
            throw new InvalidOperationException("BML requires the beneficiary's alphanumeric national ID or passport number.");
    }

    public async Task<bool> ReconcilePaymentBatchAsync(int paymentBatchId, int organisationId, int userId, string reference)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var batch = await db.PaymentBatches
            .Include(b => b.Transactions)
            .SingleOrDefaultAsync(b => b.PaymentBatchId == paymentBatchId && b.OrganisationId == organisationId);

        if (batch == null) throw new InvalidOperationException("Payment batch not found.");

        batch.Status = PaymentBatchStatus.Reconciled;
        foreach (var tx in batch.Transactions)
        {
            tx.Status = "Reconciled";
            tx.TransactionReference = reference;
        }

        var run = await db.PayrollRuns.SingleOrDefaultAsync(r => r.PayrollRunId == batch.PayrollRunId);
        if (run != null)
        {
            run.Status = PayrollRunStatus.Paid;
        }

        var attendanceAdjustments = await db.AttendanceAdjustments
            .Where(a => a.PayrollRunId == batch.PayrollRunId && a.OrganisationId == organisationId)
            .ToListAsync();
        foreach (var adjustment in attendanceAdjustments)
        {
            adjustment.PayrollProcessingStatus = "Paid";
            adjustment.UpdatedAtUtc = DateTime.UtcNow;
        }

        var snapshotJson = await db.PayrollInputSnapshots.AsNoTracking()
            .Where(s => s.PayrollRunId == batch.PayrollRunId)
            .Select(s => s.SnapshotJson)
            .ToListAsync();
        var overtimeRequestIds = snapshotJson
            .Select(json => JsonSerializer.Deserialize<EmployeePayrollInputItem>(json))
            .Where(input => input != null)
            .SelectMany(input => input!.OvertimeClaims)
            .Select(claim => claim.RequestId)
            .Distinct()
            .ToList();
        if (overtimeRequestIds.Count > 0)
        {
            var overtimeClaims = await db.OvertimeRequests
                .Where(o => o.OrganisationId == organisationId && overtimeRequestIds.Contains(o.RequestId) && !o.PaidAtUtc.HasValue)
                .ToListAsync();
            foreach (var claim in overtimeClaims)
            {
                claim.PaidAtUtc = DateTime.UtcNow;
                claim.PaidByUserId = userId;
                claim.PaymentReference = reference;
            }
        }

        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = organisationId,
            PayrollRunId = batch.PayrollRunId,
            UserId = userId,
            ActionType = "RECONCILE_PAYMENTS",
            NewState = "Paid",
            Description = $"Reconciled payment batch {batch.BatchReference} with bank reference {reference}",
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Reconciled payment batch {BatchId} with ref {Ref}", paymentBatchId, reference);
        return true;
    }
}
