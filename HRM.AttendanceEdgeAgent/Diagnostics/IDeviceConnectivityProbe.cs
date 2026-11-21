using HRM.AttendanceEdgeAgent.Configuration;

namespace HRM.AttendanceEdgeAgent.Diagnostics;

public interface IDeviceConnectivityProbe
{
    Task<DeviceConnectivityStatus> ProbeAsync(DeviceOptions device, CancellationToken ct = default);
}
