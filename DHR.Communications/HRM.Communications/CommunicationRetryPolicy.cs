namespace HRM.Communications;

public static class CommunicationRetryPolicy
{
    public static bool CanRetry(string status, string? error, int attempts, DateTime createdAtUtc, DateTime nowUtc, int maxAgeHours)
        => status == "Failed" && attempts < 5 && createdAtUtc >= nowUtc.AddHours(-Math.Clamp(maxAgeHours, 1, 168)) &&
           error is "CredentialNotConfigured" or "ConnectionDisabledOrUnavailable" or "DestinationDisabledOrUnavailable" or "ProviderNotRegistered" or "OutboundDisabled";
}
