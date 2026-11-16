namespace HRM.AttendanceEdgeAgent.Configuration;

public sealed class AttendanceEdgeOptions
{
    public string AgentId { get; set; } = "";
    public string OrganisationId { get; set; } = "";
    public Uri ApiBaseUrl { get; set; } = new("https://dhr.example.invalid/");
    public int PollSeconds { get; set; } = 30;
    public string ClientCertificatePath { get; set; } = "";
    public string? ClientCertificatePassword { get; set; }
    public string DataDirectory { get; set; } = "./data";
    public int MaxBatchSize { get; set; } = 500;
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public int DeviceTimeoutSeconds { get; set; } = 5;
    public List<DeviceOptions> Devices { get; set; } = [];
}

public sealed class DeviceOptions
{
    public int DeviceId { get; set; }
    public string Adapter { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string? Location { get; set; }
    public bool Enabled { get; set; } = true;
    public string? DropPath { get; set; }
}
