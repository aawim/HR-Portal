namespace HRM.DTOs.Communications;

public sealed class CommunicationHubConfigurationDto
{
    public List<CommunicationPlatformDto> Platforms { get; set; } =
    [
        new("TELEGRAM", "Telegram", "Channel and group announcements", true),
        new("WHATSAPP", "WhatsApp Business", "Individual employee messages", false),
        new("MSTEAMS", "Microsoft Teams", "Channel announcements", false),
        new("FACEBOOK_PAGE", "Facebook Pages", "Public Page announcements", false),
        new("FACEBOOK_MESSENGER", "Facebook Messenger", "Individual conversations", false)
    ];
    public List<TelegramConnectionDto> TelegramConnections { get; set; } = [];
    public List<TelegramDestinationDto> TelegramDestinations { get; set; } = [];
    public List<CommunicationRuleDto> Rules { get; set; } = [];
    public List<CommunicationDeliveryHistoryDto> RecentDeliveries { get; set; } = [];
    public List<CommunicationApprovalPreviewDto> PendingApprovals { get; set; } = [];
}
public sealed record CommunicationApprovalPreviewDto(long Id, string Subject, string Body, DateTime CreatedAtUtc, int? RequestId = null);
public sealed record CommunicationApprovalReviewDto(long Id, int RequestId, int ExpectedState, string Subject, string Body, string Destination, string Status, string Stage, bool CanApprove, bool CanCancel);
public sealed class CommunicationApprovalActionDto
{
    public long Id { get; set; }
    public int ExpectedState { get; set; }
    public string Action { get; set; } = "";
    public string? Comment { get; set; }
}
public sealed record CommunicationPlatformDto(string Code, string Name, string Purpose, bool CanConfigure);
public sealed record CommunicationConnectionValidationDto(bool Succeeded, string? ErrorCode);
public sealed class TelegramConnectionDto { public long Id { get; set; } public string Name { get; set; } = ""; public string CredentialReference { get; set; } = ""; public bool IsEnabled { get; set; } }
public sealed class TelegramDestinationDto { public long Id { get; set; } public long ConnectionId { get; set; } public string DisplayName { get; set; } = ""; public string ChatId { get; set; } = ""; public bool IsActive { get; set; } }
public sealed class CommunicationRuleDto { public long Id { get; set; } public long? DestinationId { get; set; } public string Name { get; set; } = ""; public string EventCode { get; set; } = "organisation-event.published.v1"; public string TemplateBody { get; set; } = "{{Title}}"; public bool RequiresApproval { get; set; } = true; public bool IsEnabled { get; set; } }
public sealed class CommunicationDeliveryHistoryDto { public long Id { get; set; } public bool CanRetry { get; set; } public string Provider { get; set; } = ""; public string Destination { get; set; } = ""; public string Status { get; set; } = ""; public int Attempts { get; set; } public string? ErrorCode { get; set; } public DateTime CreatedAtUtc { get; set; } public DateTime? SentAtUtc { get; set; } }
