using HRM.AttendanceEdgeAgent.Adapters;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Ingestion;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "HRM_EDGE_");
builder.Services.AddOptions<AttendanceEdgeOptions>().Bind(builder.Configuration.GetSection("AttendanceEdge")).ValidateOnStart();
var edge = builder.Configuration.GetSection("AttendanceEdge").Get<AttendanceEdgeOptions>() ?? new();
if (string.IsNullOrWhiteSpace(edge.ClientCertificatePath) || !File.Exists(edge.ClientCertificatePath)) throw new InvalidOperationException("A valid mTLS client certificate path is required.");
var certificate = new X509Certificate2(edge.ClientCertificatePath, edge.ClientCertificatePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
if (!certificate.HasPrivateKey) throw new InvalidOperationException("The mTLS certificate must include a private key.");
builder.Services.AddHttpClient<DhrIngestionClient>().ConfigurePrimaryHttpMessageHandler(()=>new HttpClientHandler { ClientCertificates = { certificate } });
builder.Services.AddSingleton<IDeviceAdapter, PlaceholderDeviceAdapter>();
builder.Services.AddHostedService<AttendancePollingWorker>();
await builder.Build().RunAsync();
