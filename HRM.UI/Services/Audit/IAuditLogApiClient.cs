using HRM.AuditLogs.Models;

namespace HRM.UI.Services.Audit;

public interface IAuditLogApiClient
{
    Task<(AuditLogPageResult? Result, bool IsUnauthorized, string? Error)> SearchAsync(AuditLogFilter filter, CancellationToken ct = default);
    Task<(AuditLogRecordDto? Record, bool IsUnauthorized, string? Error)> GetByIdAsync(long id, CancellationToken ct = default);
    Task<(AuditLogPageResult? Result, bool IsUnauthorized, string? Error)> GetEntityHistoryAsync(string entityType, string entityId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<(byte[]? CsvBytes, string? FileName, bool IsUnauthorized, string? Error)> ExportCsvAsync(AuditLogFilter filter, CancellationToken ct = default);
}
