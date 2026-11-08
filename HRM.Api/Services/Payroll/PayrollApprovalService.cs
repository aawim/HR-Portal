using System;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Payroll;

public sealed class PayrollApprovalService : IPayrollApprovalService
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly ILogger<PayrollApprovalService> _logger;

    public PayrollApprovalService(IDbContextFactory<HrmTeContext> factory, ILogger<PayrollApprovalService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<bool> SubmitForReviewAsync(int organisationId, int payrollRunId, int userId, string? comment)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var run = await db.PayrollRuns.SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId);
        if (run == null) throw new InvalidOperationException("Payroll run not found.");

        if (run.Status != PayrollRunStatus.Calculated && run.Status != PayrollRunStatus.ReadyForReview)
        {
            throw new InvalidOperationException($"Cannot submit run for review from state '{run.Status}'.");
        }

        // Check for unresolved blocking exceptions
        bool hasBlockingErrors = await db.PayrollExceptions.AnyAsync(e => e.PayrollRunId == payrollRunId && e.Severity == PayrollIssueSeverity.Blocking);
        if (hasBlockingErrors)
        {
            throw new InvalidOperationException("Cannot submit payroll for review with unresolved blocking errors.");
        }

        var oldState = run.Status.ToString();
        run.Status = PayrollRunStatus.UnderReview;
        run.PreparedByUserId ??= userId;
        run.PreparedAtUtc ??= DateTime.UtcNow;

        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = organisationId,
            PayrollRunId = payrollRunId,
            UserId = userId,
            ActionType = "SUBMIT_FOR_REVIEW",
            OldState = oldState,
            NewState = run.Status.ToString(),
            Description = comment ?? "Submitted for approval review",
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Payroll run {RunId} submitted for review by user {User}", payrollRunId, userId);
        return true;
    }

    public async Task<bool> ApprovePayrollAsync(int organisationId, int payrollRunId, int userId, string? comment)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var run = await db.PayrollRuns.SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId);
        if (run == null) throw new InvalidOperationException("Payroll run not found.");

        if (run.Status != PayrollRunStatus.UnderReview)
        {
            throw new InvalidOperationException($"Cannot approve run from state '{run.Status}'.");
        }

        // Segregation of duties: preparer != approver
        if (run.PreparedByUserId == userId)
        {
            throw new InvalidOperationException("Segregation of duties violation: The user who prepared or submitted the payroll cannot approve it.");
        }

        var oldState = run.Status.ToString();
        run.Status = PayrollRunStatus.Approved;
        run.ApprovedByUserId = userId;
        run.ApprovedAtUtc = DateTime.UtcNow;

        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = organisationId,
            PayrollRunId = payrollRunId,
            UserId = userId,
            ActionType = "APPROVE_PAYROLL",
            OldState = oldState,
            NewState = run.Status.ToString(),
            Description = comment ?? "Approved payroll run",
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Payroll run {RunId} approved by user {User}", payrollRunId, userId);
        return true;
    }

    public async Task<bool> FinalisePayrollAsync(int organisationId, int payrollRunId, int userId, string? comment)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var run = await db.PayrollRuns.SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId);
        if (run == null) throw new InvalidOperationException("Payroll run not found.");

        if (run.Status != PayrollRunStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot finalise run from state '{run.Status}'. It must be approved first.");
        }

        var oldState = run.Status.ToString();
        run.Status = PayrollRunStatus.Finalised;
        run.FinalisedByUserId = userId;
        run.FinalisedAtUtc = DateTime.UtcNow;

        var attendanceAdjustments = await db.AttendanceAdjustments
            .Where(a => a.PayrollRunId == payrollRunId && a.OrganisationId == organisationId &&
                        (a.PayrollProcessingStatus == "Ingested" || a.PayrollProcessingStatus == "Calculated"))
            .ToListAsync();
        foreach (var adjustment in attendanceAdjustments)
        {
            adjustment.PayrollProcessingStatus = "Locked";
            adjustment.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = organisationId,
            PayrollRunId = payrollRunId,
            UserId = userId,
            ActionType = "FINALISE_PAYROLL",
            OldState = oldState,
            NewState = run.Status.ToString(),
            Description = comment ?? "Payroll finalised and locked",
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Payroll run {RunId} finalised and locked by user {User}", payrollRunId, userId);
        return true;
    }

    public async Task<bool> RejectPayrollAsync(int organisationId, int payrollRunId, int userId, string reason)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var run = await db.PayrollRuns.SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId);
        if (run == null) throw new InvalidOperationException("Payroll run not found.");

        if (run.Status == PayrollRunStatus.Finalised || run.Status == PayrollRunStatus.Paid || run.Status == PayrollRunStatus.Closed)
        {
            throw new InvalidOperationException($"Cannot reject finalised or closed payroll run in state '{run.Status}'.");
        }

        var oldState = run.Status.ToString();
        run.Status = PayrollRunStatus.ValidationFailed;
        run.Description = $"Rejected: {reason}";

        var attendanceAdjustments = await db.AttendanceAdjustments
            .Where(a => a.PayrollRunId == payrollRunId && a.OrganisationId == organisationId &&
                        (a.PayrollProcessingStatus == "Ingested" || a.PayrollProcessingStatus == "Calculated"))
            .ToListAsync();
        foreach (var adjustment in attendanceAdjustments)
        {
            adjustment.PayrollProcessingStatus = "Pending";
            adjustment.PayrollRunId = null;
            adjustment.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.PayrollAuditEvents.Add(new PayrollAuditEvent
        {
            OrganisationId = organisationId,
            PayrollRunId = payrollRunId,
            UserId = userId,
            ActionType = "REJECT_PAYROLL",
            OldState = oldState,
            NewState = run.Status.ToString(),
            Description = $"Rejected: {reason}",
            TimestampUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        _logger.LogWarning("Payroll run {RunId} rejected by user {User}: {Reason}", payrollRunId, userId, reason);
        return true;
    }
}
