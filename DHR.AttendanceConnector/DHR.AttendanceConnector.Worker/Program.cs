using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Services;
using DHR.AttendanceConnector.Devices.Abstractions;
using DHR.AttendanceConnector.Devices.GenericTcp;
using DHR.AttendanceConnector.Devices.Simulator;
using DHR.AttendanceConnector.Devices.ZKTeco;
using DHR.AttendanceConnector.Infrastructure;
using DHR.AttendanceConnector.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "DHR_CONNECTOR_");

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DHR Attendance Connector";
});
builder.Services.AddSystemd();

builder.Services.AddDhrConnectorCoreAndInfrastructure(builder.Configuration);

// Vendor-independent biometric device adapters
builder.Services.AddSingleton<IBiometricDeviceAdapter, ZkTecoTcpDeviceAdapter>();
builder.Services.AddSingleton<IBiometricDeviceAdapter, GenericTcpDeviceAdapter>();
builder.Services.AddSingleton<VirtualBiometricDeviceSimulator>();
builder.Services.AddSingleton<IBiometricDeviceAdapter>(sp => sp.GetRequiredService<VirtualBiometricDeviceSimulator>());
builder.Services.AddSingleton<IDeviceAdapterRegistry, DeviceAdapterRegistry>();

// CLI subcommands (do not start background workers for one-shot CLI operations)
var isCli = args.Length > 0 && args[0].ToLowerInvariant() is "activate" or "status" or "diagnostics";
if (!isCli)
{
    builder.Services.AddHostedService<DevicePollingWorker>();
    builder.Services.AddHostedService<AttendanceSyncWorker>();
    builder.Services.AddHostedService<HeartbeatWorker>();
    builder.Services.AddHostedService<CommandWorker>();
}

var host = builder.Build();
var repo = host.Services.GetRequiredService<IConnectorRepository>();
await repo.InitializeAsync();

if (isCli)
{
    switch (args[0].ToLowerInvariant())
    {
        case "activate":
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: DHR.AttendanceConnector.Worker activate <https-api-url> <DHR-ACT-XXXX-XXXX> [connector-name]");
                return 1;
            }
            var activation = host.Services.GetRequiredService<ConnectorActivationService>();
            var profile = await activation.ActivateAsync(args[1], args[2], args.Length > 3 ? args[3] : null);
            Console.WriteLine($"Activated connector '{profile.AgentIdentifier}' for {profile.OrganisationName} (Config v{profile.ConfigurationVersion}).");
            return 0;
        }
        case "status":
        {
            var profile = await repo.GetProfileAsync();
            var metrics = await repo.GetSyncMetricsAsync();
            var devices = await repo.GetDevicesAsync();
            Console.WriteLine($"Connector:    {profile?.AgentIdentifier ?? "Not Activated"}");
            Console.WriteLine($"Organisation: {profile?.OrganisationName ?? "-"}");
            Console.WriteLine($"Config Ver:   {profile?.ConfigurationVersion ?? 0}");
            Console.WriteLine($"Devices:      {devices.Count} registered");
            Console.WriteLine($"Queue:        {metrics.PendingCount} pending, {metrics.AcknowledgedCount} uploaded, {metrics.QuarantinedCount} quarantined");
            Console.WriteLine($"Last Sync:    {metrics.LastSuccessfulSyncUtc?.ToString("u") ?? "Never"}");
            return 0;
        }
        case "diagnostics":
        {
            var diag = host.Services.GetRequiredService<DiagnosticsExportService>();
            var path = await diag.ExportSanitisedZipAsync();
            Console.WriteLine($"Sanitised diagnostic package exported to: {path}");
            return 0;
        }
    }
}

await host.RunAsync();
return 0;
