using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Devices.ZKTeco;

/// <summary>
/// Cross-platform native TCP adapter for ZKTeco and compatible biometric terminals (default port 4370).
/// Refactored from HRM.AttendanceEdgeAgent's proven binary protocol framing; requires no Windows-only DLLs.
/// Device-log cleanup is intentionally never invoked.
/// </summary>
public sealed class ZkTecoTcpDeviceAdapter(
    IOptions<ConnectorOptions> options,
    ILogger<ZkTecoTcpDeviceAdapter> logger) : IBiometricDeviceAdapter
{
    public string ProtocolCode => "zkteco";

    private TimeSpan Timeout => TimeSpan.FromSeconds(Math.Clamp(options.Value.DeviceTimeoutSeconds, 2, 30));

    public async Task<DeviceConnectionResult> TestConnectionAsync(DeviceConfiguration device, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress))
            return new DeviceConnectionResult(false, 0, "Device IP address is empty.");

        var port = device.Port > 0 ? device.Port : 4370;
        var sw = Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Timeout);

            await client.ConnectAsync(device.IpAddress, port, timeoutCts.Token);
            await using var stream = client.GetStream();

            var sessionId = await ConnectSessionAsync(stream, timeoutCts.Token);
            sw.Stop();

            if (sessionId is null)
                return new DeviceConnectionResult(false, sw.ElapsedMilliseconds, "ZKTeco terminal rejected connection handshake.");

            string? firmware = null;
            try
            {
                firmware = await TryReadStringCommandAsync(stream, sessionId.Value, 1100, 1, [], timeoutCts.Token); // CMD_GET_VERSION
            }
            catch
            {
                // Some firmware variants omit CMD_GET_VERSION
            }
            finally
            {
                try { await DisconnectSessionAsync(stream, sessionId.Value, CancellationToken.None); }
                catch { /* ignore */ }
            }

            return new DeviceConnectionResult(true, sw.ElapsedMilliseconds, null, firmware);
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or OperationCanceledException)
        {
            sw.Stop();
            return new DeviceConnectionResult(false, sw.ElapsedMilliseconds, $"Connection failed or timed out: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Unexpected error testing ZKTeco device {Ip}:{Port}", device.IpAddress, port);
            return new DeviceConnectionResult(false, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<DeviceInformation> GetDeviceInformationAsync(DeviceConfiguration device, CancellationToken cancellationToken = default)
    {
        var port = device.Port > 0 ? device.Port : 4370;
        using var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Timeout);

        await client.ConnectAsync(device.IpAddress, port, timeoutCts.Token);
        await using var stream = client.GetStream();

        var sessionId = await ConnectSessionAsync(stream, timeoutCts.Token)
            ?? throw new InvalidOperationException("ZKTeco terminal rejected connection handshake.");

        try
        {
            var firmware = await TryReadStringCommandAsync(stream, sessionId, 1100, 1, [], timeoutCts.Token) ?? "Unknown";
            var serial = await TryReadStringCommandAsync(stream, sessionId, 11, 2, Encoding.ASCII.GetBytes("~SerialNumber\0"), timeoutCts.Token) ?? "Unknown";
            var model = await TryReadStringCommandAsync(stream, sessionId, 11, 3, Encoding.ASCII.GetBytes("~DeviceName\0"), timeoutCts.Token) ?? "Unknown";

            return new DeviceInformation(
                Vendor: "ZKTeco",
                Model: model.Replace("~DeviceName=", "", StringComparison.OrdinalIgnoreCase).Trim('\0', ' '),
                SerialNumber: serial.Replace("~SerialNumber=", "", StringComparison.OrdinalIgnoreCase).Trim('\0', ' '),
                FirmwareVersion: firmware.Trim('\0', ' '),
                DeviceClockUtc: null,
                StoredLogCount: null);
        }
        finally
        {
            try { await DisconnectSessionAsync(stream, sessionId, CancellationToken.None); }
            catch { /* ignore */ }
        }
    }

    public async Task<IReadOnlyList<RawAttendanceEvent>> ReadAttendanceLogsAsync(
        DeviceConfiguration device,
        DeviceSyncCheckpoint? checkpoint,
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(device.IpAddress))
            throw new InvalidOperationException($"Device #{device.DeviceId} has no configured IP address.");

        var port = device.Port > 0 ? device.Port : 4370;

        using var client = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Timeout);

        await client.ConnectAsync(device.IpAddress, port, timeoutCts.Token);
        await using var stream = client.GetStream();

        var sessionId = await ConnectSessionAsync(stream, timeoutCts.Token)
            ?? throw new InvalidOperationException($"ZKTeco terminal at {device.IpAddress}:{port} refused handshake.");

        try
        {
            return await FetchAttendanceLogsAsync(stream, sessionId, device.DeviceId, checkpoint, connectorId, timeoutCts.Token);
        }
        finally
        {
            try { await DisconnectSessionAsync(stream, sessionId, CancellationToken.None); }
            catch { /* ignore clean exit failures */ }
        }
    }

    private static async Task<ushort?> ConnectSessionAsync(NetworkStream stream, CancellationToken ct)
    {
        var connectPacket = BuildPacket(1000, 0, 0, []);
        await stream.WriteAsync(connectPacket, ct);

        var response = await ReadPacketAsync(stream, ct);
        if (response is null || response.Length < 16) return null;

        var replyCode = BitConverter.ToUInt16(response, 8);
        var sessionId = BitConverter.ToUInt16(response, 12);
        return replyCode == 2000 ? sessionId : null;
    }

    private static async Task DisconnectSessionAsync(NetworkStream stream, ushort sessionId, CancellationToken ct)
    {
        var exitPacket = BuildPacket(1001, sessionId, 99, []);
        await stream.WriteAsync(exitPacket, ct);
    }

    private static async Task<string?> TryReadStringCommandAsync(
        NetworkStream stream,
        ushort sessionId,
        ushort command,
        ushort replyId,
        byte[] payload,
        CancellationToken ct)
    {
        var packet = BuildPacket(command, sessionId, replyId, payload);
        await stream.WriteAsync(packet, ct);
        var response = await ReadPacketAsync(stream, ct);
        if (response is null || response.Length <= 16) return null;
        var replyCode = BitConverter.ToUInt16(response, 8);
        if (replyCode != 2000) return null;
        return Encoding.ASCII.GetString(response, 16, response.Length - 16).Trim('\0', ' ');
    }

    private static async Task<List<RawAttendanceEvent>> FetchAttendanceLogsAsync(
        NetworkStream stream,
        ushort sessionId,
        int deviceId,
        DeviceSyncCheckpoint? checkpoint,
        string connectorId,
        CancellationToken ct)
    {
        var results = new List<RawAttendanceEvent>();
        var requestData = new byte[] { 0x01, 0x0d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        ushort replyId = 2;
        var requestPacket = BuildPacket(1503, sessionId, replyId++, requestData);
        await stream.WriteAsync(requestPacket, ct);

        var response = await ReadPacketAsync(stream, ct);
        if (response is null || response.Length < 16) return results;

        var replyCode = BitConverter.ToUInt16(response, 8);
        var payload = response.Length > 16 ? response[16..] : [];

        byte[] dataset;
        if (replyCode == 1501)
        {
            dataset = payload.Length > 4 ? payload[4..] : [];
        }
        else if (replyCode == 2000 && payload.Length >= 5 && payload.Length < 40)
        {
            var totalBytes = BitConverter.ToUInt32(payload, 1);
            if (totalBytes == 0) return results;

            var buffer = new List<byte>((int)Math.Min(totalBytes, 50_000_000));
            const uint maxChunk = 65472;
            uint offset = 0;

            while (offset < totalBytes)
            {
                var reqSize = Math.Min(maxChunk, totalBytes - offset);
                var rdyData = new byte[8];
                BitConverter.TryWriteBytes(rdyData.AsSpan(0, 4), offset);
                BitConverter.TryWriteBytes(rdyData.AsSpan(4, 4), reqSize);

                var rdyPacket = BuildPacket(1504, sessionId, replyId++, rdyData);
                await stream.WriteAsync(rdyPacket, ct);

                var chunkResp = await ReadPacketAsync(stream, ct);
                if (chunkResp is null || chunkResp.Length < 16) break;

                var chunkReply = BitConverter.ToUInt16(chunkResp, 8);
                if (chunkReply != 1501 || chunkResp.Length <= 24) break;

                var chunkSlice = chunkResp[24..];
                if (chunkSlice.Length == 0) break;

                buffer.AddRange(chunkSlice);
                offset += (uint)chunkSlice.Length;
            }

            var assembled = buffer.ToArray();
            dataset = assembled.Length > 4 ? assembled[4..] : [];
        }
        else if (replyCode == 2000 && payload.Length >= 40)
        {
            dataset = payload;
        }
        else
        {
            return results;
        }

        if (dataset.Length >= 40)
        {
            ParseLogPayload(dataset, deviceId, checkpoint, connectorId, results);
        }

        return results;
    }

    internal static void ParseLogPayload(
        byte[] payload,
        int deviceId,
        DeviceSyncCheckpoint? checkpoint,
        string connectorId,
        List<RawAttendanceEvent> results)
    {
        var now = DateTime.UtcNow;
        var offset = 0;
        while (offset + 40 <= payload.Length)
        {
            var userSpan = payload.AsSpan(offset + 2, 24);
            var zeroIdx = userSpan.IndexOf((byte)0);
            var userBytes = zeroIdx >= 0 ? userSpan[..zeroIdx] : userSpan;
            var userId = Encoding.ASCII.GetString(userBytes).Trim();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var verifyType = payload[offset + 26];
                var timeVal = BitConverter.ToUInt32(payload, offset + 27);
                var state = payload[offset + 31];
                var dtUtc = DecodeZkTime(timeVal).ToUniversalTime();

                if (checkpoint is null || !checkpoint.LastEventTimestampUtc.HasValue || dtUtc > checkpoint.LastEventTimestampUtc.Value)
                {
                    var eventId = $"{userId}-{dtUtc:yyyyMMddHHmmss}";
                    var dedupKey = DeviceCollectionService.ComputeDeduplicationKey(connectorId, deviceId, userId, dtUtc, eventId);
                    results.Add(new RawAttendanceEvent(
                        LocalId: 0,
                        ConnectorId: connectorId,
                        DeviceId: deviceId,
                        DeviceUserId: userId,
                        DeviceTimestampUtc: dtUtc,
                        DeviceEventId: eventId,
                        PunchStatus: state.ToString(),
                        SourceProtocol: "zkteco",
                        RawMetadata: $"VerifyType={verifyType};Status={state}",
                        IngestedAtUtc: now,
                        DeduplicationKey: dedupKey,
                        State: OutboxEventState.Collected));
                }
            }

            offset += 40;
        }
    }

    internal static DateTime DecodeZkTime(uint t)
    {
        var second = (int)(t % 60);
        t /= 60;
        var minute = (int)(t % 60);
        t /= 60;
        var hour = (int)(t % 24);
        t /= 24;
        var day = (int)(t % 31) + 1;
        t /= 31;
        var month = (int)(t % 12) + 1;
        t /= 12;
        var year = (int)t + 2000;

        try { return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local); }
        catch { return DateTime.UtcNow; }
    }

    internal static byte[] BuildPacket(ushort command, ushort sessionId, ushort replyId, byte[] data)
    {
        var packetLen = 8 + data.Length;
        var buf = new byte[8 + packetLen];

        buf[0] = 0x50; buf[1] = 0x50; buf[2] = 0x82; buf[3] = 0x7D;
        buf[4] = (byte)(packetLen & 0xFF);
        buf[5] = (byte)((packetLen >> 8) & 0xFF);
        buf[6] = 0x00;
        buf[7] = 0x00;

        BitConverter.TryWriteBytes(buf.AsSpan(8, 2), command);
        BitConverter.TryWriteBytes(buf.AsSpan(12, 2), sessionId);
        BitConverter.TryWriteBytes(buf.AsSpan(14, 2), replyId);

        if (data.Length > 0)
            data.CopyTo(buf.AsSpan(16));

        ushort checksum = 0;
        for (var i = 8; i < buf.Length; i += 2)
        {
            if (i == 10) continue;
            var val = i + 1 < buf.Length ? BitConverter.ToUInt16(buf, i) : buf[i];
            checksum += val;
        }
        checksum = (ushort)~checksum;
        BitConverter.TryWriteBytes(buf.AsSpan(10, 2), checksum);

        return buf;
    }

    internal static async Task<byte[]?> ReadPacketAsync(NetworkStream stream, CancellationToken ct)
    {
        var header = new byte[8];
        var read = 0;
        while (read < 8)
        {
            var r = await stream.ReadAsync(header.AsMemory(read, 8 - read), ct);
            if (r == 0) return null;
            read += r;
        }

        if (header[0] != 0x50 || header[1] != 0x50) return null;

        var length = BitConverter.ToUInt16(header, 4);
        var body = new byte[length];
        read = 0;
        while (read < length)
        {
            var r = await stream.ReadAsync(body.AsMemory(read, length - read), ct);
            if (r == 0) return null;
            read += r;
        }

        return [.. header, .. body];
    }
}
