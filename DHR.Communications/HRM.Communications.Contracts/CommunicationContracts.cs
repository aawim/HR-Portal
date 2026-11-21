namespace HRM.Communications.Contracts;

public static class CommunicationEventCodes
{
    public const string OrganisationEventPublished = "organisation-event.published.v1";
    public const string OrganisationEventCancelled = "organisation-event.cancelled.v1";
    public const string TelegramTestRequested = "telegram.test-requested.v1";
    public const string TelegramTestBody = "DHR Communication Hub test message. This confirms the configured Telegram delivery path.";
}

public sealed record CommunicationEvent(Guid EventId, string EventCode, int OrganisationId, string AggregateType, string AggregateId, string PayloadJson, DateTime OccurredAtUtc);
public sealed record CommunicationSendRequest(string IdempotencyKey, string CredentialReference, string Destination, string Subject, string Body);
public sealed record ProviderSendResult(bool Succeeded, string? ProviderMessageId = null, string? ErrorCode = null, bool IsTransient = false, TimeSpan? RetryAfter = null);
public sealed record ProviderValidationResult(bool Succeeded, string? ErrorCode = null);
public interface ICommunicationConnectionValidator
{
    Task<ProviderValidationResult> ValidateAsync(string credentialReference, CancellationToken cancellationToken);
    Task<ProviderValidationResult> ValidateDestinationAsync(string credentialReference, string destination, CancellationToken cancellationToken);
}

public interface ICommunicationProvider
{
    string ProviderCode { get; }
    Task<ProviderSendResult> SendAsync(CommunicationSendRequest request, CancellationToken cancellationToken);
}
