namespace HRM.AttendanceEdgeAgent.Diagnostics;

public sealed record DeviceConnectivityStatus(
    int DeviceId,
    string Host,
    int Port,
    bool IsReachable,
    long LatencyMs,
    string? ErrorMessage = null);
