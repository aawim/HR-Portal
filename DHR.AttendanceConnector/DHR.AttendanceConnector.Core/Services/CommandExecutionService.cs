using System.Text.Json;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using Microsoft.Extensions.Logging;

namespace DHR.AttendanceConnector.Core.Services;

public sealed class CommandExecutionService(
    IConnectorRepository repository,
    IDhrApiClient apiClient,
    ConnectorActivationService activationService,
    DeviceCollectionService collectionService,
    AttendanceSyncService syncService,
    DiagnosticsExportService diagnosticsService,
    ILogger<CommandExecutionService> logger)
{
    public static readonly HashSet<string> SupportedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "SyncNow",
        "TestDevice",
        "RefreshConfig",
        "ExportDiagnostics"
    };

    public async Task<int> PollAndExecuteAsync(CancellationToken ct = default)
    {
        var profile = await repository.GetProfileAsync(ct);
        if (profile is null) return 0;

        var commands = await apiClient.PollCommandsAsync(profile.ApiBaseUrl, profile.AgentIdentifier, ct);
        var executed = 0;

        foreach (var cmd in commands)
        {
            await repository.SaveReceivedCommandAsync(
                new AgentCommandRecord(cmd.CommandId, cmd.CommandType, cmd.PayloadJson, "Received", DateTime.UtcNow),
                ct);

            var (succeeded, resultJson, error) = await ExecuteSingleCommandAsync(cmd, ct);

            await repository.UpdateCommandResultAsync(
                cmd.CommandId,
                succeeded ? "Completed" : "Failed",
                resultJson,
                error,
                ct);

            try
            {
                await apiClient.ReportCommandResultAsync(
                    profile.ApiBaseUrl,
                    cmd.CommandId,
                    profile.AgentIdentifier,
                    succeeded,
                    resultJson,
                    error,
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to report result for command #{CommandId}", cmd.CommandId);
            }

            executed++;
        }

        return executed;
    }

    public async Task<(bool Succeeded, string? ResultJson, string? ErrorMessage)> ExecuteSingleCommandAsync(
        RemoteCommandItem cmd,
        CancellationToken ct = default)
    {
        if (!SupportedCommands.Contains(cmd.CommandType))
        {
            var err = $"Rejected unsupported or unauthorised command type '{cmd.CommandType}'.";
            await repository.WriteHealthLogAsync("Warning", "Commands", err, ct: ct);
            return (false, null, err);
        }

        try
        {
            switch (cmd.CommandType.Trim().ToLowerInvariant())
            {
                case "syncnow":
                {
                    var collected = await collectionService.PollAllDevicesAsync(ct);
                    var sync = await syncService.DrainPendingAsync(ct);
                    return (sync.Succeeded, JsonSerializer.Serialize(new { collected, sync.BatchesSent, sync.Accepted, sync.Duplicate, sync.Rejected }), sync.ErrorMessage);
                }
                case "refreshconfig":
                {
                    var updated = await activationService.RefreshConfigurationAsync(ct);
                    return (updated is not null, JsonSerializer.Serialize(new { configVersion = updated?.ConfigurationVersion }), updated is null ? "Connector not activated." : null);
                }
                case "testdevice":
                {
                    int? deviceId = null;
                    if (!string.IsNullOrWhiteSpace(cmd.PayloadJson))
                    {
                        using var doc = JsonDocument.Parse(cmd.PayloadJson);
                        if (doc.RootElement.TryGetProperty("deviceId", out var el) && el.TryGetInt32(out var parsed))
                            deviceId = parsed;
                    }

                    if (!deviceId.HasValue)
                        return (false, null, "TestDevice requires a registered 'deviceId' in PayloadJson.");

                    var testResult = await collectionService.TestDeviceAsync(deviceId.Value, ct);
                    return (testResult.IsReachable, JsonSerializer.Serialize(testResult), testResult.ErrorMessage);
                }
                case "exportdiagnostics":
                {
                    var summary = await diagnosticsService.BuildSanitisedDiagnosticSummaryAsync(ct);
                    return (true, summary, null);
                }
                default:
                    return (false, null, $"Unsupported command '{cmd.CommandType}'.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Command #{CommandId} ({CommandType}) failed.", cmd.CommandId, cmd.CommandType);
            return (false, null, ex.Message);
        }
    }
}
