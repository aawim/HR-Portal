using HRM.AuditLogs.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.AuditLogs.Abstractions;

public interface IAuditLogService
{
    Task<AuditLogResult> RecordAsync(
        AuditLogRequest request,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordBatchAsync(
        IEnumerable<AuditLogRequest> requests,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordWithinTransactionAsync(
        DbContext businessContext,
        IEnumerable<AuditLogRequest> requests,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordApprovalAsync(
        string module,
        string entityType,
        string entityId,
        string description,
        string? reason = null,
        int? organisationId = null,
        int? divisionId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordRejectionAsync(
        string module,
        string entityType,
        string entityId,
        string description,
        string? reason = null,
        int? organisationId = null,
        int? divisionId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordAttendanceCorrectionAsync(
        string entityType,
        string entityId,
        string description,
        IReadOnlyList<AuditPropertyChange> changes,
        string? reason = null,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordSalaryChangeAsync(
        string entityType,
        string entityId,
        string description,
        IReadOnlyList<AuditPropertyChange> changes,
        string? reason = null,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordPayrollAdjustmentAsync(
        string entityType,
        string entityId,
        string description,
        IReadOnlyList<AuditPropertyChange> changes,
        string? reason = null,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordPermissionChangeAsync(
        string action,
        string entityType,
        string entityId,
        string description,
        IReadOnlyList<AuditPropertyChange>? changes = null,
        string? reason = null,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordOrganisationAccessChangeAsync(
        string action,
        string entityId,
        string description,
        int? organisationId = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordStaffTransferDecisionAsync(
        string action,
        string transferId,
        string description,
        string? reason = null,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordWorkflowOverrideAsync(
        string module,
        string entityType,
        string entityId,
        string description,
        string reason,
        int? organisationId = null,
        CancellationToken cancellationToken = default);

    Task<AuditLogResult> RecordSecurityEventAsync(
        string action,
        string outcome,
        string module,
        string entityType,
        string entityId,
        string description,
        string? reason = null,
        int? organisationId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
