using DHR.AttendanceConnector.Core.Interfaces;

namespace DHR.AttendanceConnector.Devices.Abstractions;

public sealed class DeviceAdapterRegistry : IDeviceAdapterRegistry
{
    private readonly Dictionary<string, IBiometricDeviceAdapter> _adapters;

    public DeviceAdapterRegistry(IEnumerable<IBiometricDeviceAdapter> adapters)
    {
        _adapters = new Dictionary<string, IBiometricDeviceAdapter>(StringComparer.OrdinalIgnoreCase);
        foreach (var adapter in adapters)
        {
            _adapters[adapter.ProtocolCode] = adapter;
        }
    }

    public IBiometricDeviceAdapter? Resolve(string protocolCode) =>
        string.IsNullOrWhiteSpace(protocolCode)
            ? null
            : _adapters.TryGetValue(protocolCode.Trim(), out var adapter) ? adapter : null;

    public IReadOnlyList<string> SupportedProtocols => _adapters.Keys.OrderBy(k => k).ToList();
}
