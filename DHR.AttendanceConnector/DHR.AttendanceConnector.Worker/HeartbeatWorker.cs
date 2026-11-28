using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Worker;

public sealed class HeartbeatWorker(
    IConnectorRepository repository,
    IDhrApiClient apiClient,
    ConnectorActivationService activationService,
    IOptions<ConnectorOptions> options,
    ILogger<HeartbeatWorker> logger) : BackgroundService
{
    private DateTime _lastConfigRefreshUtc = DateTime.MinValue;
    private DateTime _lastCertCheckUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await repository.InitializeAsync(stoppingToken);
        logger.LogInformation("HeartbeatWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = 60;
            try
            {
                var profile = await repository.GetProfileAsync(stoppingToken);
                if (profile is not null)
                {
                    intervalSeconds = Math.Clamp(profile.HeartbeatIntervalSeconds, 15, 600);
                    var metrics = await repository.GetSyncMetricsAsync(stoppingToken);
                    var devices = await repository.GetDevicesAsync(stoppingToken);

                    var healthStatus = "Online";
                    if (metrics.PendingCount >= 500 || metrics.QuarantinedCount > 0)
                        healthStatus = "SyncDelayed";
                    else if (devices.Any(d => d.Enabled && d.LastConnectionStatus == "Offline"))
                        healthStatus = "Degraded";

                    await apiClient.SendHeartbeatAsync(
                        profile.ApiBaseUrl,
                        new RemoteHeartbeatPayload(
                            AgentId: profile.AgentIdentifier,
                            HealthStatus: healthStatus,
                            ConnectorVersion: options.Value.ConnectorVersion,
                            OperatingSystem: System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                            PendingEventCount: metrics.PendingCount + metrics.UploadingCount,
                            ConfigurationVersion: profile.ConfigurationVersion,
                            LastSyncAtUtc: metrics.LastSuccessfulSyncUtc),
                        stoppingToken);

                    // Periodically pull authoritative configuration from DHR (every 5 minutes)
                    if (DateTime.UtcNow - _lastConfigRefreshUtc >= TimeSpan.FromMinutes(5))
                    {
                        _lastConfigRefreshUtc = DateTime.UtcNow;
                        await activationService.RefreshConfigurationAsync(stoppingToken);
                    }

                    // Periodically check certificate expiry for automatic rotation (every 6 hours)
                    if (DateTime.UtcNow - _lastCertCheckUtc >= TimeSpan.FromHours(6))
                    {
                        _lastCertCheckUtc = DateTime.UtcNow;
                        await activationService.RenewCredentialsIfNeededAsync(force: false, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Heartbeat or config sync failed.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        logger.LogInformation("HeartbeatWorker stopped gracefully.");
    }
}
