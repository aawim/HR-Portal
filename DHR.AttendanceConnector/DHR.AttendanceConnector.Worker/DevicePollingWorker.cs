using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DHR.AttendanceConnector.Worker;

public sealed class DevicePollingWorker(
    IConnectorRepository repository,
    DeviceCollectionService collectionService,
    ILogger<DevicePollingWorker> logger) : BackgroundService
{
    private readonly Dictionary<int, DateTime> _lastPolledUtc = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await repository.InitializeAsync(stoppingToken);
        logger.LogInformation("DevicePollingWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var profile = await repository.GetProfileAsync(stoppingToken);
                if (profile is not null)
                {
                    var devices = await repository.GetDevicesAsync(stoppingToken);
                    var now = DateTime.UtcNow;

                    foreach (var device in devices.Where(d => d.Enabled))
                    {
                        var interval = TimeSpan.FromSeconds(Math.Clamp(device.PollingIntervalSeconds, 5, 3600));
                        if (!_lastPolledUtc.TryGetValue(device.DeviceId, out var last) || (now - last) >= interval)
                        {
                            _lastPolledUtc[device.DeviceId] = now;
                            await collectionService.PollSingleDeviceAsync(profile.AgentIdentifier, device, stoppingToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in DevicePollingWorker loop.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        logger.LogInformation("DevicePollingWorker stopped gracefully.");
    }
}
