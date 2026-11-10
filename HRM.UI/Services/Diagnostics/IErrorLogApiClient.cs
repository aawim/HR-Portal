using HRM.ErrorLogs.Models;

namespace HRM.UI.Services.Diagnostics;

public interface IErrorLogApiClient
{
    Task<IReadOnlyList<ErrorLogEntry>> GetRecentAsync(int count = 100, ErrorCriticalityLevel? minLevel = null, string? search = null);
    Task<ErrorLogEntry?> GetByIdAsync(Guid id);
    Task<ErrorStatistics> GetStatisticsAsync();
    Task<bool> MarkResolvedAsync(Guid id, string resolvedBy, string? notes = null);
    Task<bool> ReopenAsync(Guid id);
    Task<int> ClearResolvedAsync();
    Task<ErrorLogEntry?> SimulateErrorAsync(string exceptionType, string? message = null, ErrorCriticalityLevel? level = null);
}
