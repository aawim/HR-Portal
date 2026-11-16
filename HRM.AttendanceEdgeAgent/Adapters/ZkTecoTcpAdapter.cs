using System.Net.Sockets;
using System.Text;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Storage;
using Microsoft.Extensions.Logging;

namespace HRM.AttendanceEdgeAgent.Adapters;

/// <summary>
/// Communicates directly with ZKTeco and compatible biometric terminals over TCP (port 4370).
/// Uses the ZKTeco standalone binary protocol framing to fetch attendance logs.
/// </summary>
public sealed class ZkTecoTcpAdapter : IDeviceAdapter
{
    public string Name => "zkteco";
    private readonly ILogger<ZkTecoTcpAdapter> _logger;

    public ZkTecoTcpAdapter(ILogger<ZkTecoTcpAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeviceAttendanceEvent>> ReadAsync(DeviceOptions device, DeviceCursor? cursor, CancellationToken ct)
    {
        var port = device.Port > 0 ? device.Port : 4370;
        if (string.IsNullOrWhiteSpace(device.Host))
        {
            _logger.LogWarning("ZKTeco adapter requires a valid host/IP for device {DeviceId}.", device.DeviceId);
            return [];
        }

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            await client.ConnectAsync(device.Host, port, timeoutCts.Token);
            await using var stream = client.GetStream();

            // Connect packet (CMD_CONNECT = 1000)
            var sessionId = await ConnectSessionAsync(stream, timeoutCts.Token);
            if (sessionId == null)
            {
                _logger.LogWarning("ZKTeco terminal at {Host}:{Port} refused connection handshake.", device.Host, port);
                return [];
            }

            try
            {
                // Read attendance log records
                var events = await FetchAttendanceLogsAsync(stream, sessionId.Value, device.DeviceId, cursor, timeoutCts.Token);
                return events;
            }
            finally
            {
                // Disconnect packet (CMD_EXIT = 1001)
                try { await DisconnectSessionAsync(stream, sessionId.Value, CancellationToken.None); }
                catch { /* Ignore clean exit failures */ }
            }
        }
        catch (Exception ex) when (ex is SocketException or TimeoutException or OperationCanceledException)
        {
            _logger.LogWarning("ZKTeco terminal at {Host}:{Port} is unreachable or timed out: {Message}", device.Host, port, ex.Message);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reading ZKTeco terminal at {Host}:{Port}.", device.Host, port);
            return [];
        }
    }

    private static async Task<ushort?> ConnectSessionAsync(NetworkStream stream, CancellationToken ct)
    {
        // ZK TCP packet: [0x50, 0x50, 0x82, 0x7D] + length (2 bytes) + 0x00, 0x00 + payload
        // Connect command: code 1000, checksum, session 0, reply_id 0
        var connectPacket = BuildPacket(1000, 0, 0, []);
        await stream.WriteAsync(connectPacket, ct);

        var response = await ReadPacketAsync(stream, ct);
        if (response == null || response.Length < 16) return null;

        var replyCode = BitConverter.ToUInt16(response, 8);
        var sessionId = BitConverter.ToUInt16(response, 12);

        // 2000 = CMD_ACK_OK
        return replyCode == 2000 ? sessionId : null;
    }

    private static async Task DisconnectSessionAsync(NetworkStream stream, ushort sessionId, CancellationToken ct)
    {
        var exitPacket = BuildPacket(1001, sessionId, 1, []);
        await stream.WriteAsync(exitPacket, ct);
    }

    private static async Task<List<DeviceAttendanceEvent>> FetchAttendanceLogsAsync(
        NetworkStream stream, ushort sessionId, int deviceId, DeviceCursor? cursor, CancellationToken ct)
    {
        var results = new List<DeviceAttendanceEvent>();

        // CMD_ATTLOG_RRQ = 9
        var requestPacket = BuildPacket(9, sessionId, 2, []);
        await stream.WriteAsync(requestPacket, ct);

        var response = await ReadPacketAsync(stream, ct);
        if (response == null || response.Length < 16) return results;

        var replyCode = BitConverter.ToUInt16(response, 8);
        if (replyCode != 2000) return results; // No data or busy

        // If data follows in subsequent packets or payload, parse ZK log format:
        // Standard ZK 40-byte record:
        // [User PIN: 24 bytes] + [Verify Type: 1 byte] + [Timestamp: 4 bytes encoded] + [Status: 1 byte]
        var payload = response.Length > 16 ? response[16..] : [];
        if (payload.Length >= 40)
        {
            ParseLogPayload(payload, deviceId, cursor, results);
        }

        return results;
    }

    private static void ParseLogPayload(byte[] payload, int deviceId, DeviceCursor? cursor, List<DeviceAttendanceEvent> results)
    {
        var offset = 0;
        while (offset + 40 <= payload.Length)
        {
            var userSpan = payload.AsSpan(offset, 24);
            var zeroIdx = userSpan.IndexOf((byte)0);
            var userBytes = zeroIdx >= 0 ? userSpan[..zeroIdx] : userSpan;
            var userId = Encoding.ASCII.GetString(userBytes).Trim();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var timeVal = BitConverter.ToUInt32(payload, offset + 26);
                var dt = DecodeZkTime(timeVal);

                if (cursor == null || !cursor.LastEventTimestampUtc.HasValue || dt > cursor.LastEventTimestampUtc.Value)
                {
                    var eventId = $"{userId}-{dt:yyyyMMddHHmmss}";
                    results.Add(new DeviceAttendanceEvent(
                        DeviceId: deviceId,
                        LocalUserId: userId,
                        DeviceTimestamp: dt,
                        DeviceEventId: eventId,
                        RawMetadata: $"VerifyType={payload[offset + 24]};Status={payload[offset + 30]}"));
                }
            }

            offset += 40;
        }
    }

    private static DateTime DecodeZkTime(uint t)
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
        catch { return DateTime.Now; }
    }

    private static byte[] BuildPacket(ushort command, ushort sessionId, ushort replyId, byte[] data)
    {
        // 8-byte command header: Command(2) + Checksum(2) + SessionId(2) + ReplyId(2)
        var packetLen = 8 + data.Length;
        var buf = new byte[8 + packetLen];

        // Magic bytes
        buf[0] = 0x50; buf[1] = 0x50; buf[2] = 0x82; buf[3] = 0x7D;
        buf[4] = (byte)(packetLen & 0xFF);
        buf[5] = (byte)((packetLen >> 8) & 0xFF);
        buf[6] = 0x00;
        buf[7] = 0x00;

        BitConverter.TryWriteBytes(buf.AsSpan(8, 2), command);
        BitConverter.TryWriteBytes(buf.AsSpan(12, 2), sessionId);
        BitConverter.TryWriteBytes(buf.AsSpan(14, 2), replyId);

        if (data.Length > 0)
        {
            data.CopyTo(buf.AsSpan(16));
        }

        // Calculate 16-bit 1's complement checksum
        ushort checksum = 0;
        for (var i = 8; i < buf.Length; i += 2)
        {
            if (i == 10) continue; // Skip checksum field itself
            var val = i + 1 < buf.Length ? BitConverter.ToUInt16(buf, i) : buf[i];
            checksum += val;
        }
        checksum = (ushort)~checksum;
        BitConverter.TryWriteBytes(buf.AsSpan(10, 2), checksum);

        return buf;
    }

    private static async Task<byte[]?> ReadPacketAsync(NetworkStream stream, CancellationToken ct)
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
