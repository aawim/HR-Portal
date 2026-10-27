using System.Data;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class StaffChatService
{
    public const int MaximumGroupMembers = 50;
    private static string GroupTitle(string title)
    {
        title = title.Trim();
        if (title.Length is < 1 or > 120 || title.Any(char.IsControl)) throw new InvalidOperationException("Enter a group name of 1–120 characters.");
        return title;
    }
    private static void Event(HrmTeContext db, long chat, int actor, string action, int? subject = null, string? detail = null) =>
        db.StaffChatEvents.Add(new() { ConversationId = chat, ActorUserId = actor, Action = action, SubjectUserId = subject, Detail = detail, CreatedAtUtc = DateTime.UtcNow });
    private static async Task<StaffChatMembership> RequireManagerAsync(HrmTeContext db, StaffChatConversation chat, int user)
    {
        var member = await db.StaffChatMemberships.SingleOrDefaultAsync(m => m.ConversationId == chat.Id && m.UserId == user && m.LeftAtUtc == null);
        if (!chat.IsGroup || member?.IsManager != true) throw new UnauthorizedAccessException("Only a current group manager can change group members or settings.");
        return member;
    }
    private static void MemberNotice(HrmTeContext db, StaffChatConversation chat, User recipient, string text, bool removed = false) =>
        db.StaffNotifications.Add(new() { UserId = recipient.UserId, IndividualId = recipient.BusinessEntityID, OrganisationId = chat.OrganisationId,
            Kind = "ChatMembership", Message = text, Url = removed ? PageUrl : $"{PageUrl}?conversationId={chat.Id}", CreatedAtUtc = DateTime.UtcNow });

    public async Task<long> CreateGroupAsync(string title, IEnumerable<int> selectedUsers)
    {
        title = GroupTitle(title);
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await ScopeAsync(db);
        var ids = selectedUsers.Append(actor.User).Distinct().Order().ToList();
        if (ids.Count is < 2 or > MaximumGroupMembers) throw new InvalidOperationException($"Select 1–{MaximumGroupMembers - 1} colleagues for the group.");
        var users = await Eligible(db, actor.Org).Where(u => ids.Contains(u.UserId)).ToListAsync();
        if (users.Count != ids.Count) throw new UnauthorizedAccessException("All group members must be active staff in your current organisation.");
        var now = DateTime.UtcNow;
        var chat = new StaffChatConversation { OrganisationId = actor.Org, IsGroup = true, Title = title, FirstUserId = ids[0], SecondUserId = ids[1],
            CreatedAtUtc = now, UpdatedAtUtc = now,
            Members = ids.Select(id => new StaffChatMembership { UserId = id, IsManager = id == actor.User, JoinedAtUtc = now, AddedByUserId = actor.User }).ToList() };
        db.StaffChatConversations.Add(chat); await db.SaveChangesAsync();
        Event(db, chat.Id, actor.User, "Created", detail: title);
        foreach (var user in users.Where(u => u.UserId != actor.User)) MemberNotice(db, chat, user, $"You were added to group {title}.");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return chat.Id;
    }
    public async Task ConvertToGroupAsync(long conversationId, string title)
    {
        title = GroupTitle(title);
        await using var db = await factory.CreateDbContextAsync(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await ScopeAsync(db);
        var chat = await Owned(db, actor.User, actor.Org).SingleOrDefaultAsync(c => c.Id == conversationId) ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        if (chat.IsGroup) throw new InvalidOperationException("This conversation is already a group.");
        var members = await db.StaffChatMemberships.Where(m => m.ConversationId == chat.Id && m.LeftAtUtc == null).ToListAsync();
        chat.IsGroup = true; chat.Title = title; chat.UpdatedAtUtc = DateTime.UtcNow;
        foreach (var member in members) member.IsManager = true;
        Event(db, chat.Id, actor.User, "Converted", detail: title);
        var ids = members.Where(m => m.UserId != actor.User).Select(m => m.UserId).ToArray();
        foreach (var user in await Eligible(db, actor.Org).Where(u => ids.Contains(u.UserId)).ToListAsync())
            MemberNotice(db, chat, user, $"Your conversation was converted to group {title}. Existing messages are retained.");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task AddMemberAsync(long conversationId, int userId)
    {
        await using var db = await factory.CreateDbContextAsync(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await ScopeAsync(db);
        var chat = await Owned(db, actor.User, actor.Org).SingleOrDefaultAsync(c => c.Id == conversationId) ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        await RequireManagerAsync(db, chat, actor.User);
        if (await db.StaffChatMemberships.AnyAsync(m => m.ConversationId == chat.Id && m.UserId == userId && m.LeftAtUtc == null))
            throw new InvalidOperationException("This staff member already belongs to the group.");
        if (await db.StaffChatMemberships.CountAsync(m => m.ConversationId == chat.Id && m.LeftAtUtc == null) >= MaximumGroupMembers)
            throw new InvalidOperationException($"A group can have up to {MaximumGroupMembers} current members.");
        var user = await Eligible(db, actor.Org).SingleOrDefaultAsync(u => u.UserId == userId) ?? throw new UnauthorizedAccessException("Choose an active staff member in this organisation.");
        var last = await db.StaffChatMessages.Where(m => m.ConversationId == chat.Id).Select(m => (long?)m.Id).MaxAsync() ?? 0;
        db.StaffChatMemberships.Add(new() { ConversationId = chat.Id, UserId = userId, JoinedAtUtc = DateTime.UtcNow, AddedByUserId = actor.User,
            JoinedAfterMessageId = last, LastReadMessageId = last });
        chat.UpdatedAtUtc = DateTime.UtcNow; Event(db, chat.Id, actor.User, "Added", userId);
        MemberNotice(db, chat, user, $"You were added to group {chat.Title}. Messages from this membership onward are available.");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    private static async Task ProtectLastManagerAsync(HrmTeContext db, StaffChatConversation chat, StaffChatMembership target)
    {
        if (!target.IsManager) return;
        var managerIds = await db.StaffChatMemberships.Where(m => m.ConversationId == chat.Id && m.LeftAtUtc == null && m.IsManager && m.UserId != target.UserId)
            .Select(m => m.UserId).ToListAsync();
        if (!await Eligible(db, chat.OrganisationId).AnyAsync(u => managerIds.Contains(u.UserId)))
            throw new InvalidOperationException("Make another active member a group manager before removing or leaving the last manager.");
    }
    public async Task RemoveMemberAsync(long conversationId, int userId)
    {
        await using var db = await factory.CreateDbContextAsync(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await ScopeAsync(db);
        var chat = await Owned(db, actor.User, actor.Org).SingleOrDefaultAsync(c => c.Id == conversationId) ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        if (!chat.IsGroup) throw new InvalidOperationException("Convert this direct conversation to a group before managing members.");
        if (actor.User != userId) await RequireManagerAsync(db, chat, actor.User);
        var target = await db.StaffChatMemberships.SingleOrDefaultAsync(m => m.ConversationId == chat.Id && m.UserId == userId && m.LeftAtUtc == null)
            ?? throw new InvalidOperationException("This staff member is no longer in the group.");
        await ProtectLastManagerAsync(db, chat, target);
        target.LeftAtUtc = DateTime.UtcNow; target.RemovedByUserId = actor.User;
        target.LeftAfterMessageId = await db.StaffChatMessages.Where(m => m.ConversationId == chat.Id).Select(m => (long?)m.Id).MaxAsync() ?? 0;
        chat.UpdatedAtUtc = DateTime.UtcNow; Event(db, chat.Id, actor.User, actor.User == userId ? "Left" : "Removed", userId);
        var url = $"{PageUrl}?conversationId={chat.Id}";
        foreach (var notification in await db.StaffNotifications.Where(n => n.UserId == userId && n.OrganisationId == actor.Org && n.Url == url && n.ReadAtUtc == null).ToListAsync()) notification.ReadAtUtc = DateTime.UtcNow;
        var user = await db.Users.SingleAsync(u => u.UserId == userId);
        MemberNotice(db, chat, user, actor.User == userId ? $"You left group {chat.Title}." : $"You were removed from group {chat.Title}.", removed: true);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task SetManagerAsync(long conversationId, int userId, bool isManager)
    {
        await using var db = await factory.CreateDbContextAsync(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await ScopeAsync(db);
        var chat = await Owned(db, actor.User, actor.Org).SingleOrDefaultAsync(c => c.Id == conversationId) ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        await RequireManagerAsync(db, chat, actor.User);
        var target = await db.StaffChatMemberships.SingleOrDefaultAsync(m => m.ConversationId == chat.Id && m.UserId == userId && m.LeftAtUtc == null)
            ?? throw new InvalidOperationException("Choose a current member.");
        if (isManager && !await Eligible(db, actor.Org).AnyAsync(u => u.UserId == userId)) throw new InvalidOperationException("Only an active staff member can manage a group.");
        if (!isManager) await ProtectLastManagerAsync(db, chat, target);
        target.IsManager = isManager; chat.UpdatedAtUtc = DateTime.UtcNow;
        Event(db, chat.Id, actor.User, isManager ? "ManagerAdded" : "ManagerRemoved", userId);
        MemberNotice(db, chat, await db.Users.SingleAsync(u => u.UserId == userId), $"Your manager role was {(isManager ? "added in" : "removed from")} group {chat.Title}.");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    public async Task RenameGroupAsync(long conversationId, string title)
    {
        title = GroupTitle(title);
        await using var db = await factory.CreateDbContextAsync(); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var actor = await ScopeAsync(db);
        var chat = await Owned(db, actor.User, actor.Org).SingleOrDefaultAsync(c => c.Id == conversationId) ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        await RequireManagerAsync(db, chat, actor.User); chat.Title = title; chat.UpdatedAtUtc = DateTime.UtcNow;
        Event(db, chat.Id, actor.User, "Renamed", detail: title);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
