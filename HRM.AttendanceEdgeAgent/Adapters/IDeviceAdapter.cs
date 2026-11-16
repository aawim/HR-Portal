using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Storage;

namespace HRM.AttendanceEdgeAgent.Adapters;

public interface IDeviceAdapter
{
    string Name { get; }
    Task<IReadOnlyList<DeviceAttendanceEvent>> ReadAsync(DeviceOptions device, DeviceCursor? cursor, CancellationToken ct);
}

public sealed record DeviceAttendanceEvent(
    int DeviceId,
    string LocalUserId,
    DateTime DeviceTimestamp,
    string DeviceEventId,
    string? RawMetadata = null);
