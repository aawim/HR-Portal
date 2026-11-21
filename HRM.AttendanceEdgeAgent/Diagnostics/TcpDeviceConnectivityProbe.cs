using System.Diagnostics;
using System.Net.Sockets;
using HRM.AttendanceEdgeAgent.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AttendanceEdgeAgent.Diagnostics;

public sealed class TcpDeviceConnectivityProbe : IDeviceConnectivityProbe
{
    private readonly int _timeoutSeconds;
    private readonly ILogger<TcpDeviceConnectivityProbe> _logger;

    public TcpDeviceConnectivityProbe(IOptions<AttendanceEdgeOptions> options, ILogger<TcpDeviceConnectivityProbe> logger)
    {
        _logger = logger;
        _timeoutSeconds = Math.Max(1, options.Value.DeviceTimeoutSeconds);
    }

    public async Task<DeviceConnectivityStatus> ProbeAsync(DeviceOptions device, CancellationToken ct = default)
    {
        // For file-drop adapters, probe the local or network folder path
        if (device.Adapter.Equals("generic-file", StringComparison.OrdinalIgnoreCase))
        {
            var dropPath = !string.IsNullOrWhiteSpace(device.DropPath) ? device.DropPath : device.Host;
            var exists = !string.IsNullOrWhiteSpace(dropPath) && Directory.Exists(dropPath);
            return new DeviceConnectivityStatus(
                device.DeviceId,
                dropPath ?? "",
                0,
                IsReachable: exists,
                LatencyMs: 0,
                ErrorMessage: exists ? null : $"Drop directory '{dropPath}' does not exist or is inaccessible.");
        }

        if (string.IsNullOrWhiteSpace(device.Host) || device.Port <= 0)
        {
            return new DeviceConnectivityStatus(
                device.DeviceId,
                device.Host,
                device.Port,
                IsReachable: false,
                LatencyMs: 0,
                ErrorMessage: "Host and valid port (> 0) must be specified for network device.");
        }

        var sw = Stopwatch.StartNew();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(device.Host, device.Port, cts.Token);
            sw.Stop();
            return new DeviceConnectivityStatus(
                device.DeviceId,
                device.Host,
                device.Port,
                IsReachable: true,
                LatencyMs: sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new DeviceConnectivityStatus(
                device.DeviceId,
                device.Host,
                device.Port,
                IsReachable: false,
                LatencyMs: sw.ElapsedMilliseconds,
                ErrorMessage: $"Connection timed out after {_timeoutSeconds} seconds.");
        }
        catch (Exception ex)
        {
            return new DeviceConnectivityStatus(
                device.DeviceId,
                device.Host,
                device.Port,
                IsReachable: false,
                LatencyMs: sw.ElapsedMilliseconds,
                ErrorMessage: ex.Message);
        }
    }
}
