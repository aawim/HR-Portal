using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.Application.Contracts.Pages;
using HRM.Application.Features.Payroll;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Payroll;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Payroll;

public sealed class EnterprisePayrollService : IEnterprisePayroll
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly IUserAccessService _access;
    private readonly IPayrollQueueService _queueService;
    private readonly IPayrollApprovalService _approvalService;
    private readonly IPayrollProcessor _processor;
    private readonly IPayrollPaymentService _paymentService;
    private readonly IPayslipService _payslipService;
    private readonly ILogger<EnterprisePayrollService> _logger;

    public EnterprisePayrollService(
        IDbContextFactory<HrmTeContext> factory,
        IUserAccessService access,
        IPayrollQueueService queueService,
        IPayrollApprovalService approvalService,
        IPayrollProcessor processor,
        IPayrollPaymentService paymentService,
        IPayslipService payslipService,
        ILogger<EnterprisePayrollService> logger)
    {
        _factory = factory;
        _access = access;
        _queueService = queueService;
        _approvalService = approvalService;
        _processor = processor;
        _paymentService = paymentService;
        _payslipService = payslipService;
        _logger = logger;
    }

    private async Task<(int orgId, int userId, int individualId, bool isSuper)> RequireOrgScopeAsync()
    {
        var actor = await _access.RequireContextAsync();
        int org = 0;
        try
        {
            org = await _access.GetCurrentOrganisationIdAsync();
        }
        catch
        {
            org = actor.Organisation?.OrganisationBusinessEntityId
                  ?? actor.ActiveJob?.OrganisationId
                  ?? 0;
        }

        if (org <= 0 && actor.IsSuperAdministrator)
        {
            await using var db = await _factory.CreateDbContextAsync();
            org = await db.Organisations.Select(o => o.BusinessEntityID).FirstOrDefaultAsync();
        }

        if (org <= 0 || (!actor.IsSuperAdministrator && !await _access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Select your current organisation.");

        return (org, actor.UserId, actor.IndividualId, actor.IsSuperAdministrator);
    }

    public async Task<List<PayrollPeriodRow>> GetPeriodsAsync(int? year = null)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.PayrollPeriods.AsNoTracking().Where(p => p.OrganisationId == orgId);
        if (year.HasValue) query = query.Where(p => p.PeriodYear == year.Value);

        return await query
            .OrderByDescending(p => p.PeriodYear)
            .ThenByDescending(p => p.PeriodMonth)
            .Select(p => new PayrollPeriodRow(
                p.PayrollPeriodId,
                p.OrganisationId,
                p.PayrollGroupId,
                p.PeriodYear,
                p.PeriodMonth,
                p.PeriodStartDate,
                p.PeriodEndDate,
                p.AttendanceCutoffDate,
                p.AdjustmentCutoffDate,
                p.PaymentDate,
                (int)p.PeriodStatus,
                p.PayrollRuns.Count
            ))
            .ToListAsync();
    }

    public async Task<ServiceResult> CreatePeriodAsync(PayrollPeriodInput input)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        bool exists = await db.PayrollPeriods.AnyAsync(p =>
            p.OrganisationId == orgId &&
            p.PeriodYear == input.Year &&
            p.PeriodMonth == input.Month &&
            p.PayrollGroupId == input.PayrollGroupId);

        if (exists)
        {
            return ServiceResult.Fail($"A payroll period for {input.Year}-{input.Month:D2} already exists for this group.");
        }

        var startDate = new DateOnly(input.Year, input.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var period = new PayrollPeriod
        {
            OrganisationId = orgId,
            PayrollGroupId = input.PayrollGroupId,
            PeriodYear = input.Year,
            PeriodMonth = input.Month,
            PeriodStartDate = input.StartDate ?? startDate,
            PeriodEndDate = input.EndDate ?? endDate,
            AttendanceCutoffDate = input.AttendanceCutoffDate ?? endDate,
            AdjustmentCutoffDate = input.AdjustmentCutoffDate ?? endDate,
            PaymentDate = input.PaymentDate ?? endDate,
            PeriodStatus = PayrollPeriodStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.PayrollPeriods.Add(period);
        await db.SaveChangesAsync();
        return ServiceResult.Ok("Payroll period created.");
    }

    public async Task<ServiceResult> UpdatePeriodStatusAsync(int periodId, int newStatus)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var period = await db.PayrollPeriods.SingleOrDefaultAsync(p => p.PayrollPeriodId == periodId && p.OrganisationId == orgId);
        if (period == null) return ServiceResult.Fail("Payroll period not found.");

        period.PeriodStatus = (PayrollPeriodStatus)newStatus;
        period.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ServiceResult.Ok("Period status updated.");
    }

    public async Task<List<PayrollRunSummaryRow>> GetRunsAsync(int? periodId = null)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.PayrollRuns.AsNoTracking().Where(r => r.OrganisationId == orgId);
        if (periodId.HasValue) query = query.Where(r => r.PayrollPeriodId == periodId.Value);

        return await query
            .OrderByDescending(r => r.PayrollRunId)
            .Select(r => new PayrollRunSummaryRow(
                r.PayrollRunId,
                r.OrganisationId,
                r.PayrollPeriodId,
                (int)r.RunType,
                (int)r.Status,
                r.Title,
                r.Description,
                r.TotalEmployees,
                r.ProcessedEmployees,
                r.TotalGrossEarnings,
                r.TotalDeductions,
                r.TotalEmployerContributions,
                r.TotalNetPayable,
                r.PreparedByUserId,
                r.PreparedAtUtc,
                r.ApprovedByUserId,
                r.ApprovedAtUtc,
                r.FinalisedByUserId,
                r.FinalisedAtUtc,
                r.Exceptions.Count,
                r.Exceptions.Count(e => e.Severity == PayrollIssueSeverity.Blocking)
            ))
            .ToListAsync();
    }

    public async Task<PayrollRunSummaryRow> GetRunDetailAsync(int runId)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var r = await db.PayrollRuns
            .AsNoTracking()
            .Include(x => x.Exceptions)
            .SingleOrDefaultAsync(x => x.PayrollRunId == runId && x.OrganisationId == orgId);

        if (r == null) throw new InvalidOperationException("Payroll run not found.");

        return new PayrollRunSummaryRow(
            r.PayrollRunId,
            r.OrganisationId,
            r.PayrollPeriodId,
            (int)r.RunType,
            (int)r.Status,
            r.Title,
            r.Description,
            r.TotalEmployees,
            r.ProcessedEmployees,
            r.TotalGrossEarnings,
            r.TotalDeductions,
            r.TotalEmployerContributions,
            r.TotalNetPayable,
            r.PreparedByUserId,
            r.PreparedAtUtc,
            r.ApprovedByUserId,
            r.ApprovedAtUtc,
            r.FinalisedByUserId,
            r.FinalisedAtUtc,
            r.Exceptions.Count,
            r.Exceptions.Count(e => e.Severity == PayrollIssueSeverity.Blocking)
        );
    }

    public async Task<PayrollProgressRow> GetProgressAsync(int runId)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var run = await db.PayrollRuns.AsNoTracking().SingleOrDefaultAsync(r => r.PayrollRunId == runId && r.OrganisationId == orgId);
        if (run == null) throw new InvalidOperationException("Payroll run not found.");

        var batches = await db.PayrollRunBatches.AsNoTracking().Where(b => b.PayrollRunId == runId).ToListAsync();
        var exceptions = await db.PayrollExceptions.AsNoTracking()
            .Where(e => e.PayrollRunId == runId && e.Severity == PayrollIssueSeverity.Blocking)
            .Take(5).Select(e => e.Message).ToListAsync();

        int successful = batches.Where(b => b.Status == PayrollBatchStatus.Completed).Sum(b => b.TotalEmployees);
        int failed = batches.Where(b => b.Status == PayrollBatchStatus.Failed).Sum(b => b.TotalEmployees);
        double pct = run.TotalEmployees > 0 ? ((double)run.ProcessedEmployees / run.TotalEmployees) * 100.0 : 0.0;

        string stage = run.Status switch
        {
            PayrollRunStatus.Preparing => "Preparing frozen snapshots...",
            PayrollRunStatus.Queued => "Queued for background worker...",
            PayrollRunStatus.Calculating => $"Processing batch ({run.ProcessedEmployees}/{run.TotalEmployees})...",
            PayrollRunStatus.Calculated => "Calculation complete.",
            PayrollRunStatus.ValidationFailed => "Validation issues detected.",
            PayrollRunStatus.Approved => "Payroll approved.",
            PayrollRunStatus.Finalised => "Payroll finalised and immutable.",
            _ => run.Status.ToString()
        };

        return new PayrollProgressRow(
            run.PayrollRunId,
            (int)run.Status,
            run.TotalEmployees,
            run.ProcessedEmployees,
            successful,
            failed,
            pct,
            stage,
            exceptions
        );
    }

    public async Task<ServiceResult> CreateRunAsync(CreatePayrollRunInput input)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var period = await db.PayrollPeriods.SingleOrDefaultAsync(p => p.PayrollPeriodId == input.PayrollPeriodId && p.OrganisationId == orgId);
        if (period == null) return ServiceResult.Fail("Invalid payroll period.");

        var run = new PayrollRun
        {
            OrganisationId = orgId,
            PayrollPeriodId = input.PayrollPeriodId,
            RunType = (PayrollRunType)input.RunType,
            Status = PayrollRunStatus.Draft,
            Title = input.Title.Trim(),
            Description = input.Description,
            PreparedByUserId = userId,
            PreparedAtUtc = DateTime.UtcNow
        };

        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync();
        return ServiceResult.Ok("Payroll run created.");
    }

    public async Task<ServiceResult> QueueCalculationAsync(int runId)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var run = await db.PayrollRuns.SingleOrDefaultAsync(r => r.PayrollRunId == runId && r.OrganisationId == orgId);
        if (run == null) return ServiceResult.Fail("Payroll run not found.");

        if (run.Status == PayrollRunStatus.Finalised || run.Status == PayrollRunStatus.Paid || run.Status == PayrollRunStatus.Closed)
        {
            return ServiceResult.Fail($"Cannot recalculate finalised payroll run in state '{run.Status}'.");
        }

        run.Status = PayrollRunStatus.Queued;
        run.Description = null;
        run.ProcessedEmployees = 0;

        // When queuing or retrying calculation, clean up any previous calculated/partial records
        var existingEmployees = await db.PayrollEmployees.Where(e => e.PayrollRunId == runId).ToListAsync();
        if (existingEmployees.Count > 0)
        {
            db.PayrollEmployees.RemoveRange(existingEmployees);
        }

        var existingExceptions = await db.PayrollExceptions.Where(e => e.PayrollRunId == runId).ToListAsync();
        if (existingExceptions.Count > 0)
        {
            db.PayrollExceptions.RemoveRange(existingExceptions);
        }

        var existingBatches = await db.PayrollRunBatches.Where(b => b.PayrollRunId == runId).ToListAsync();
        if (existingBatches.Count > 0)
        {
            db.PayrollRunBatches.RemoveRange(existingBatches);
        }

        var existingSnapshots = await db.PayrollInputSnapshots.Where(s => s.PayrollRunId == runId).ToListAsync();
        if (existingSnapshots.Count > 0)
        {
            db.PayrollInputSnapshots.RemoveRange(existingSnapshots);
        }

        await db.SaveChangesAsync();

        long jobId = await _queueService.EnqueueJobAsync(orgId, runId, PayrollJobType.CalculateBatch, priority: 10);
        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = orgId,
            PayrollRunId = runId,
            UserId = userId,
            ActionType = "ENQUEUE_CALCULATION",
            NewState = "Queued",
            Description = $"Enqueued background payroll calculation job #{jobId}.",
            TimestampUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return ServiceResult.Ok($"Payroll calculation queued with background job #{jobId}.");
    }

    public async Task<ServiceResult> SubmitReviewAsync(int runId, string? comment = null)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        try
        {
            await _approvalService.SubmitForReviewAsync(orgId, runId, userId, comment);
            return ServiceResult.Ok("Payroll submitted for review.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> ApprovePayrollAsync(int runId, string? comment = null)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        try
        {
            await _approvalService.ApprovePayrollAsync(orgId, runId, userId, comment);
            return ServiceResult.Ok("Payroll approved successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> FinalisePayrollAsync(int runId, string? comment = null)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        try
        {
            await _approvalService.FinalisePayrollAsync(orgId, runId, userId, comment);
            return ServiceResult.Ok("Payroll finalised and locked.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> RejectPayrollAsync(int runId, string reason)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        try
        {
            await _approvalService.RejectPayrollAsync(orgId, runId, userId, reason);
            return ServiceResult.Ok("Payroll rejected.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<List<PayrollEmployeeRow>> GetEmployeesAsync(int runId, int page = 1, int pageSize = 50, string? search = null)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.PayrollEmployees
            .AsNoTracking()
            .Include(e => e.Lines)
            .Where(e => e.PayrollRunId == runId && e.PayrollRun!.OrganisationId == orgId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim().ToLower();
            query = query.Where(e => e.EmployeeName.ToLower().Contains(s) || e.EmployeeNumber.ToLower().Contains(s));
        }

        return await query
            .OrderBy(e => e.EmployeeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new PayrollEmployeeRow(
                e.PayrollEmployeeId,
                e.PayrollRunId,
                e.PayrollRunBatchId,
                e.JobId,
                e.IndividualId,
                e.EmployeeNumber,
                e.EmployeeName,
                e.DepartmentName,
                e.PositionTitle,
                e.GradeCode,
                e.StepNumber,
                e.BaseSalary,
                e.GrossEarnings,
                e.TotalDeductions,
                e.EmployerContributions,
                e.NetSalary,
                e.CalculationStatus,
                e.ErrorMessage,
                e.Lines.Select(l => new PayrollLineItemRow(
                    l.PayrollLineId,
                    l.PayrollEmployeeId,
                    l.ComponentCode,
                    l.ComponentName,
                    (int)l.ComponentType,
                    l.Rate,
                    l.Units,
                    l.CalculatedAmount,
                    l.IsTaxable,
                    l.IsPensionable,
                    l.SourceReference
                )).ToList()
            ))
            .ToListAsync();
    }

    public async Task<List<PayrollExceptionRow>> GetExceptionsAsync(int runId)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        return await db.PayrollExceptions
            .AsNoTracking()
            .Where(e => e.PayrollRunId == runId && e.PayrollRun!.OrganisationId == orgId)
            .OrderByDescending(e => e.Severity)
            .ThenBy(e => e.PayrollExceptionId)
            .Select(e => new PayrollExceptionRow(
                e.PayrollExceptionId,
                e.PayrollRunId,
                e.JobId,
                e.IndividualId,
                (int)e.Severity,
                e.Code,
                e.Message,
                e.Details,
                e.IsAcknowledged
            ))
            .ToListAsync();
    }

    public async Task<List<PaymentBatchRow>> GetPaymentBatchesAsync(int? runId = null)
    {
        var (orgId, _, _, _) = await RequireOrgScopeAsync();
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.PaymentBatches.AsNoTracking().Where(b => b.OrganisationId == orgId);
        if (runId.HasValue) query = query.Where(b => b.PayrollRunId == runId.Value);

        return await query
            .OrderByDescending(b => b.PaymentBatchId)
            .Select(b => new PaymentBatchRow(
                b.PaymentBatchId,
                b.OrganisationId,
                b.PayrollRunId,
                b.BatchReference,
                b.BankCode,
                b.FormatCode,
                b.TotalAmount,
                b.TotalCount,
                (int)b.Status,
                b.CreatedAtUtc,
                b.ApprovedAtUtc
            ))
            .ToListAsync();
    }

    public async Task<PaymentBatchRow> PreparePaymentBatchAsync(int runId, string bankCode, string formatCode)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        var batch = await _paymentService.PreparePaymentBatchAsync(orgId, runId, bankCode, formatCode, userId);
        return new PaymentBatchRow(
            batch.PaymentBatchId,
            batch.OrganisationId,
            batch.PayrollRunId,
            batch.BatchReference,
            batch.BankCode,
            batch.FormatCode,
            batch.TotalAmount,
            batch.TotalCount,
            (int)batch.Status,
            batch.CreatedAtUtc,
            batch.ApprovedAtUtc
        );
    }

    public async Task<string> ExportPaymentFileAsync(int paymentBatchId)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        return await _paymentService.ExportBankFileAsync(paymentBatchId, orgId, userId);
    }

    public async Task<ServiceResult> ReconcilePaymentBatchAsync(int paymentBatchId, string reference)
    {
        var (orgId, userId, _, _) = await RequireOrgScopeAsync();
        try
        {
            await _paymentService.ReconcilePaymentBatchAsync(paymentBatchId, orgId, userId, reference);
            return ServiceResult.Ok("Payment batch reconciled.");
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }

    public async Task<List<PayslipRow>> GetMyPayslipsAsync()
    {
        var (orgId, _, individualId, _) = await RequireOrgScopeAsync();
        var payslips = await _payslipService.GetEmployeePayslipsAsync(individualId, orgId);

        return payslips.Select(p => new PayslipRow(
            p.PayrollEmployeeId,
            p.PayrollRunId,
            p.OrganisationId,
            p.OrganisationName,
            p.IndividualId,
            p.EmployeeNumber,
            p.EmployeeName,
            p.DepartmentName,
            p.PositionTitle,
            p.GradeCode,
            p.StepNumber,
            p.PeriodYear,
            p.PeriodMonth,
            p.PeriodStartDate,
            p.PeriodEndDate,
            p.PaymentDate,
            p.BaseSalary,
            p.GrossEarnings,
            p.TotalDeductions,
            p.EmployerContributions,
            p.NetSalary,
            p.BankName,
            p.BankAccountNumber,
            p.PaymentReference,
            p.Earnings.Select(l => new PayrollLineItemRow(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, (int)l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference)).ToList(),
            p.Deductions.Select(l => new PayrollLineItemRow(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, (int)l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference)).ToList(),
            p.EmployerItems.Select(l => new PayrollLineItemRow(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, (int)l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference)).ToList()
        )).ToList();
    }

    public async Task<PayslipRow> GetPayslipDetailAsync(int payrollEmployeeId)
    {
        var (orgId, _, individualId, isSuper) = await RequireOrgScopeAsync();
        var p = await _payslipService.GetPayslipDetailAsync(payrollEmployeeId, orgId, individualId, isSuper);
        if (p == null) throw new InvalidOperationException("Payslip not found.");

        return new PayslipRow(
            p.PayrollEmployeeId,
            p.PayrollRunId,
            p.OrganisationId,
            p.OrganisationName,
            p.IndividualId,
            p.EmployeeNumber,
            p.EmployeeName,
            p.DepartmentName,
            p.PositionTitle,
            p.GradeCode,
            p.StepNumber,
            p.PeriodYear,
            p.PeriodMonth,
            p.PeriodStartDate,
            p.PeriodEndDate,
            p.PaymentDate,
            p.BaseSalary,
            p.GrossEarnings,
            p.TotalDeductions,
            p.EmployerContributions,
            p.NetSalary,
            p.BankName,
            p.BankAccountNumber,
            p.PaymentReference,
            p.Earnings.Select(l => new PayrollLineItemRow(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, (int)l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference)).ToList(),
            p.Deductions.Select(l => new PayrollLineItemRow(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, (int)l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference)).ToList(),
            p.EmployerItems.Select(l => new PayrollLineItemRow(l.PayrollLineId, l.PayrollEmployeeId, l.ComponentCode, l.ComponentName, (int)l.ComponentType, l.Rate, l.Units, l.CalculatedAmount, l.IsTaxable, l.IsPensionable, l.SourceReference)).ToList()
        );
    }
}
