using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        var compensations = await db.EmployeeCompensations
            .Where(c => c.OrganisationId == organisationId && individualIds.Contains(c.IndividualId) && c.IsActive)
            .ToDictionaryAsync(c => c.IndividualId);

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
            string accNum = comp?.BankAccountNumber ?? $"7701{emp.IndividualId:D8}";
            string bankName = comp?.BankName ?? "Bank of Maldives";
            string branch = comp?.BankBranch ?? "Male";

            batch.Transactions.Add(new PaymentTransaction
            {
                PayrollEmployeeId = emp.PayrollEmployeeId,
                IndividualId = emp.IndividualId,
                EmployeeName = emp.EmployeeName,
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

        var sb = new StringBuilder();
        // Standard Bank CSV Export
        sb.AppendLine("PaymentReference,EmployeeName,BankAccountNumber,BankName,BankBranch,Amount,Currency,Status");
        foreach (var tx in batch.Transactions)
        {
            sb.AppendLine($"\"{tx.PaymentReference}\",\"{tx.EmployeeName}\",\"{tx.BankAccountNumber}\",\"{tx.BankName}\",\"{tx.BankBranch}\",{tx.Amount:F2},MVR,\"{tx.Status}\"");
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
