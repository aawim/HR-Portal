namespace DHR.AttendanceConnector.Core.Configuration;

public sealed class ConnectorOptions
{
    public const string SectionName = "DhrConnector";

    /// <summary>
    /// Base URL of the central DHR API (must be HTTPS in production).
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://dhr.gov.mv/";

    /// <summary>
    /// Local data directory for SQLite database, encrypted secrets, and logs.
    /// Defaults to OS-appropriate application data directory when empty.
    /// </summary>
    public string DataDirectory { get; set; } = "";

    /// <summary>
    /// Allows HTTP localhost URLs ONLY in explicit development/testing environments.
    /// Production builds must keep this false.
    /// </summary>
    public bool AllowLoopbackHttpForDevelopment { get; set; } = false;

    /// <summary>
    /// Maximum concurrent biometric device polls.
    /// </summary>
    public int MaxConcurrentDevicePolls { get; set; } = 4;

    /// <summary>
    /// Default device socket timeout in seconds.
    /// </summary>
    public int DeviceTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Enables the development-only virtual biometric device simulator.
    /// Must remain false in production.
    /// </summary>
    public bool EnableDevelopmentSimulator { get; set; } = false;

    /// <summary>
    /// Retention period (days) for acknowledged raw events before optional local pruning.
    /// Unsynchronised events are never pruned.
    /// </summary>
    public int AcknowledgedRetentionDays { get; set; } = 30;

    /// <summary>
    /// Connector semantic version.
    /// </summary>
    public string ConnectorVersion { get; set; } = "1.0.0";

    public string ResolveDataDirectory()
    {
        if (!string.IsNullOrWhiteSpace(DataDirectory))
            return Path.GetFullPath(DataDirectory);

        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DHR", "AttendanceConnector");
        if (OperatingSystem.IsMacOS())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHR", "AttendanceConnector");
        return "/var/lib/dhr-attendance-connector";
    }
}
