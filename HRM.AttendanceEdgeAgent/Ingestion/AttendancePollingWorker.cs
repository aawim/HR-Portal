using HRM.AttendanceEdgeAgent.Adapters;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Diagnostics;
using HRM.AttendanceEdgeAgent.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AttendanceEdgeAgent.Ingestion;

public sealed class AttendancePollingWorker : BackgroundService
{
    private readonly IEnumerable<IDeviceAdapter> _adapters;
    private readonly IOptions<AttendanceEdgeOptions> _options;
    private readonly IDeviceCursorStore _cursorStore;
    private readonly IOfflineEventBuffer _outbox;
    private readonly IDeviceConnectivityProbe _probe;
    private readonly ILogger<AttendancePollingWorker> _logger;

    public AttendancePollingWorker(
        IEnumerable<IDeviceAdapter> adapters,
        IOptions<AttendanceEdgeOptions> options,
        IDeviceCursorStore cursorStore,
        IOfflineEventBuffer outbox,
        IDeviceConnectivityProbe probe,
        ILogger<AttendancePollingWorker> logger)
    {
        _adapters = adapters;
        _options = options;
        _cursorStore = cursorStore;
        _outbox = outbox;
        _probe = probe;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = _options.Value;
        if (string.IsNullOrWhiteSpace(config.AgentId) || string.IsNullOrWhiteSpace(config.OrganisationId))
        {
            throw new InvalidOperationException("AgentId and OrganisationId must be configured.");
        }

        _logger.LogInformation("Attendance polling worker started. Polling every {Seconds} seconds for {DeviceCount} configured device(s).",
            config.PollSeconds, config.Devices.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var enabledDevices = config.Devices.Where(d => d.Enabled).ToList();
                foreach (var device in enabledDevices)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    await PollDeviceAsync(device, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unexpected error in device polling cycle.");
            }

            var delaySeconds = Math.Max(5, config.PollSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }

    private async Task PollDeviceAsync(DeviceOptions device, CancellationToken ct)
    {
        var adapter = _adapters.FirstOrDefault(x => x.Name.Equals(device.Adapter, StringComparison.OrdinalIgnoreCase));
        if (adapter == null)
        {
            _logger.LogWarning("No adapter registered for type '{Adapter}' on device {DeviceId}.", device.Adapter, device.DeviceId);
            return;
        }

        // 1. Probe connectivity
        var status = await _probe.ProbeAsync(device, ct);
        if (!status.IsReachable)
        {
            _logger.LogWarning("Device {DeviceId} ({Host}:{Port}) is unreachable: {Error}",
                device.DeviceId, device.Host, device.Port, status.ErrorMessage);
            return;
        }

        // 2. Load persistent cursor
        var cursor = await _cursorStore.GetCursorAsync(device.DeviceId, ct) ?? new DeviceCursor
        {
            DeviceId = device.DeviceId,
            LastPollUtc = DateTime.UtcNow
        };

        // 3. Read events using adapter
        IReadOnlyList<DeviceAttendanceEvent> rawEvents;
        try
        {
            rawEvents = await adapter.ReadAsync(device, cursor, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Adapter '{Adapter}' failed while reading device {DeviceId}.", device.Adapter, device.DeviceId);
            return;
        }

        if (rawEvents.Count == 0)
        {
            cursor.LastPollUtc = DateTime.UtcNow;
            await _cursorStore.SaveCursorAsync(device.DeviceId, cursor, ct);
            return;
        }

        // 4. Transform to envelopes
        var envelopes = rawEvents.Select(x => new AttendanceEnvelope(
            DeviceId: x.DeviceId > 0 ? x.DeviceId : device.DeviceId,
            LocalUserId: x.LocalUserId,
            DeviceTimestamp: x.DeviceTimestamp,
            DeviceEventId: x.DeviceEventId,
            RawMetadata: x.RawMetadata)).ToList();

        // 5. Commit into offline buffer
        await _outbox.EnqueueAsync(envelopes, ct);

        // 6. Update high-water mark cursor
        var maxTimestamp = rawEvents.Max(e => e.DeviceTimestamp);
        var lastEvent = rawEvents.OrderByDescending(e => e.DeviceTimestamp).First();

        cursor.LastEventTimestampUtc = maxTimestamp;
        cursor.LastEventId = lastEvent.DeviceEventId;
        cursor.LastPollUtc = DateTime.UtcNow;
        cursor.TotalEventsIngested += rawEvents.Count;

        await _cursorStore.SaveCursorAsync(device.DeviceId, cursor, ct);
        _logger.LogInformation("Device {DeviceId} successfully polled: {Count} event(s) buffered. Cursor moved to {Timestamp:u}.",
            device.DeviceId, rawEvents.Count, maxTimestamp);
    }
}
