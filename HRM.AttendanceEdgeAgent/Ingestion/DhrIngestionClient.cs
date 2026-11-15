using System.Net.Http.Json;
using HRM.AttendanceEdgeAgent.Configuration;
using Microsoft.Extensions.Options;
namespace HRM.AttendanceEdgeAgent.Ingestion;
public sealed class DhrIngestionClient(HttpClient http,IOptions<AttendanceEdgeOptions> options)
{
    // The API must authenticate this call using an enrolled agent certificate/client credential;
    // The agent never sends a user credential; the server derives organisation/device ownership from its mTLS identity.
    public async Task SendAsync(IReadOnlyList<AttendanceEnvelope> events,CancellationToken ct)
    { if(events.Count==0)return; using var request=new HttpRequestMessage(HttpMethod.Post,new Uri(options.Value.ApiBaseUrl,"api/v1/edge-attendance/events")){Content=JsonContent.Create(new AttendanceBatch(options.Value.AgentId,events))};var response=await http.SendAsync(request,ct);response.EnsureSuccessStatusCode(); }
    public async Task HeartbeatAsync(string healthStatus, CancellationToken ct)
    { using var request=new HttpRequestMessage(HttpMethod.Post,new Uri(options.Value.ApiBaseUrl,"api/v1/edge-attendance/heartbeat")){Content=JsonContent.Create(new AttendanceHeartbeat(options.Value.AgentId,healthStatus))};var response=await http.SendAsync(request,ct);response.EnsureSuccessStatusCode(); }
}
public sealed record AttendanceEnvelope(int DeviceId,string LocalUserId,DateTime DeviceTimestamp,string DeviceEventId,string? RawMetadata);
public sealed record AttendanceBatch(string AgentId,IReadOnlyList<AttendanceEnvelope> Events);
public sealed record AttendanceHeartbeat(string AgentId,string HealthStatus);
