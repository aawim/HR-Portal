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

    public IBiometricDeviceAdapter? Resolve(string protocolCode)
    {
        if (string.IsNullOrWhiteSpace(protocolCode))
            return null;

        var trimmed = protocolCode.Trim();
        if (_adapters.TryGetValue(trimmed, out var exact))
            return exact;

        // Support parameterized protocol strings like "zkteco:1" or "zkteco?key=1234"
        var sepIdx = trimmed.IndexOfAny([':', '?']);
        if (sepIdx > 0)
        {
            var baseProto = trimmed[..sepIdx].Trim();
            if (_adapters.TryGetValue(baseProto, out var matched))
                return matched;
        }

        return null;
    }

    public IReadOnlyList<string> SupportedProtocols => _adapters.Keys.OrderBy(k => k).ToList();
}
