using HRM.AttendanceEdgeAgent.Configuration;
namespace HRM.AttendanceEdgeAgent.Adapters;
public interface IDeviceAdapter { string Name { get; } Task<IReadOnlyList<DeviceAttendanceEvent>> ReadAsync(DeviceOptions device, CancellationToken ct); }
public sealed record DeviceAttendanceEvent(int DeviceId,string LocalUserId,DateTime DeviceTimestamp,string DeviceEventId,string? RawMetadata);
