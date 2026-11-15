using HRM.AttendanceEdgeAgent.Adapters;
using HRM.AttendanceEdgeAgent.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace HRM.AttendanceEdgeAgent.Ingestion;
public sealed class AttendancePollingWorker(IEnumerable<IDeviceAdapter> adapters,IOptions<AttendanceEdgeOptions> options,DhrIngestionClient ingestion,ILogger<AttendancePollingWorker> log) : BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 { var config=options.Value;if(string.IsNullOrWhiteSpace(config.AgentId)||string.IsNullOrWhiteSpace(config.OrganisationId))throw new InvalidOperationException("AgentId and OrganisationId must be configured."); while(!stoppingToken.IsCancellationRequested){try{var events=new List<AttendanceEnvelope>();foreach(var device in config.Devices){var adapter=adapters.SingleOrDefault(x=>x.Name.Equals(device.Adapter,StringComparison.OrdinalIgnoreCase))??throw new InvalidOperationException($"No adapter is registered for {device.Adapter}.");events.AddRange((await adapter.ReadAsync(device,stoppingToken)).Select(x=>new AttendanceEnvelope(x.DeviceId,x.LocalUserId,x.DeviceTimestamp,x.DeviceEventId,x.RawMetadata)));}await ingestion.SendAsync(events,stoppingToken);await ingestion.HeartbeatAsync("Online",stoppingToken);}catch(Exception ex) when(!stoppingToken.IsCancellationRequested){log.LogError(ex,"Attendance poll failed; events will be retried by the device adapter cursor.");try{await ingestion.HeartbeatAsync("Degraded",stoppingToken);}catch(Exception heartbeatException) when(!stoppingToken.IsCancellationRequested){log.LogWarning(heartbeatException,"Unable to report degraded agent health.");}}await Task.Delay(TimeSpan.FromSeconds(Math.Max(5,config.PollSeconds)),stoppingToken);}}
}
