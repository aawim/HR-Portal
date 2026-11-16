using System.Text.Json;
using HRM.AttendanceEdgeAgent.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AttendanceEdgeAgent.Storage;

public sealed class FileDeviceCursorStore : IDeviceCursorStore
{
    private readonly string _cursorDir;
    private readonly ILogger<FileDeviceCursorStore> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public FileDeviceCursorStore(IOptions<AttendanceEdgeOptions> options, ILogger<FileDeviceCursorStore> logger)
    {
        _logger = logger;
        _cursorDir = Path.Combine(options.Value.DataDirectory, "cursors");
        Directory.CreateDirectory(_cursorDir);
    }

    public async Task<DeviceCursor?> GetCursorAsync(int deviceId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_cursorDir, $"{deviceId}.json");
        if (!File.Exists(filePath)) return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<DeviceCursor>(stream, JsonOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cursor for device {DeviceId}; starting fresh.", deviceId);
            return null;
        }
    }

    public async Task SaveCursorAsync(int deviceId, DeviceCursor cursor, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_cursorDir, $"{deviceId}.json");
        var tempPath = Path.Combine(_cursorDir, $"{deviceId}.tmp.{Guid.NewGuid():N}");

        try
        {
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, cursor, JsonOptions, ct);
            }
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cursor for device {DeviceId}.", deviceId);
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* ignore cleanup error */ }
            }
            throw;
        }
    }
}
