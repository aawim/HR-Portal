using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Services;
using DHR.AttendanceConnector.Infrastructure.Networking;
using DHR.AttendanceConnector.Infrastructure.Persistence;
using DHR.AttendanceConnector.Infrastructure.Repositories;
using DHR.AttendanceConnector.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DHR.AttendanceConnector.Infrastructure;

public static class ConnectorServiceCollectionExtensions
{
    public static IServiceCollection AddDhrConnectorCoreAndInfrastructure(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<ConnectorOptions>? configureOptions = null)
    {
        if (configuration is not null)
        {
            services.Configure<ConnectorOptions>(configuration.GetSection(ConnectorOptions.SectionName));
        }

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddSingleton<SqliteConnectorDatabase>();
        services.AddSingleton<IConnectorRepository, SqliteConnectorRepository>();
        services.AddSingleton<ISecretStore, OsProtectedSecretStore>();
        services.AddSingleton<IDeviceIdentityKeyManager, DeviceIdentityKeyManager>();
        services.AddSingleton<IDhrApiClient, DhrConnectorHttpClient>();

        services.AddSingleton<ConnectorActivationService>();
        services.AddSingleton<DeviceCollectionService>();
        services.AddSingleton<AttendanceSyncService>();
        services.AddSingleton<DiagnosticsExportService>();
        services.AddSingleton<CommandExecutionService>();

        return services;
    }
}
