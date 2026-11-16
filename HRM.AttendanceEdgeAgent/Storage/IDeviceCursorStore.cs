namespace HRM.AttendanceEdgeAgent.Storage;

public interface IDeviceCursorStore
{
    Task<DeviceCursor?> GetCursorAsync(int deviceId, CancellationToken ct = default);
    Task SaveCursorAsync(int deviceId, DeviceCursor cursor, CancellationToken ct = default);
}
