using System.Data;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.Services;

public sealed record ChatContact(int UserId, string Name, bool IsHr, bool IsSupervisor);

public sealed record ChatThread(long Id, string Name, int Unread, DateTime UpdatedAtUtc) { public bool IsGroup { get; init; } }

public sealed record ChatRequestStatus(int Id, string Name);

public sealed record ChatReference(int Id, string Reference, string Type, string? Url);

public sealed record ChatMessage(long Id, bool IsMine, string Body, DateTime SentAtUtc, ChatReference? Request, bool HasUnavailableReference) { public string SenderName { get; init; } = ""; }

public sealed record ChatPage(long Id, string Name, List<ChatMessage> Messages, bool HasOlder, bool CanSend)
{
    public bool IsGroup { get; init; }
    public bool CanManage { get; init; }
    public List<ChatMember> Members { get; init; } = [];
}

public sealed record ChatMember(long MembershipId, int UserId, string Name, bool IsManager, bool IsMe, bool IsAvailable, DateTime JoinedAtUtc, DateTime? LeftAtUtc);
