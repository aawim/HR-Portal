using System;
using System.Collections.Generic;

namespace HRM.Models;

public static class HelpdeskTicketStatuses
{
    public const string BotActive = "BotActive";
    public const string WaitingForAgent = "WaitingForAgent";
    public const string AgentAssigned = "AgentAssigned";
    public const string Resolved = "Resolved";
}

public static class HelpdeskSenderTypes
{
    public const string User = "User";
    public const string Bot = "Bot";
    public const string Agent = "Agent";
    public const string System = "System";
}

public sealed class HelpdeskTicket
{
    public long Id { get; set; }
    public string TicketNumber { get; set; } = "";
    public int OrganisationId { get; set; }
    public int RequesterUserId { get; set; }
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Status { get; set; } = HelpdeskTicketStatuses.BotActive;
    public string Priority { get; set; } = "Normal";
    public int? AssignedAgentUserId { get; set; }
    public string? EscalationReason { get; set; }
    public int BotUnresolvedTurns { get; set; }
    public int? Rating { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? EscalatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public long RequesterLastReadMessageId { get; set; }
    public long AgentLastReadMessageId { get; set; }

    public ICollection<HelpdeskMessage> Messages { get; set; } = new List<HelpdeskMessage>();
}

public sealed class HelpdeskMessage
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string SenderType { get; set; } = HelpdeskSenderTypes.User;
    public int? SenderUserId { get; set; }
    public string SenderName { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsInternalNote { get; set; }
    public int? MatchedArticleId { get; set; }
    public decimal? Confidence { get; set; }
    public DateTime SentAtUtc { get; set; }

    public HelpdeskTicket Ticket { get; set; } = null!;
}

public sealed class HelpdeskArticle
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
