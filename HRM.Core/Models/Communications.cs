namespace HRM.Models;

public sealed class CommunicationOutbox
{
    public long CommunicationOutboxId { get; set; }
    public Guid EventId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string EventCode { get; set; } = "";
    public string AggregateType { get; set; } = "";
    public string AggregateId { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
}

public sealed class CommunicationProviderConnection
{
    public long CommunicationProviderConnectionId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string ProviderCode { get; set; } = "";
    public string ConnectionName { get; set; } = "";
    public string CredentialReference { get; set; } = "";
    public string? ConfigurationJson { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CommunicationDestination
{
    public long CommunicationDestinationId { get; set; }
    public long CommunicationProviderConnectionId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string DestinationType { get; set; } = "";
    public string ExternalDestinationId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class CommunicationAutomationRule
{
    public long? CommunicationDestinationId { get; set; }
    public long CommunicationAutomationRuleId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string EventCode { get; set; } = "";
    public string RuleName { get; set; } = "";
    public string TemplateBody { get; set; } = "";
    public bool RequiresApproval { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class CommunicationMessage
{
    public long? DestinationConnectionId { get; set; }
    public string? DestinationExternalId { get; set; }
    public string? DestinationDisplayName { get; set; }
    public int? ApprovalRequestId { get; set; }
    public int? ApprovalJobId { get; set; }
    public long? CommunicationDestinationId { get; set; }
    public string ApprovalStatus { get; set; } = "NotRequired";
    public long? CommunicationAutomationRuleId { get; set; }
    public long CommunicationMessageId { get; set; }
    public long CommunicationOutboxId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string Subject { get; set; } = "";
    public string RenderedBody { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CommunicationDelivery
{
    public long CommunicationDeliveryId { get; set; }
    public long CommunicationMessageId { get; set; }
    public long CommunicationProviderConnectionId { get; set; }
    public long CommunicationDestinationId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string Status { get; set; } = "Pending";
    public string IdempotencyKey { get; set; } = "";
    public string? ProviderMessageId { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CommunicationDeliveryAttempt
{
    public long CommunicationDeliveryAttemptId { get; set; }
    public long CommunicationDeliveryId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
    public string ResultStatus { get; set; } = "";
    public int? HttpStatusCode { get; set; }
    public string? SanitizedErrorCode { get; set; }
}
