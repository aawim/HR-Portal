using System.Net;
using System.Net.Http.Json;
using HRM.AuditLogs.Models;

namespace HRM.UI.Services.Audit;

public sealed class AuditLogApiClient(HttpClient client) : IAuditLogApiClient
{
    public async Task<(AuditLogPageResult? Result, bool IsUnauthorized, string? Error)> SearchAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        try
        {
            var url = BuildQuery("/api/v1/audit-logs", filter);
            using var response = await client.GetAsync(url, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (null, true, "You are not authorised to view audit logs for this scope.");
            if (!response.IsSuccessStatusCode)
                return (null, false, $"Audit logs could not be loaded (HTTP {(int)response.StatusCode}).");

            var data = await response.Content.ReadFromJsonAsync<AuditLogPageResult>(cancellationToken: ct);
            return (data ?? new AuditLogPageResult(), false, null);
        }
        catch (Exception)
        {
            return (null, false, "Audit logs service is currently unavailable.");
        }
    }

    public async Task<(AuditLogRecordDto? Record, bool IsUnauthorized, string? Error)> GetByIdAsync(long id, CancellationToken ct = default)
    {
        try
        {
            using var response = await client.GetAsync($"/api/v1/audit-logs/{id}", ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (null, true, "You are not authorised to view this audit record.");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return (null, false, "Audit record not found.");
            if (!response.IsSuccessStatusCode)
                return (null, false, $"Could not load audit details (HTTP {(int)response.StatusCode}).");

            var data = await response.Content.ReadFromJsonAsync<AuditLogRecordDto>(cancellationToken: ct);
            return (data, false, null);
        }
        catch (Exception)
        {
            return (null, false, "Audit details could not be loaded.");
        }
    }

    public async Task<(AuditLogPageResult? Result, bool IsUnauthorized, string? Error)> GetEntityHistoryAsync(
        string entityType, string entityId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/v1/audit-logs/entity/{Uri.EscapeDataString(entityType)}/{Uri.EscapeDataString(entityId)}?page={page}&pageSize={pageSize}";
            using var response = await client.GetAsync(url, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (null, true, "You do not have permission to view audit history for this record.");
            if (!response.IsSuccessStatusCode)
                return (null, false, $"Audit history could not be loaded (HTTP {(int)response.StatusCode}).");

            var data = await response.Content.ReadFromJsonAsync<AuditLogPageResult>(cancellationToken: ct);
            return (data ?? new AuditLogPageResult(), false, null);
        }
        catch (Exception)
        {
            return (null, false, "Audit history service is unavailable.");
        }
    }

    public async Task<(byte[]? CsvBytes, string? FileName, bool IsUnauthorized, string? Error)> ExportCsvAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        try
        {
            var url = BuildQuery("/api/v1/audit-logs/export", filter);
            using var response = await client.GetAsync(url, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (null, null, true, "You do not have permission to export audit logs.");
            if (!response.IsSuccessStatusCode)
                return (null, null, false, $"Export failed (HTTP {(int)response.StatusCode}).");

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return (bytes, fileName, false, null);
        }
        catch (Exception)
        {
            return (null, null, false, "Audit export failed. Please try again.");
        }
    }

    private static string BuildQuery(string basePath, AuditLogFilter f)
    {
        var parts = new List<string>
        {
            $"page={Math.Max(1, f.Page)}",
            $"pageSize={Math.Clamp(f.PageSize <= 0 ? 25 : f.PageSize, 1, 200)}",
            $"sortBy={Uri.EscapeDataString(f.SortBy ?? "OccurredAtUtc")}",
            $"sortDescending={f.SortDescending.ToString().ToLowerInvariant()}",
            $"includeDetails={f.IncludeDetails.ToString().ToLowerInvariant()}"
        };

        if (f.OrganisationId.HasValue && f.OrganisationId.Value > 0) parts.Add($"organisationId={f.OrganisationId.Value}");
        if (f.DivisionId.HasValue && f.DivisionId.Value > 0) parts.Add($"divisionId={f.DivisionId.Value}");
        if (f.UserId.HasValue && f.UserId.Value > 0) parts.Add($"userId={f.UserId.Value}");
        if (!string.IsNullOrWhiteSpace(f.Action)) parts.Add($"action={Uri.EscapeDataString(f.Action.Trim())}");
        if (!string.IsNullOrWhiteSpace(f.Outcome)) parts.Add($"outcome={Uri.EscapeDataString(f.Outcome.Trim())}");
        if (!string.IsNullOrWhiteSpace(f.Module)) parts.Add($"module={Uri.EscapeDataString(f.Module.Trim())}");
        if (!string.IsNullOrWhiteSpace(f.EntityType)) parts.Add($"entityType={Uri.EscapeDataString(f.EntityType.Trim())}");
        if (!string.IsNullOrWhiteSpace(f.EntityId)) parts.Add($"entityId={Uri.EscapeDataString(f.EntityId.Trim())}");
        if (!string.IsNullOrWhiteSpace(f.CorrelationId)) parts.Add($"correlationId={Uri.EscapeDataString(f.CorrelationId.Trim())}");
        if (!string.IsNullOrWhiteSpace(f.SearchText)) parts.Add($"search={Uri.EscapeDataString(f.SearchText.Trim())}");
        if (f.FromUtc.HasValue) parts.Add($"fromUtc={Uri.EscapeDataString(f.FromUtc.Value.ToString("O"))}");
        if (f.ToUtc.HasValue) parts.Add($"toUtc={Uri.EscapeDataString(f.ToUtc.Value.ToString("O"))}");

        return $"{basePath}?{string.Join("&", parts)}";
    }
}
