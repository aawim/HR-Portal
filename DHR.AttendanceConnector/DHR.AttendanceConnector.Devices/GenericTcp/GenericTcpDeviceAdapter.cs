using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Devices.GenericTcp;

/// <summary>
/// Vendor-neutral TCP line-delimited JSON adapter for modern IP biometric terminals or gateways.
/// Protocol: Client sends "PING\n" or "READ_LOGS <iso-utc-since>\n", terminal replies with JSON lines terminated by "END\n".
/// </summary>
public sealed class GenericTcpDeviceAdapter(IOptions<ConnectorOptions> options) : IBiometricDeviceAdapter
{
    public string ProtocolCode => "generic-tcp";

    private TimeSpan Timeout => TimeSpan.FromSeconds(Math.Clamp(options.Value.DeviceTimeoutSeconds, 2, 30));

    public async Task<DeviceConnectionResult> TestConnectionAsync(DeviceConfiguration device, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress) || device.Port <= 0)
            return new DeviceConnectionResult(false, 0, "Valid IP address and port are required.");

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Timeout);

            await client.ConnectAsync(device.IpAddress, device.Port, timeoutCts.Token);
            await using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

            await writer.WriteLineAsync("PING".AsMemory(), timeoutCts.Token);
            var line = await reader.ReadLineAsync(timeoutCts.Token);
            sw.Stop();

            return string.Equals(line?.Trim(), "PONG", StringComparison.OrdinalIgnoreCase)
                ? new DeviceConnectionResult(true, sw.ElapsedMilliseconds)
                : new DeviceConnectionResult(false, sw.ElapsedMilliseconds, $"Unexpected response: '{line ?? "<eof>"}'");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DeviceConnectionResult(false, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<DeviceInformation> GetDeviceInformationAsync(DeviceConfiguration device, CancellationToken cancellationToken = default)
    {
        var result = await TestConnectionAsync(device, cancellationToken);
        if (!result.IsReachable)
            throw new InvalidOperationException(result.ErrorMessage ?? "Device unreachable.");

        return new DeviceInformation("Generic", "TCP-JSON Terminal", $"TCP-{device.DeviceId}", "1.0");
    }

    public async Task<IReadOnlyList<RawAttendanceEvent>> ReadAttendanceLogsAsync(
        DeviceConfiguration device,
        DeviceSyncCheckpoint? checkpoint,
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress) || device.Port <= 0)
            throw new InvalidOperationException("Valid IP address and port are required.");

        using var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Timeout);

        await client.ConnectAsync(device.IpAddress, device.Port, timeoutCts.Token);
        await using var stream = client.GetStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        var since = checkpoint?.LastEventTimestampUtc?.ToString("O") ?? "1970-01-01T00:00:00Z";
        await writer.WriteLineAsync($"READ_LOGS {since}".AsMemory(), timeoutCts.Token);

        var results = new List<RawAttendanceEvent>();
        var now = DateTime.UtcNow;

        while (!timeoutCts.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(timeoutCts.Token);
            if (line is null || line.Equals("END", StringComparison.OrdinalIgnoreCase))
                break;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var item = JsonSerializer.Deserialize<GenericTcpLogLine>(line);
            if (item is null || string.IsNullOrWhiteSpace(item.UserId))
                continue;

            var tsUtc = item.TimestampUtc.ToUniversalTime();
            if (checkpoint?.LastEventTimestampUtc is not null && tsUtc <= checkpoint.LastEventTimestampUtc.Value)
                continue;

            var eventId = string.IsNullOrWhiteSpace(item.EventId) ? $"{item.UserId}-{tsUtc:yyyyMMddHHmmss}" : item.EventId.Trim();
            var dedup = DeviceCollectionService.ComputeDeduplicationKey(connectorId, device.DeviceId, item.UserId, tsUtc, eventId);
            results.Add(new RawAttendanceEvent(
                0, connectorId, device.DeviceId, item.UserId.Trim(), tsUtc, eventId, item.PunchStatus,
                ProtocolCode, item.RawMetadata, now, dedup, OutboxEventState.Collected));
        }

        return results;
    }

    private sealed record GenericTcpLogLine(string UserId, DateTime TimestampUtc, string? EventId, string? PunchStatus, string? RawMetadata);
}
