namespace HRM.AttendanceEdgeAgent.Configuration;
public sealed class AttendanceEdgeOptions
{
    public string AgentId { get; set; } = "";
    public string OrganisationId { get; set; } = "";
    public Uri ApiBaseUrl { get; set; } = new("https://dhr.example.invalid/");
    public int PollSeconds { get; set; } = 30;
    public string ClientCertificatePath { get; set; } = "";
    public string? ClientCertificatePassword { get; set; }
    public List<DeviceOptions> Devices { get; set; } = [];
}
public sealed class DeviceOptions { public int DeviceId { get; set; } public string Adapter { get; set; } = ""; public string Host { get; set; } = ""; public int Port { get; set; } }
