using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Diagnostics;
using HRM.AttendanceEdgeAgent.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AttendanceEdgeAgent.Ingestion;

public sealed class HeartbeatWorker : BackgroundService
{
    private readonly DhrIngestionClient _ingestion;
    private readonly IOptions<AttendanceEdgeOptions> _options;
    private readonly IOfflineEventBuffer _outbox;
    private readonly IDeviceConnectivityProbe _probe;
    private readonly ILogger<HeartbeatWorker> _logger;

    public HeartbeatWorker(
        DhrIngestionClient ingestion,
        IOptions<AttendanceEdgeOptions> options,
        IOfflineEventBuffer outbox,
        IDeviceConnectivityProbe probe,
        ILogger<HeartbeatWorker> logger)
    {
        _ingestion = ingestion;
        _options = options;
        _outbox = outbox;
        _probe = probe;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(10, _options.Value.HeartbeatIntervalSeconds);
        _logger.LogInformation("Heartbeat worker started. Reporting health every {Seconds} seconds.", intervalSeconds);

        // Send initial heartbeat on startup
        await ReportHeartbeatAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            await ReportHeartbeatAsync(stoppingToken);
        }
    }

    private async Task ReportHeartbeatAsync(CancellationToken ct)
    {
        try
        {
            var isDegraded = false;
            var config = _options.Value;

            // Check device reachability
            var enabledDevices = config.Devices.Where(d => d.Enabled).ToList();
            foreach (var device in enabledDevices)
            {
                var status = await _probe.ProbeAsync(device, ct);
                if (!status.IsReachable)
                {
                    isDegraded = true;
                    _logger.LogWarning("Heartbeat detected device {DeviceId} is offline: {Error}", device.DeviceId, status.ErrorMessage);
                }
            }

            // Check outbox backlog
            var stats = await _outbox.GetStatisticsAsync(ct);
            if (stats.PendingBatches > 100)
            {
                isDegraded = true;
                _logger.LogWarning("Heartbeat detected large outbox backlog: {Batches} batches ({Events} events) pending delivery.",
                    stats.PendingBatches, stats.PendingEvents);
            }

            var healthStatus = isDegraded ? "Degraded" : "Online";
            await _ingestion.HeartbeatAsync(healthStatus, ct);
            _logger.LogDebug("Heartbeat dispatched to DHR: Status={Status}, OutboxPending={Pending}.", healthStatus, stats.PendingEvents);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Failed to dispatch heartbeat to DHR server.");
        }
    }
}
