
namespace HRM.Models;

public sealed class StaffChatConversation
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public bool IsGroup { get; set; }
    public string? Title { get; set; }
    public int FirstUserId { get; set; }
    public int SecondUserId { get; set; }
    public long FirstReadMessageId { get; set; }
    public long SecondReadMessageId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<StaffChatMembership> Members { get; set; } = new List<StaffChatMembership>();
    public ICollection<StaffChatMessage> Messages { get; set; } = new List<StaffChatMessage>();
}

public sealed class StaffChatMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public int SenderUserId { get; set; }
    public Guid ClientMessageId { get; set; }
    public string Body { get; set; } = "";
    public int? RequestId { get; set; }
    public DateTime SentAtUtc { get; set; }
    public StaffChatConversation Conversation { get; set; } = null!;
}

public sealed class StaffChatMembership
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public int UserId { get; set; }
    public bool IsManager { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public int? AddedByUserId { get; set; }
    public long JoinedAfterMessageId { get; set; }
    public long LastReadMessageId { get; set; }
    public DateTime? LeftAtUtc { get; set; }
    public int? RemovedByUserId { get; set; }
    public long? LeftAfterMessageId { get; set; }
    public StaffChatConversation Conversation { get; set; } = null!;
}

public sealed class StaffChatEvent
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public int ActorUserId { get; set; }
    public int? SubjectUserId { get; set; }
    public string Action { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
