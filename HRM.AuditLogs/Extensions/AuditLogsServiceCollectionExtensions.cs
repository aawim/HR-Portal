using HRM.AuditLogs.Abstractions;
using HRM.AuditLogs.Interceptors;
using HRM.AuditLogs.Options;
using HRM.AuditLogs.Persistence;
using HRM.AuditLogs.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HRM.AuditLogs.Extensions;

public static class AuditLogsServiceCollectionExtensions
{
    public static IServiceCollection AddAuditLogs(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AuditLogOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new AuditLogOptions();
        configuration.GetSection(AuditLogOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        services.Configure<AuditLogOptions>(opt =>
        {
            configuration.GetSection(AuditLogOptions.SectionName).Bind(opt);
            configure?.Invoke(opt);
        });

        var connectionString = !string.IsNullOrWhiteSpace(options.ConnectionString)
            ? options.ConnectionString
            : configuration.GetConnectionString(options.ConnectionStringName);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContextFactory<AuditDbContext>(db =>
                db.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory_AuditLogs", options.Schema);
                    sql.EnableRetryOnFailure(maxRetryCount: 3);
                }));
        }

        services.TryAddSingleton<IAuditClock, SystemAuditClock>();
        services.TryAddSingleton<IAuditContextAccessor, DefaultAuditContextAccessor>();
        services.TryAddSingleton<IAuditLogService, AuditLogService>();
        services.TryAddSingleton<IAuditLogQueryService, AuditLogQueryService>();
        services.TryAddSingleton<AuditSaveChangesInterceptor>();

        return services;
    }

    private sealed class DefaultAuditContextAccessor : IAuditContextAccessor
    {
        public AuditActorContext? GetCurrentContext() => AuditActorContext.System();
        public Task<AuditActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(AuditActorContext.System());
    }
}
