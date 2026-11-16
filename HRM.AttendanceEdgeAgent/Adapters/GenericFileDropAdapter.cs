using System.Globalization;
using System.Text.Json;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Storage;
using Microsoft.Extensions.Logging;

namespace HRM.AttendanceEdgeAgent.Adapters;

/// <summary>
/// Monitors a local or SMB network folder for exported attendance log files (.csv, .tsv, .json).
/// Commonly used with legacy biometric software (ZKTime, BioStar, etc.) or file-export terminals.
/// </summary>
public sealed class GenericFileDropAdapter : IDeviceAdapter
{
    public string Name => "generic-file";
    private readonly ILogger<GenericFileDropAdapter> _logger;

    public GenericFileDropAdapter(ILogger<GenericFileDropAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeviceAttendanceEvent>> ReadAsync(DeviceOptions device, DeviceCursor? cursor, CancellationToken ct)
    {
        var folder = !string.IsNullOrWhiteSpace(device.DropPath) ? device.DropPath : device.Host;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return [];
        }

        var processedFolder = Path.Combine(folder, ".processed");
        Directory.CreateDirectory(processedFolder);

        var events = new List<DeviceAttendanceEvent>();
        var files = Directory.GetFiles(folder, "*.*")
            .Where(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(File.GetCreationTimeUtc)
            .ToList();

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var fileEvents = file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    ? await ParseJsonFileAsync(device.DeviceId, file, ct)
                    : await ParseCsvFileAsync(device.DeviceId, file, ct);

                events.AddRange(fileEvents);

                // Move file to .processed directory
                var destFile = Path.Combine(processedFolder, $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(file)}");
                File.Move(file, destFile, overwrite: true);
                _logger.LogInformation("Processed and archived drop file {File} with {Count} events.", Path.GetFileName(file), fileEvents.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse attendance drop file {File}.", file);
            }
        }

        return events;
    }

    private static async Task<List<DeviceAttendanceEvent>> ParseCsvFileAsync(int deviceId, string filePath, CancellationToken ct)
    {
        var results = new List<DeviceAttendanceEvent>();
        var separator = filePath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
        var lines = await File.ReadAllLinesAsync(filePath, ct);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split(separator).Select(p => p.Trim(' ', '"')).ToArray();
            if (parts.Length < 2) continue;

            // Skip header row if first row has non-digit/date text
            if (i == 0 && (parts[0].Equals("user", StringComparison.OrdinalIgnoreCase) ||
                           parts[0].Equals("userid", StringComparison.OrdinalIgnoreCase) ||
                           parts[0].Equals("enrollmentnumber", StringComparison.OrdinalIgnoreCase) ||
                           parts[0].Equals("id", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var localUserId = parts[0];
            if (!DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp))
            {
                if (!DateTime.TryParse(parts[1], out timestamp))
                    continue;
            }

            var eventId = parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2])
                ? parts[2]
                : $"{localUserId}-{timestamp:yyyyMMddHHmmss}";

            var metadata = parts.Length >= 4 ? parts[3] : null;

            results.Add(new DeviceAttendanceEvent(
                DeviceId: deviceId,
                LocalUserId: localUserId,
                DeviceTimestamp: timestamp,
                DeviceEventId: eventId,
                RawMetadata: metadata));
        }

        return results;
    }

    private static async Task<List<DeviceAttendanceEvent>> ParseJsonFileAsync(int deviceId, string filePath, CancellationToken ct)
    {
        var results = new List<DeviceAttendanceEvent>();
        await using var stream = File.OpenRead(filePath);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (doc.RootElement.ValueKind != JsonValueKind.Array) return results;

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var userId = el.TryGetProperty("localUserId", out var u) ? u.GetString() :
                         el.TryGetProperty("userId", out var u2) ? u2.GetString() : null;

            if (string.IsNullOrWhiteSpace(userId)) continue;

            var timeStr = el.TryGetProperty("deviceTimestamp", out var t) ? t.GetString() :
                          el.TryGetProperty("timestamp", out var t2) ? t2.GetString() : null;

            if (string.IsNullOrWhiteSpace(timeStr) || !DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp))
                continue;

            var eventId = el.TryGetProperty("deviceEventId", out var e) ? e.GetString() :
                          el.TryGetProperty("eventId", out var e2) ? e2.GetString() :
                          $"{userId}-{timestamp:yyyyMMddHHmmss}";

            var metadata = el.TryGetProperty("rawMetadata", out var m) ? m.GetString() : null;

            results.Add(new DeviceAttendanceEvent(
                DeviceId: deviceId,
                LocalUserId: userId,
                DeviceTimestamp: timestamp,
                DeviceEventId: eventId ?? $"{userId}-{timestamp:yyyyMMddHHmmss}",
                RawMetadata: metadata));
        }

        return results;
    }
}
