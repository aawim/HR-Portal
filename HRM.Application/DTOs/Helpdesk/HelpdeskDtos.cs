using System;
using System.Collections.Generic;

namespace HRM.DTOs.Helpdesk;

public sealed record HelpdeskOrganisationOption(int Id, string Name);

public sealed record HelpdeskTicketSummaryDto(
    long Id,
    string TicketNumber,
    int OrganisationId,
    string OrganisationName,
    int RequesterUserId,
    string RequesterName,
    string Subject,
    string Category,
    string Status,
    string Priority,
    int? AssignedAgentUserId,
    string? AssignedAgentName,
    string? EscalationReason,
    int UnreadCount,
    string LastMessagePreview,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? EscalatedAtUtc,
    DateTime? ResolvedAtUtc,
    int? Rating);

public sealed record HelpdeskMessageDto(
    long Id,
    long TicketId,
    string SenderType,
    int? SenderUserId,
    string SenderName,
    string Body,
    bool IsInternalNote,
    bool IsMine,
    decimal? Confidence,
    DateTime SentAtUtc);

public sealed record HelpdeskTicketDetailDto(
    HelpdeskTicketSummaryDto Ticket,
    List<HelpdeskMessageDto> Messages,
    bool CanReplyAsUser,
    bool CanActAsAgent,
    bool CanEscalate,
    bool CanResolve);

public sealed class HelpdeskAgentInboxDto
{
    public int WaitingCount { get; set; }
    public int AgentAssignedCount { get; set; }
    public int BotActiveCount { get; set; }
    public int ResolvedCount { get; set; }
    public string BotEngineMode { get; set; } = "Knowledge Base (Hybrid Ready)";
    public List<HelpdeskOrganisationOption> Organisations { get; set; } = [];
    public List<HelpdeskTicketSummaryDto> Tickets { get; set; } = [];
}

public sealed class HelpdeskArticleDto
{
    public int Id { get; set; }
    public string Category { get; set; } = "General";
    public string Title { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string Answer { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
