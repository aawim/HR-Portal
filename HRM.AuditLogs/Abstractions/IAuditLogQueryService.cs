using HRM.AuditLogs.Models;

namespace HRM.AuditLogs.Abstractions;

public interface IAuditLogQueryService
{
    Task<AuditLogPageResult> SearchAsync(
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AuditLogRecordDto?> GetByIdAsync(
        long auditLogId,
        AuditActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AuditLogPageResult> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AuditLogPageResult> GetUserHistoryAsync(
        int targetUserId,
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AuditLogPageResult> GetOrganisationHistoryAsync(
        int organisationId,
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogRecordDto>> GetByCorrelationIdAsync(
        string correlationId,
        AuditActorContext actor,
        CancellationToken cancellationToken = default);

    Task<AuditExportResult> ExportCsvAsync(
        AuditLogFilter filter,
        AuditActorContext actor,
        CancellationToken cancellationToken = default);
}
