namespace HRM.ErrorLogs.Services;

/// <summary>
/// Explicit contextual data supplied when manually logging or capturing an error.
/// </summary>
public sealed class ErrorContext
{
    public string? Username { get; set; }
    public string? UserId { get; set; }
    public string? RemoteIp { get; set; }
    public string? PageLocation { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
    public string? UserAgent { get; set; }
    public string? TraceId { get; set; }
    public string? ApplicationSource { get; set; }
    public string? CustomDataJson { get; set; }
}
