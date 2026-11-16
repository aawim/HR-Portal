using System.Security.Cryptography.X509Certificates;
using HRM.AttendanceEdgeAgent.Adapters;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Diagnostics;
using HRM.AttendanceEdgeAgent.Ingestion;
using HRM.AttendanceEdgeAgent.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var isProbeCommand = args.Length > 0 && args[0].Equals("probe", StringComparison.OrdinalIgnoreCase);
var isStatusCommand = args.Length > 0 && args[0].Equals("status", StringComparison.OrdinalIgnoreCase);

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "HRM_EDGE_");

builder.Services.AddOptions<AttendanceEdgeOptions>()
    .Bind(builder.Configuration.GetSection("AttendanceEdge"))
    .ValidateOnStart();

var edge = builder.Configuration.GetSection("AttendanceEdge").Get<AttendanceEdgeOptions>() ?? new();

// Core storage & diagnostics
builder.Services.AddSingleton<IDeviceCursorStore, FileDeviceCursorStore>();
builder.Services.AddSingleton<IOfflineEventBuffer, FileOfflineEventBuffer>();
builder.Services.AddSingleton<IDeviceConnectivityProbe, TcpDeviceConnectivityProbe>();

// Registered device adapters
builder.Services.AddSingleton<IDeviceAdapter, PlaceholderDeviceAdapter>();
builder.Services.AddSingleton<IDeviceAdapter, GenericFileDropAdapter>();
builder.Services.AddSingleton<IDeviceAdapter, ZkTecoTcpAdapter>();

// mTLS Certificate Setup
X509Certificate2? certificate = null;
if (!string.IsNullOrWhiteSpace(edge.ClientCertificatePath) && File.Exists(edge.ClientCertificatePath))
{
    certificate = new X509Certificate2(edge.ClientCertificatePath, edge.ClientCertificatePassword,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
    if (!certificate.HasPrivateKey)
    {
        throw new InvalidOperationException("The mTLS certificate must include a private key.");
    }
}
else if (!isProbeCommand)
{
    throw new InvalidOperationException($"A valid mTLS client certificate path is required at '{edge.ClientCertificatePath}'.");
}

if (certificate != null)
{
    builder.Services.AddHttpClient<DhrIngestionClient>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ClientCertificates = { certificate } });
}
else
{
    builder.Services.AddHttpClient<DhrIngestionClient>();
}

// Background Workers
builder.Services.AddHostedService<AttendancePollingWorker>();
builder.Services.AddHostedService<OutboxDrainWorker>();
builder.Services.AddHostedService<HeartbeatWorker>();

var app = builder.Build();

// CLI Commands
if (isProbeCommand)
{
    var probe = app.Services.GetRequiredService<IDeviceConnectivityProbe>();
    Console.WriteLine("==================================================");
    Console.WriteLine($"Attendance Edge Agent — LAN Device Connectivity Probe");
    Console.WriteLine($"Agent ID: {edge.AgentId} | Organisation ID: {edge.OrganisationId}");
    Console.WriteLine("==================================================");

    if (edge.Devices.Count == 0)
    {
        Console.WriteLine("No devices configured in AttendanceEdge:Devices.");
        return;
    }

    foreach (var device in edge.Devices)
    {
        Console.Write($"Device #{device.DeviceId} ({device.Adapter} @ {device.Host}:{device.Port})... ");
        var res = await probe.ProbeAsync(device);
        if (res.IsReachable)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[ONLINE] {res.LatencyMs}ms");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[OFFLINE] {res.ErrorMessage}");
            Console.ResetColor();
        }
    }
    return;
}

if (isStatusCommand)
{
    var outbox = app.Services.GetRequiredService<IOfflineEventBuffer>();
    var stats = await outbox.GetStatisticsAsync();

    Console.WriteLine("==================================================");
    Console.WriteLine($"Attendance Edge Agent — Status & Queue Overview");
    Console.WriteLine("==================================================");
    Console.WriteLine($"Agent ID:            {edge.AgentId}");
    Console.WriteLine($"Organisation ID:     {edge.OrganisationId}");
    Console.WriteLine($"API Base URL:        {edge.ApiBaseUrl}");
    Console.WriteLine($"Poll Interval:       {edge.PollSeconds}s");
    Console.WriteLine($"Data Directory:      {edge.DataDirectory}");
    Console.WriteLine($"Certificate Path:    {edge.ClientCertificatePath}");
    Console.WriteLine($"Certificate Valid:   {(certificate != null ? $"Yes (Thumbprint: {certificate.Thumbprint}, Expires: {certificate.NotAfter:u})" : "No certificate loaded")}");
    Console.WriteLine($"Pending Outbox Batches: {stats.PendingBatches}");
    Console.WriteLine($"Pending Outbox Events:  {stats.PendingEvents}");
    Console.WriteLine($"Archived Batches:       {stats.ArchivedBatches}");
    Console.WriteLine("==================================================");
    return;
}

await app.RunAsync();
