using System.Net.Http.Json;
using HRM.AttendanceEdgeAgent.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.AttendanceEdgeAgent.Ingestion;

public sealed class DhrIngestionClient(HttpClient http, IOptions<AttendanceEdgeOptions> options, ILogger<DhrIngestionClient> logger)
{
    public async Task<EdgeAttendanceIngestionResult?> SendAsync(IReadOnlyList<AttendanceEnvelope> events, CancellationToken ct)
    {
        if (events == null || events.Count == 0) return null;

        var endpoint = new Uri(options.Value.ApiBaseUrl, "api/v1/edge-attendance/events");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new AttendanceBatch(options.Value.AgentId, events))
        };

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("DHR event ingestion failed with status {Status}: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<EdgeAttendanceIngestionResult>(cancellationToken: ct);
    }

    public async Task HeartbeatAsync(string healthStatus, CancellationToken ct)
    {
        var endpoint = new Uri(options.Value.ApiBaseUrl, "api/v1/edge-attendance/heartbeat");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new AttendanceHeartbeat(options.Value.AgentId, healthStatus))
        };

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("DHR heartbeat failed with status {Status}: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await HeartbeatAsync("Testing", ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DHR connection test failed.");
            return false;
        }
    }
}

public sealed record AttendanceEnvelope(
    int DeviceId,
    string LocalUserId,
    DateTime DeviceTimestamp,
    string DeviceEventId,
    string? RawMetadata = null);

public sealed record AttendanceBatch(
    string AgentId,
    IReadOnlyList<AttendanceEnvelope> Events);

public sealed record AttendanceHeartbeat(
    string AgentId,
    string HealthStatus);

public sealed record EdgeAttendanceIngestionResult(
    int Accepted,
    int Duplicate,
    int Rejected);
