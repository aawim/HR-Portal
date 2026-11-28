using System.Collections.Concurrent;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Devices.Simulator;

public sealed record SimulatedDeviceState(
    int DeviceId,
    bool IsOffline = false,
    bool ThrowProtocolError = false,
    TimeSpan ClockDrift = default);

public sealed record SimulatedPunch(
    string DeviceUserId,
    DateTime TimestampUtc,
    string? EventId = null,
    string? PunchStatus = null,
    string? RawMetadata = null);

/// <summary>
/// Development-only virtual biometric device simulator.
/// Guarded by ConnectorOptions.EnableDevelopmentSimulator so synthetic events never enter production.
/// </summary>
public sealed class VirtualBiometricDeviceSimulator(IOptions<ConnectorOptions> options) : IBiometricDeviceAdapter
{
    public string ProtocolCode => "simulator";

    private readonly ConcurrentDictionary<int, SimulatedDeviceState> _states = new();
    private readonly ConcurrentDictionary<int, List<SimulatedPunch>> _deviceLogs = new();
    private readonly object _lock = new();

    public void ConfigureDeviceState(int deviceId, bool isOffline = false, bool throwProtocolError = false, TimeSpan clockDrift = default)
    {
        _states[deviceId] = new SimulatedDeviceState(deviceId, isOffline, throwProtocolError, clockDrift);
    }

    public void AddPunches(int deviceId, IEnumerable<SimulatedPunch> punches)
    {
        lock (_lock)
        {
            var list = _deviceLogs.GetOrAdd(deviceId, _ => []);
            list.AddRange(punches);
        }
    }

    public void ClearDevice(int deviceId)
    {
        lock (_lock)
        {
            _deviceLogs.TryRemove(deviceId, out _);
        }
    }

    /// <summary>
    /// Seeds realistic Maldives government attendance scenarios:
    /// official hours, shift duty, overnight shift, overlapping official + duty, missing checkout,
    /// duplicate punches, and 5 daily Imam prayer-duty sessions.
    /// </summary>
    public void SeedEnterpriseScenarios(int deviceId, DateTime baseDateUtc)
    {
        var day = baseDateUtc.Date;
        var punches = new List<SimulatedPunch>
        {
            // 1. Official hours (08:00 - 14:00 MVT = 03:00 - 09:00 UTC)
            new("EMP-1001", day.AddHours(3).AddMinutes(2), $"SIM-OFF-IN-{day:yyyyMMdd}", "0", "Scenario=OfficialHours"),
            new("EMP-1001", day.AddHours(9).AddMinutes(5), $"SIM-OFF-OUT-{day:yyyyMMdd}", "1", "Scenario=OfficialHours"),

            // 2. Duplicate punch (exact same event ID repeated)
            new("EMP-1001", day.AddHours(3).AddMinutes(2), $"SIM-OFF-IN-{day:yyyyMMdd}", "0", "Scenario=Duplicate"),

            // 3. Missing checkout
            new("EMP-1002", day.AddHours(3).AddMinutes(10), $"SIM-MISS-IN-{day:yyyyMMdd}", "0", "Scenario=MissingCheckout"),

            // 4. Overnight shift (22:00 MVT to 06:00 next day = 17:00 UTC to 01:00+1 UTC)
            new("EMP-1003", day.AddHours(17), $"SIM-OVN-IN-{day:yyyyMMdd}", "0", "Scenario=OvernightShift"),
            new("EMP-1003", day.AddDays(1).AddHours(1), $"SIM-OVN-OUT-{day:yyyyMMdd}", "1", "Scenario=OvernightShift"),

            // 5. Overlapping official + extra duty segments
            new("EMP-1004", day.AddHours(3), $"SIM-OVL-1-{day:yyyyMMdd}", "0", "Scenario=OverlappingOfficial"),
            new("EMP-1004", day.AddHours(8).AddMinutes(45), $"SIM-OVL-2-{day:yyyyMMdd}", "0", "Scenario=OverlappingDutyStart"),
            new("EMP-1004", day.AddHours(9), $"SIM-OVL-3-{day:yyyyMMdd}", "1", "Scenario=OverlappingOfficialEnd"),
            new("EMP-1004", day.AddHours(12), $"SIM-OVL-4-{day:yyyyMMdd}", "1", "Scenario=OverlappingDutyEnd"),

            // 6. Five daily Imam prayer-duty sessions (Fajr, Dhuhr, Asr, Maghrib, Isha in UTC approx MVT-5)
            new("IMAM-2001", day.AddHours(0).AddMinutes(0),  $"SIM-FAJR-IN-{day:yyyyMMdd}", "0", "Prayer=Fajr"),
            new("IMAM-2001", day.AddHours(0).AddMinutes(35), $"SIM-FAJR-OUT-{day:yyyyMMdd}", "1", "Prayer=Fajr"),
            new("IMAM-2001", day.AddHours(7).AddMinutes(10), $"SIM-DHUHR-IN-{day:yyyyMMdd}", "0", "Prayer=Dhuhr"),
            new("IMAM-2001", day.AddHours(7).AddMinutes(45), $"SIM-DHUHR-OUT-{day:yyyyMMdd}", "1", "Prayer=Dhuhr"),
            new("IMAM-2001", day.AddHours(10).AddMinutes(25), $"SIM-ASR-IN-{day:yyyyMMdd}", "0", "Prayer=Asr"),
            new("IMAM-2001", day.AddHours(10).AddMinutes(55), $"SIM-ASR-OUT-{day:yyyyMMdd}", "1", "Prayer=Asr"),
            new("IMAM-2001", day.AddHours(13).AddMinutes(0),  $"SIM-MAGHRIB-IN-{day:yyyyMMdd}", "0", "Prayer=Maghrib"),
            new("IMAM-2001", day.AddHours(13).AddMinutes(30), $"SIM-MAGHRIB-OUT-{day:yyyyMMdd}", "1", "Prayer=Maghrib"),
            new("IMAM-2001", day.AddHours(14).AddMinutes(20), $"SIM-ISHA-IN-{day:yyyyMMdd}", "0", "Prayer=Isha"),
            new("IMAM-2001", day.AddHours(14).AddMinutes(55), $"SIM-ISHA-OUT-{day:yyyyMMdd}", "1", "Prayer=Isha")
        };

        AddPunches(deviceId, punches);
    }

    public void SeedLargeBatch(int deviceId, int eventCount, DateTime startUtc)
    {
        var punches = Enumerable.Range(0, eventCount)
            .Select(i => new SimulatedPunch(
                DeviceUserId: $"BATCH-{(i % 50) + 1:D4}",
                TimestampUtc: startUtc.AddSeconds(i * 3),
                EventId: $"SIM-BATCH-{deviceId}-{i:D6}",
                PunchStatus: (i % 2).ToString(),
                RawMetadata: "Scenario=LargeBatch"))
            .ToList();

        AddPunches(deviceId, punches);
    }

    public Task<DeviceConnectionResult> TestConnectionAsync(DeviceConfiguration device, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var state = _states.GetValueOrDefault(device.DeviceId) ?? new SimulatedDeviceState(device.DeviceId);
        if (state.IsOffline)
            return Task.FromResult(new DeviceConnectionResult(false, 50, "Simulated device is offline."));
        if (state.ThrowProtocolError)
            return Task.FromResult(new DeviceConnectionResult(false, 12, "Simulated protocol framing error."));

        return Task.FromResult(new DeviceConnectionResult(true, 4, null, "SIM-FW-1.0", $"SIM-SN-{device.DeviceId:D4}"));
    }

    public Task<DeviceInformation> GetDeviceInformationAsync(DeviceConfiguration device, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var state = _states.GetValueOrDefault(device.DeviceId) ?? new SimulatedDeviceState(device.DeviceId);
        if (state.IsOffline)
            throw new TimeoutException("Simulated device is offline.");

        int count;
        lock (_lock)
        {
            count = _deviceLogs.TryGetValue(device.DeviceId, out var list) ? list.Count : 0;
        }

        return Task.FromResult(new DeviceInformation(
            Vendor: "DHR Virtual Simulator",
            Model: "VirtualBiometric-DevOnly",
            SerialNumber: $"SIM-SN-{device.DeviceId:D4}",
            FirmwareVersion: "SIM-FW-1.0",
            DeviceClockUtc: DateTime.UtcNow + state.ClockDrift,
            StoredLogCount: count));
    }

    public Task<IReadOnlyList<RawAttendanceEvent>> ReadAttendanceLogsAsync(
        DeviceConfiguration device,
        DeviceSyncCheckpoint? checkpoint,
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var state = _states.GetValueOrDefault(device.DeviceId) ?? new SimulatedDeviceState(device.DeviceId);
        if (state.IsOffline)
            throw new TimeoutException($"Simulated device #{device.DeviceId} is offline.");
        if (state.ThrowProtocolError)
            throw new InvalidDataException($"Simulated protocol error on device #{device.DeviceId}.");

        List<SimulatedPunch> snapshot;
        lock (_lock)
        {
            snapshot = _deviceLogs.TryGetValue(device.DeviceId, out var list) ? [.. list] : [];
        }

        var now = DateTime.UtcNow;
        var results = new List<RawAttendanceEvent>();
        foreach (var punch in snapshot)
        {
            var effectiveTs = (punch.TimestampUtc.ToUniversalTime() + state.ClockDrift);
            if (checkpoint?.LastEventTimestampUtc is not null && effectiveTs <= checkpoint.LastEventTimestampUtc.Value)
                continue;

            var eventId = string.IsNullOrWhiteSpace(punch.EventId)
                ? $"{punch.DeviceUserId}-{effectiveTs:yyyyMMddHHmmss}"
                : punch.EventId.Trim();

            var dedup = DeviceCollectionService.ComputeDeduplicationKey(connectorId, device.DeviceId, punch.DeviceUserId, effectiveTs, eventId);
            results.Add(new RawAttendanceEvent(
                0, connectorId, device.DeviceId, punch.DeviceUserId, effectiveTs, eventId, punch.PunchStatus,
                ProtocolCode, punch.RawMetadata, now, dedup, OutboxEventState.Collected));
        }

        return Task.FromResult<IReadOnlyList<RawAttendanceEvent>>(results);
    }

    private void EnsureEnabled()
    {
        if (!options.Value.EnableDevelopmentSimulator)
            throw new InvalidOperationException("VirtualBiometricDeviceSimulator is disabled outside development/test environments.");
    }
}
