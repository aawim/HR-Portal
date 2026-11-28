using System.Net;
using System.Net.Sockets;
using System.Text;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Devices.Abstractions;
using DHR.AttendanceConnector.Devices.GenericTcp;
using DHR.AttendanceConnector.Devices.Simulator;
using DHR.AttendanceConnector.Devices.ZKTeco;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DHR.AttendanceConnector.Tests;

public sealed class DeviceIntegrationTests
{
    private static IOptions<ConnectorOptions> DevOptions() => Options.Create(new ConnectorOptions
    {
        EnableDevelopmentSimulator = true,
        DeviceTimeoutSeconds = 2
    });

    [Fact]
    public async Task Simulator_SuccessfulConnection_And_Reconnection_AfterOffline()
    {
        var sim = new VirtualBiometricDeviceSimulator(DevOptions());
        var device = new DeviceConfiguration(101, "Main Entrance", "192.168.10.21", 4370, "simulator", 30, true);

        // 1. Online
        var res1 = await sim.TestConnectionAsync(device);
        Assert.True(res1.IsReachable);
        Assert.NotNull(res1.FirmwareVersion);

        // 2. Disconnection / offline
        sim.ConfigureDeviceState(101, isOffline: true);
        var res2 = await sim.TestConnectionAsync(device);
        Assert.False(res2.IsReachable);
        await Assert.ThrowsAsync<TimeoutException>(() => sim.ReadAttendanceLogsAsync(device, null, "conn-1"));

        // 3. Reconnection
        sim.ConfigureDeviceState(101, isOffline: false);
        sim.AddPunches(101, [new SimulatedPunch("1001", DateTime.UtcNow.AddMinutes(-1), "EVT-RECONN-1")]);
        var res3 = await sim.TestConnectionAsync(device);
        Assert.True(res3.IsReachable);
        var logs = await sim.ReadAttendanceLogsAsync(device, null, "conn-1");
        Assert.Single(logs);
    }

    [Fact]
    public async Task Simulator_ProtocolError_ThrowsInvalidDataException()
    {
        var sim = new VirtualBiometricDeviceSimulator(DevOptions());
        var device = new DeviceConfiguration(102, "Annex Gate", "192.168.10.22", 4370, "simulator", 30, true);
        sim.ConfigureDeviceState(102, throwProtocolError: true);

        var test = await sim.TestConnectionAsync(device);
        Assert.False(test.IsReachable);
        await Assert.ThrowsAsync<InvalidDataException>(() => sim.ReadAttendanceLogsAsync(device, null, "conn-1"));
    }

    [Fact]
    public async Task ZkTecoAdapter_InvalidDeviceConfiguration_ReturnsFailure()
    {
        var adapter = new ZkTecoTcpDeviceAdapter(DevOptions(), NullLogger<ZkTecoTcpDeviceAdapter>.Instance);
        var invalidDevice = new DeviceConfiguration(103, "Broken", "", 4370, "zkteco", 30, true);

        var result = await adapter.TestConnectionAsync(invalidDevice);
        Assert.False(result.IsReachable);
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.ReadAttendanceLogsAsync(invalidDevice, null, "conn-1"));
    }

    [Fact]
    public async Task ZkTecoAdapter_UnreachablePort_TimesOutGracefully()
    {
        var adapter = new ZkTecoTcpDeviceAdapter(DevOptions(), NullLogger<ZkTecoTcpDeviceAdapter>.Instance);
        var unreachable = new DeviceConfiguration(104, "Offline ZK", "127.0.0.1", 1, "zkteco", 30, true);

        var result = await adapter.TestConnectionAsync(unreachable);
        Assert.False(result.IsReachable);
    }

    [Fact]
    public async Task GenericTcpAdapter_ConnectsAndReadsEventsFromLoopbackServer()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            // Handle 2 connections: PING and READ_LOGS
            for (var i = 0; i < 2; i++)
            {
                using var client = await listener.AcceptTcpClientAsync();
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
                var line = await reader.ReadLineAsync();
                if (line == "PING")
                {
                    await writer.WriteLineAsync("PONG");
                }
                else if (line is not null && line.StartsWith("READ_LOGS"))
                {
                    await writer.WriteLineAsync("""{"UserId":"E-555","TimestampUtc":"2026-11-19T03:05:00Z","EventId":"TCP-1","PunchStatus":"0"}""");
                    await writer.WriteLineAsync("END");
                }
            }
        });

        var adapter = new GenericTcpDeviceAdapter(DevOptions());
        var device = new DeviceConfiguration(201, "TCP Terminal", "127.0.0.1", port, "generic-tcp", 30, true);

        var test = await adapter.TestConnectionAsync(device);
        Assert.True(test.IsReachable);

        var logs = await adapter.ReadAttendanceLogsAsync(device, null, "conn-1");
        Assert.Single(logs);
        Assert.Equal("E-555", logs[0].DeviceUserId);
        Assert.Equal("TCP-1", logs[0].DeviceEventId);

        await serverTask;
    }

    [Fact]
    public void AdapterRegistry_ResolvesProtocolsCaseInsensitively()
    {
        var registry = new DeviceAdapterRegistry([
            new ZkTecoTcpDeviceAdapter(DevOptions(), NullLogger<ZkTecoTcpDeviceAdapter>.Instance),
            new GenericTcpDeviceAdapter(DevOptions()),
            new VirtualBiometricDeviceSimulator(DevOptions())
        ]);

        Assert.NotNull(registry.Resolve("ZKTECO"));
        Assert.NotNull(registry.Resolve("generic-tcp"));
        Assert.NotNull(registry.Resolve("simulator"));
        Assert.Null(registry.Resolve("unknown-vendor"));
    }
}
