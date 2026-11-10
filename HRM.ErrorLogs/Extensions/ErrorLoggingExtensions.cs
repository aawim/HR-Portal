using HRM.ErrorLogs.Middleware;
using HRM.ErrorLogs.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HRM.ErrorLogs.Extensions;

public static class ErrorLoggingExtensions
{
    /// <summary>
    /// Registers the HRM Error Logging service and diagnostic engine into the DI container.
    /// </summary>
    public static IServiceCollection AddHrmErrorLogging(
        this IServiceCollection services,
        string applicationSource = "HRM.Api")
    {
        // High-performance bounded channel queue and batch background processor
        services.AddSingleton<IErrorLogQueue, ChannelErrorLogQueue>();
        services.AddHostedService<ErrorLogBackgroundProcessor>();

        services.AddSingleton<IErrorLogService>(sp =>
        {
            var store = sp.GetService<IErrorLogStore>();
            var queue = sp.GetRequiredService<IErrorLogQueue>();
            var logger = sp.GetService<ILogger<ErrorLogService>>();
            return new ErrorLogService(store, queue, logger, applicationSource);
        });

        // Register logger provider so standard ILogger.LogError feeds into ErrorLogService
        services.AddSingleton<ILoggerProvider>(sp => new HRMErrorLoggerProvider(sp, applicationSource));

        return services;
    }

    /// <summary>
    /// Adds the unhandled HTTP exception capturing middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseHrmErrorLogging(
        this IApplicationBuilder app,
        string applicationSource = "HRM.Api")
    {
        return app.UseMiddleware<ErrorLoggingMiddleware>(applicationSource);
    }
}
