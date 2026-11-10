using System.Collections.Concurrent;
using System.Text.Json;
using HRM.ErrorLogs.Diagnostics;
using HRM.ErrorLogs.Models;
using Microsoft.Extensions.Logging;

namespace HRM.ErrorLogs.Services;

/// <summary>
/// Thread-safe hybrid in-memory and persistent error logging service.
/// </summary>
public sealed class ErrorLogService : IErrorLogService
{
    private const int MaxBufferSize = 1000;
    private readonly ConcurrentDictionary<Guid, ErrorLogEntry> _entries = new();
    private readonly ConcurrentQueue<Guid> _order = new();
    private readonly IErrorLogStore? _persistentStore;
    private readonly IErrorLogQueue? _queue;
    private readonly ILogger<ErrorLogService>? _logger;
    private readonly string _defaultApplicationSource;

    public ErrorLogService(
        IErrorLogStore? persistentStore = null,
        IErrorLogQueue? queue = null,
        ILogger<ErrorLogService>? logger = null,
        string defaultApplicationSource = "HRM.Api")
    {
        _persistentStore = persistentStore;
        _queue = queue;
        _logger = logger;
        _defaultApplicationSource = defaultApplicationSource;

        SeedSampleIfEmpty();
    }

    public async Task<ErrorLogEntry> CaptureExceptionAsync(
        Exception ex,
        ErrorCriticalityLevel level = ErrorCriticalityLevel.Error,
        ErrorContext? context = null)
    {
        if (ex == null)
            throw new ArgumentNullException(nameof(ex));

        var location = StackTraceAnalyzer.Analyze(ex);
        var fix = FixRecommendationEngine.Generate(ex, location);

        var entry = new ErrorLogEntry
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            TimestampLocal = DateTime.Now,
            CriticalityLevel = level,
            ApplicationSource = context?.ApplicationSource ?? _defaultApplicationSource,
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            MachineName = System.Environment.MachineName,
            ProcessId = System.Environment.ProcessId,
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
            TraceId = context?.TraceId,
            Username = context?.Username ?? "Anonymous",
            UserId = context?.UserId,
            RemoteIp = context?.RemoteIp,
            PageLocation = context?.PageLocation,
            HttpMethod = context?.HttpMethod,
            StatusCode = context?.StatusCode ?? (level == ErrorCriticalityLevel.Fatal || level == ErrorCriticalityLevel.Critical ? 500 : 400),
            UserAgent = context?.UserAgent,
            ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
            ErrorMessage = ex.Message,
            StackTrace = ex.StackTrace,
            Location = location,
            Fix = fix,
            CustomDataJson = context?.CustomDataJson
        };

        if (ex.InnerException != null)
        {
            entry.InnerExceptionType = ex.InnerException.GetType().FullName ?? ex.InnerException.GetType().Name;
            entry.InnerErrorMessage = ex.InnerException.Message;
            entry.InnerStackTrace = ex.InnerException.StackTrace;
        }

        return await LogAsync(entry);
    }

    public Task<ErrorLogEntry> LogAsync(ErrorLogEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        // Enforce in-memory ring buffer capacity
        _entries[entry.Id] = entry;
        _order.Enqueue(entry.Id);

        while (_order.Count > MaxBufferSize && _order.TryDequeue(out var oldId))
        {
            _entries.TryRemove(oldId, out _);
        }

        // Enqueue to bounded channel queue for background batch writing (zero thread blocking)
        if (_queue != null)
        {
            _queue.Enqueue(entry);
        }
        else if (_persistentStore != null)
        {
            // Fallback for standalone/in-memory environments without hosted queue
            _ = Task.Run(async () =>
            {
                try
                {
                    await _persistentStore.PersistAsync(entry);
                }
                catch (Exception persistEx)
                {
                    _logger?.LogWarning(persistEx, "ErrorLogService: Failed to persist error entry {Id} to database store.", entry.Id);
                }
            });
        }

        return Task.FromResult(entry);
    }

    public async Task<IReadOnlyList<ErrorLogEntry>> GetRecentErrorsAsync(
        int count = 100,
        ErrorCriticalityLevel? minLevel = null,
        string? search = null)
    {
        IEnumerable<ErrorLogEntry> combined;

        if (_persistentStore != null)
        {
            try
            {
                var dbEntries = await _persistentStore.LoadRecentAsync(count);
                var map = new Dictionary<Guid, ErrorLogEntry>();

                foreach (var item in dbEntries)
                {
                    map[item.Id] = item;
                    _entries.TryAdd(item.Id, item);
                }

                foreach (var item in _entries.Values)
                {
                    map.TryAdd(item.Id, item);
                }

                combined = map.Values;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "ErrorLogService: Failed to load recent errors from persistent store, falling back to memory.");
                combined = _entries.Values;
            }
        }
        else
        {
            combined = _entries.Values;
        }

        var query = combined.AsEnumerable();

        if (minLevel.HasValue)
        {
            query = query.Where(e => e.CriticalityLevel >= minLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                (e.ErrorMessage != null && e.ErrorMessage.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.ExceptionType != null && e.ExceptionType.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.Location?.FileName != null && e.Location.FileName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.Location?.MethodName != null && e.Location.MethodName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.Username != null && e.Username.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.RemoteIp != null && e.RemoteIp.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.PageLocation != null && e.PageLocation.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (e.ApplicationSource != null && e.ApplicationSource.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var results = query
            .OrderByDescending(e => e.TimestampUtc)
            .Take(Math.Min(count, MaxBufferSize))
            .ToList();

        return results;
    }

    public async Task<ErrorLogEntry?> GetErrorByIdAsync(Guid id)
    {
        if (_entries.TryGetValue(id, out var entry))
            return entry;

        if (_persistentStore != null)
        {
            try
            {
                var dbEntry = await _persistentStore.GetByIdAsync(id);
                if (dbEntry != null)
                {
                    _entries[dbEntry.Id] = dbEntry;
                    return dbEntry;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "ErrorLogService: Failed to fetch error {Id} from persistent store.", id);
            }
        }

        return null;
    }

    public async Task<bool> MarkResolvedAsync(Guid id, string resolvedBy, string? notes = null)
    {
        bool found = false;
        if (_entries.TryGetValue(id, out var entry))
        {
            entry.IsResolved = true;
            entry.ResolvedBy = resolvedBy;
            entry.ResolvedAt = DateTime.UtcNow;
            entry.ResolutionNotes = notes;
            found = true;
        }

        if (_persistentStore != null)
        {
            try
            {
                var storeUpdated = await _persistentStore.UpdateResolutionAsync(id, true, resolvedBy, notes);
                found = found || storeUpdated;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update resolution in store for {Id}", id);
            }
        }

        return found;
    }

    public async Task<bool> ReopenAsync(Guid id)
    {
        bool found = false;
        if (_entries.TryGetValue(id, out var entry))
        {
            entry.IsResolved = false;
            entry.ResolvedBy = null;
            entry.ResolvedAt = null;
            found = true;
        }

        if (_persistentStore != null)
        {
            try
            {
                var storeUpdated = await _persistentStore.UpdateResolutionAsync(id, false, null, null);
                found = found || storeUpdated;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update resolution in store for {Id}", id);
            }
        }

        return found;
    }

    public async Task<int> ClearResolvedAsync()
    {
        var resolvedIds = _entries.Values
            .Where(e => e.IsResolved)
            .Select(e => e.Id)
            .ToList();

        int removed = 0;
        foreach (var id in resolvedIds)
        {
            if (_entries.TryRemove(id, out _))
            {
                removed++;
            }
        }

        if (_persistentStore != null)
        {
            try
            {
                var storeCleared = await _persistentStore.ClearResolvedAsync();
                return Math.Max(removed, storeCleared);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to clear resolved error logs from persistent store.");
            }
        }

        return removed;
    }

    public async Task<ErrorStatistics> GetStatisticsAsync()
    {
        var all = (await GetRecentErrorsAsync(MaxBufferSize)).ToList();
        var now = DateTime.UtcNow;

        var stats = new ErrorStatistics
        {
            TotalErrors = all.Count,
            UnresolvedCount = all.Count(e => !e.IsResolved),
            ResolvedCount = all.Count(e => e.IsResolved),
            FatalCount = all.Count(e => e.CriticalityLevel == ErrorCriticalityLevel.Fatal),
            CriticalCount = all.Count(e => e.CriticalityLevel == ErrorCriticalityLevel.Critical),
            ErrorCount = all.Count(e => e.CriticalityLevel == ErrorCriticalityLevel.Error),
            WarningCount = all.Count(e => e.CriticalityLevel == ErrorCriticalityLevel.Warning),
            ErrorsLast24Hours = all.Count(e => e.TimestampUtc >= now.AddHours(-24))
        };

        foreach (var group in all.GroupBy(e => e.ApplicationSource, StringComparer.OrdinalIgnoreCase))
        {
            stats.SourceBreakdown[group.Key] = group.Count();
        }

        stats.TopExceptions = all
            .GroupBy(e => e.ExceptionType)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new TopExceptionStat
            {
                ExceptionType = g.Key,
                Count = g.Count(),
                SampleMessage = g.First().ErrorMessage
            })
            .ToList();

        return stats;
    }

    private void SeedSampleIfEmpty()
    {
        if (_persistentStore != null) return;

        if (_entries.IsEmpty)
        {
            // Seed a representative sample entry so developer portal demonstrates rich metadata immediately
            var sampleId = Guid.NewGuid();
            var sample = new ErrorLogEntry
            {
                Id = sampleId,
                TimestampUtc = DateTime.UtcNow.AddMinutes(-12),
                TimestampLocal = DateTime.Now.AddMinutes(-12),
                CriticalityLevel = ErrorCriticalityLevel.Error,
                ApplicationSource = "HRM.Api",
                Environment = "Development",
                MachineName = System.Environment.MachineName,
                ProcessId = System.Environment.ProcessId,
                ThreadId = 14,
                TraceId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                Username = "ahmed.developer",
                UserId = "USR-2026-DEV-01",
                RemoteIp = "127.0.0.1",
                PageLocation = "/api/payroll/queue/claim",
                HttpMethod = "POST",
                StatusCode = 500,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/128.0.0.0",
                ExceptionType = "System.NullReferenceException",
                ErrorMessage = "Object reference not set to an instance of an object.",
                StackTrace = @"   at HRM.Services.Payroll.PayrollProcessor.ProcessRunBatchesAsync(Int32 orgId, Int64 payrollRunId, CancellationToken stoppingToken) in G:\NewEadmin\WebApp\HRM\HRM.Api\Services\Payroll\PayrollProcessor.cs:line 142
   at HRM.Payroll.Worker.Worker.ProcessJobWithHeartbeatAsync(PayrollBackgroundJob job, IPayrollQueueService queueService, IPayrollProcessor processor, CancellationToken stoppingToken) in G:\NewEadmin\WebApp\HRM\HRM.Payroll.Worker\Worker.cs:line 104",
                Location = new ErrorLocation
                {
                    FileName = "PayrollProcessor.cs",
                    FilePath = @"G:\NewEadmin\WebApp\HRM\HRM.Api\Services\Payroll\PayrollProcessor.cs",
                    LineNumber = 142,
                    ColumnNumber = 17,
                    ClassName = "PayrollProcessor",
                    MethodName = "ProcessRunBatchesAsync",
                    Namespace = "HRM.Services.Payroll",
                    AssemblyName = "HRM.Api"
                },
                Fix = new FixRecommendation
                {
                    RootCauseSummary = "An object reference evaluated to 'null' when accessed at line 142 in PayrollProcessor.cs. A database query returned null or an uninitialized collection was enumerated.",
                    RecommendedAction = "Inspect PayrollProcessor.cs around line 142. Add null-conditional operators ('?.'), null-coalescing defaults ('??'), or defensive guard clauses.",
                    DiagnosticSteps = new List<string>
                    {
                        "Open PayrollProcessor.cs and navigate to line 142.",
                        "Verify whether active payroll period or compensation snapshot for the organization exists in PAY_PayrollPeriods.",
                        "Add a null guard: 'if (batch == null) throw new InvalidOperationException(\"No payroll batch found\");'."
                    },
                    SuggestedCodePattern = @"var period = await _context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == periodId);
if (period == null)
{
    _logger.LogWarning(""Payroll period {Id} not found."", periodId);
    return;
}"
                },
                IsResolved = false
            };

            _entries[sampleId] = sample;
            _order.Enqueue(sampleId);
        }
    }
}
