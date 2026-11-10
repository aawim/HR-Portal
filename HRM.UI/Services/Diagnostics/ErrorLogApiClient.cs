using System.Net.Http.Json;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Microsoft.Extensions.Logging;

namespace HRM.UI.Services.Diagnostics;

public sealed class ErrorLogApiClient : IErrorLogApiClient
{
    private readonly HttpClient _client;
    private readonly IErrorLogService? _localService;
    private readonly ILogger<ErrorLogApiClient>? _logger;

    public ErrorLogApiClient(
        HttpClient client,
        IErrorLogService? localService = null,
        ILogger<ErrorLogApiClient>? logger = null)
    {
        _client = client;
        _localService = localService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ErrorLogEntry>> GetRecentAsync(
        int count = 100,
        ErrorCriticalityLevel? minLevel = null,
        string? search = null)
    {
        try
        {
            var url = $"/api/v1/error-logs?count={count}";
            if (minLevel.HasValue) url += $"&minLevel={(int)minLevel.Value}";
            if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";

            var response = await _client.GetFromJsonAsync<List<ErrorLogEntry>>(url);
            if (response != null && response.Count > 0)
                return response;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ErrorLogApiClient: Failed to fetch errors from API endpoint, trying local service.");
        }

        if (_localService != null)
        {
            return await _localService.GetRecentErrorsAsync(count, minLevel, search);
        }

        return Array.Empty<ErrorLogEntry>();
    }

    public async Task<ErrorLogEntry?> GetByIdAsync(Guid id)
    {
        try
        {
            var entry = await _client.GetFromJsonAsync<ErrorLogEntry>($"/api/v1/error-logs/{id}");
            if (entry != null) return entry;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ErrorLogApiClient: Failed to fetch error {Id} from API, trying local service.", id);
        }

        if (_localService != null)
        {
            return await _localService.GetErrorByIdAsync(id);
        }

        return null;
    }

    public async Task<ErrorStatistics> GetStatisticsAsync()
    {
        try
        {
            var stats = await _client.GetFromJsonAsync<ErrorStatistics>("/api/v1/error-logs/statistics");
            if (stats != null) return stats;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ErrorLogApiClient: Failed to fetch error statistics from API, trying local service.");
        }

        if (_localService != null)
        {
            return await _localService.GetStatisticsAsync();
        }

        return new ErrorStatistics();
    }

    public async Task<bool> MarkResolvedAsync(Guid id, string resolvedBy, string? notes = null)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"/api/v1/error-logs/{id}/resolve", new
            {
                ResolvedBy = resolvedBy,
                Notes = notes
            });

            if (response.IsSuccessStatusCode)
                return true;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ErrorLogApiClient: Failed to mark error {Id} resolved via API, trying local service.", id);
        }

        if (_localService != null)
        {
            return await _localService.MarkResolvedAsync(id, resolvedBy, notes);
        }

        return false;
    }

    public async Task<bool> ReopenAsync(Guid id)
    {
        try
        {
            var response = await _client.PostAsync($"/api/v1/error-logs/{id}/reopen", null);
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ErrorLogApiClient: Failed to reopen error {Id} via API, trying local service.", id);
        }

        if (_localService != null)
        {
            return await _localService.ReopenAsync(id);
        }

        return false;
    }

    public async Task<int> ClearResolvedAsync()
    {
        try
        {
            var response = await _client.DeleteAsync("/api/v1/error-logs/resolved");
            if (response.IsSuccessStatusCode)
            {
                var count = await response.Content.ReadFromJsonAsync<int>();
                return count;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ErrorLogApiClient: Failed to clear resolved via API, trying local service.");
        }

        if (_localService != null)
        {
            return await _localService.ClearResolvedAsync();
        }

        return 0;
    }

    public async Task<ErrorLogEntry?> SimulateErrorAsync(string exceptionType, string? message = null, ErrorCriticalityLevel? level = null)
    {
        try
        {
            var response = await _client.PostAsJsonAsync("/api/v1/error-logs/simulate", new
            {
                ExceptionType = exceptionType,
                Message = message,
                Source = "HRM.Api",
                PageLocation = "/developer/error-logs",
                Level = level ?? ErrorCriticalityLevel.Error
            });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ErrorLogEntry>();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "ErrorLogApiClient: Failed to simulate error via API.");
        }

        return null;
    }
}
