using System.IO.Compression;
using System.Text.Json;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Core.Services;

public sealed class DiagnosticsExportService(
    IConnectorRepository repository,
    IOptions<ConnectorOptions> options)
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    /// <summary>
    /// Builds a sanitised JSON diagnostic summary (never includes private keys, certificates, or activation codes).
    /// </summary>
    public async Task<string> BuildSanitisedDiagnosticSummaryAsync(CancellationToken ct = default)
    {
        var profile = await repository.GetProfileAsync(ct);
        var devices = await repository.GetDevicesAsync(ct);
        var metrics = await repository.GetSyncMetricsAsync(ct);
        var logs = await repository.GetHealthLogsAsync(100, ct);
        var configs = await repository.GetConfigurationHistoryAsync(10, ct);
        var dbHealthy = await repository.CheckHealthAsync(ct);

        var summary = new
        {
            ExportedAtUtc = DateTime.UtcNow,
            ConnectorVersion = options.Value.ConnectorVersion,
            OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            LocalStorageHealthy = dbHealthy,
            Profile = profile is null ? null : new
            {
                profile.ConnectorId,
                profile.AgentIdentifier,
                profile.OrganisationId,
                profile.OrganisationName,
                profile.SiteName,
                profile.ApiBaseUrl,
                CertificateThumbprintPrefix = profile.CertificateThumbprint.Length > 8 ? profile.CertificateThumbprint[..8] + "..." : "***",
                profile.CertificateNotAfterUtc,
                profile.ConfigurationVersion,
                profile.LastConfigSyncAtUtc
            },
            SyncMetrics = metrics,
            Devices = devices.Select(d => new
            {
                d.DeviceId,
                d.Name,
                d.ProtocolCode,
                d.Port,
                d.PollingIntervalSeconds,
                d.Enabled,
                d.LastConnectionStatus,
                d.LastSuccessfulPollUtc,
                d.LastEventTimestampUtc,
                d.LastError
            }),
            ConfigurationHistory = configs,
            RecentHealthLogs = logs
        };

        return JsonSerializer.Serialize(summary, PrettyJson);
    }

    public async Task<string> ExportSanitisedZipAsync(string? outputDirectory = null, CancellationToken ct = default)
    {
        var dir = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.Combine(options.Value.ResolveDataDirectory(), "diagnostics")
            : outputDirectory;
        Directory.CreateDirectory(dir);

        var zipPath = Path.Combine(dir, $"dhr-connector-diagnostics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
        var summaryJson = await BuildSanitisedDiagnosticSummaryAsync(ct);

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("diagnostics-summary.json");
        await using var writer = new StreamWriter(entry.Open());
        await writer.WriteAsync(summaryJson);

        await repository.WriteHealthLogAsync("Info", "Diagnostics", $"Exported sanitised diagnostic package to {Path.GetFileName(zipPath)}.", ct: ct);
        return zipPath;
    }
}
