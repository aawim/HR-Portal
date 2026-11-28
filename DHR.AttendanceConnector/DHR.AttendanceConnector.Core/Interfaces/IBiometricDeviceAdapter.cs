using DHR.AttendanceConnector.Core.Models;

namespace DHR.AttendanceConnector.Core.Interfaces;

public interface IBiometricDeviceAdapter
{
    string ProtocolCode { get; }

    Task<DeviceConnectionResult> TestConnectionAsync(
        DeviceConfiguration device,
        CancellationToken cancellationToken = default);

    Task<DeviceInformation> GetDeviceInformationAsync(
        DeviceConfiguration device,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RawAttendanceEvent>> ReadAttendanceLogsAsync(
        DeviceConfiguration device,
        DeviceSyncCheckpoint? checkpoint,
        string connectorId,
        CancellationToken cancellationToken = default);
}

public interface IDeviceAdapterRegistry
{
    IBiometricDeviceAdapter? Resolve(string protocolCode);
    IReadOnlyList<string> SupportedProtocols { get; }
}
