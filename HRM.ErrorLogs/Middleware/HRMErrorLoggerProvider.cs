using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRM.ErrorLogs.Middleware;

/// <summary>
/// Intercepts Error and Critical logs throughout Microsoft.Extensions.Logging and pipes them to ErrorLogService.
/// </summary>
public sealed class HRMErrorLoggerProvider : ILoggerProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _applicationSource;

    public HRMErrorLoggerProvider(IServiceProvider serviceProvider, string applicationSource = "HRM.Api")
    {
        _serviceProvider = serviceProvider;
        _applicationSource = applicationSource;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new HRMErrorLogger(categoryName, _serviceProvider, _applicationSource);
    }

    public void Dispose() { }

    private sealed class HRMErrorLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _applicationSource;
        private static readonly AsyncLocal<bool> InLogging = new();

        public HRMErrorLogger(string categoryName, IServiceProvider serviceProvider, string applicationSource)
        {
            _categoryName = categoryName;
            _serviceProvider = serviceProvider;
            _applicationSource = applicationSource;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel < LogLevel.Error || exception == null) return;
            if (exception is OperationCanceledException) return; // Ignore normal task cancellations
            if (InLogging.Value) return; // Prevent recursive loops

            // Filter out ErrorLogService internal logs to avoid feedback
            if (_categoryName.Contains("ErrorLog", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                InLogging.Value = true;
                using var scope = _serviceProvider.CreateScope();
                var errorService = scope.ServiceProvider.GetService<IErrorLogService>();
                if (errorService == null) return;

                var criticality = logLevel == LogLevel.Critical
                    ? ErrorCriticalityLevel.Critical
                    : ErrorCriticalityLevel.Error;

                var context = new ErrorContext
                {
                    ApplicationSource = _applicationSource,
                    CustomDataJson = $"{{\"Category\": \"{_categoryName}\", \"EventId\": {eventId.Id}}}"
                };

                _ = errorService.CaptureExceptionAsync(exception, criticality, context);
            }
            catch
            {
                // Never allow logging provider failure to bubble up
            }
            finally
            {
                InLogging.Value = false;
            }
        }
    }
}
