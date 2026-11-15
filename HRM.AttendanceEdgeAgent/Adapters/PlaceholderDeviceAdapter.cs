using HRM.AttendanceEdgeAgent.Configuration;
namespace HRM.AttendanceEdgeAgent.Adapters;
/// <summary>Safe placeholder: production adapters implement vendor network protocols, never UI automation.</summary>
public sealed class PlaceholderDeviceAdapter : IDeviceAdapter
{ public string Name => "placeholder"; public Task<IReadOnlyList<DeviceAttendanceEvent>> ReadAsync(DeviceOptions device,CancellationToken ct) => Task.FromResult<IReadOnlyList<DeviceAttendanceEvent>>([]); }
