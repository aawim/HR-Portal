using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using HRM.ActivityLogs.Persistence;
using HRM.ActivityLogs.Processing;
using HRM.ActivityLogs.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HRM.ActivityLogs.Extensions;

public static class ActivityLogsServiceCollectionExtensions
{
    public static IServiceCollection AddActivityLogs(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeBackgroundWorkers = true,
        Action<ActivityLogOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ActivityLogOptions();
        configuration.GetSection(ActivityLogOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        services.Configure<ActivityLogOptions>(opt =>
        {
            configuration.GetSection(ActivityLogOptions.SectionName).Bind(opt);
            configure?.Invoke(opt);
        });

        var connectionString = !string.IsNullOrWhiteSpace(options.ConnectionString)
            ? options.ConnectionString
            : configuration.GetConnectionString(options.ConnectionStringName);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContextFactory<ActivityDbContext>(db =>
                db.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory_ActivityLogs", options.Schema);
                    sql.EnableRetryOnFailure(maxRetryCount: 3);
                }));
        }

        services.TryAddSingleton<IActivityClock, SystemActivityClock>();
        services.TryAddSingleton<IAuditlessClockAdapter>();
        services.TryAddSingleton<IActivityContextAccessor, DefaultActivityContextAccessor>();
        services.TryAddSingleton<ActivityQueueChannel>();
        services.TryAddSingleton<IActivityLogService, ActivityLogService>();
        services.TryAddSingleton<IActivityLogQueryService, ActivityLogQueryService>();

        if (includeBackgroundWorkers)
        {
            services.AddHostedService<ActivityEventProcessor>();
            services.AddHostedService<ActivityRetentionProcessor>();
        }

        return services;
    }

    private sealed class DefaultActivityContextAccessor : IActivityContextAccessor
    {
        public Task<ActivityActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ActivityActorContext.System());
    }
}
