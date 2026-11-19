using System.Security.Cryptography;
using System.Text;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Core.Services;

public sealed class DeviceCollectionService(
    IConnectorRepository repository,
    IDeviceAdapterRegistry adapterRegistry,
    IOptions<ConnectorOptions> options,
    ILogger<DeviceCollectionService> logger)
{
    public async Task<int> PollAllDevicesAsync(CancellationToken ct = default)
    {
        var profile = await repository.GetProfileAsync(ct);
        if (profile is null) return 0;

        var devices = await repository.GetDevicesAsync(ct);
        var enabled = devices.Where(d => d.Enabled).ToList();
        if (enabled.Count == 0) return 0;

        var maxConcurrency = Math.Clamp(options.Value.MaxConcurrentDevicePolls, 1, 16);
        using var gate = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var totalCollected = 0;

        var tasks = enabled.Select(async device =>
        {
            await gate.WaitAsync(ct);
            try
            {
                var count = await PollSingleDeviceAsync(profile.AgentIdentifier, device, ct);
                Interlocked.Add(ref totalCollected, count);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
        return totalCollected;
    }

    public async Task<int> PollSingleDeviceAsync(string connectorId, DeviceConfiguration device, CancellationToken ct = default)
    {
        var adapter = adapterRegistry.Resolve(device.ProtocolCode);
        if (adapter is null)
        {
            var err = $"Unsupported device protocol '{device.ProtocolCode}'.";
            await repository.UpdateDeviceHealthAsync(device.DeviceId, "ProtocolError", null, null, err, ct);
            await repository.WriteHealthLogAsync("Warning", "DevicePolling", err, $"Device #{device.DeviceId} ({device.Name})", ct);
            return 0;
        }

        try
        {
            var checkpoint = await repository.GetCheckpointAsync(device.DeviceId, ct);
            var events = await adapter.ReadAttendanceLogsAsync(device, checkpoint, connectorId, ct);
            var now = DateTime.UtcNow;

            if (events.Count > 0)
            {
                var persisted = await repository.PersistCollectedEventsAsync(events, ct);
                // Guard checkpoint against corrupt far-future device RTC records (e.g. year 2103)
                var validMaxEvent = events
                    .Where(e => e.DeviceTimestampUtc <= now.AddMinutes(5))
                    .OrderByDescending(e => e.DeviceTimestampUtc)
                    .FirstOrDefault();

                if (validMaxEvent is not null)
                {
                    var newCheckpoint = new DeviceSyncCheckpoint(
                        DeviceId: device.DeviceId,
                        LastEventId: validMaxEvent.DeviceEventId,
                        LastEventTimestampUtc: validMaxEvent.DeviceTimestampUtc,
                        TotalEventsCollected: (checkpoint?.TotalEventsCollected ?? 0) + persisted,
                        UpdatedAtUtc: now);

                    await repository.SaveCheckpointAsync(newCheckpoint, ct);
                }

                await repository.UpdateDeviceHealthAsync(device.DeviceId, "Online", now, validMaxEvent?.DeviceTimestampUtc ?? checkpoint?.LastEventTimestampUtc, null, ct);
                if (persisted > 0)
                {
                    await repository.WriteHealthLogAsync("Info", "DevicePolling", $"Collected {persisted} new event(s) from {device.Name}.", ct: ct);
                }
                return persisted;
            }
            else
            {
                await repository.UpdateDeviceHealthAsync(device.DeviceId, "Online", now, checkpoint?.LastEventTimestampUtc, null, ct);
                return 0;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Polling failed for device #{DeviceId} ({Name})", device.DeviceId, device.Name);
            await repository.UpdateDeviceHealthAsync(device.DeviceId, "Offline", null, null, ex.Message, ct);
            await repository.WriteHealthLogAsync("Warning", "DevicePolling", $"Failed to poll {device.Name}: {ex.Message}", ct: ct);
            return 0;
        }
    }

    public async Task<DeviceConnectionResult> TestDeviceAsync(int deviceId, CancellationToken ct = default)
    {
        var devices = await repository.GetDevicesAsync(ct);
        var device = devices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device is null)
            return new DeviceConnectionResult(false, 0, $"Device #{deviceId} is not registered on this connector.");

        var adapter = adapterRegistry.Resolve(device.ProtocolCode);
        if (adapter is null)
            return new DeviceConnectionResult(false, 0, $"Unsupported protocol '{device.ProtocolCode}'.");

        var result = await adapter.TestConnectionAsync(device, ct);
        await repository.UpdateDeviceHealthAsync(
            device.DeviceId,
            result.IsReachable ? "Online" : "Offline",
            result.IsReachable ? DateTime.UtcNow : null,
            device.LastEventTimestampUtc,
            result.IsReachable ? null : result.ErrorMessage,
            ct);
        return result;
    }

    public static string ComputeDeduplicationKey(string connectorId, int deviceId, string deviceUserId, DateTime deviceTimestampUtc, string deviceEventId)
    {
        var raw = $"{connectorId.Trim()}|{deviceId}|{deviceUserId.Trim()}|{deviceTimestampUtc.ToUniversalTime().Ticks}|{deviceEventId.Trim()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
