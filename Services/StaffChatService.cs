using System.Data;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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

public sealed partial class StaffChatService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
{
    public const string PageUrl = "/online/chat";
    private static DateTime Today => DateTime.UtcNow.AddHours(5).Date;
    private static IQueryable<User> Eligible(HrmTeContext db, int org) => db.Users.Where(u => u.IsActive && u.LocalUserStateId == 1 &&
        u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == org && db.Jobs.Any(j => j.IndividualID == u.BusinessEntityID &&
            j.OrganisationID == org && j.JobStateId == 4 && j.JoinedDate <= Today && (!j.TerminatedDate.HasValue || j.TerminatedDate > Today) &&
            db.JobPositions.Any(p => p.JobId == j.JobId && p.JobPositionStateId == 4 && p.FromDate <= Today && (!p.ToDate.HasValue || p.ToDate > Today))));
    private async Task<(int User, int Person, int Org)> ScopeAsync(HrmTeContext db)
    {
        var context = await access.RequireContextAsync(); var org = await access.GetCurrentOrganisationIdAsync();
        var user = await Eligible(db, org).AsNoTracking().SingleOrDefaultAsync(u => u.UserId == context.UserId && u.BusinessEntityID == context.IndividualId);
        if (user == null || !await access.HasOrganisationAccessAsync(org))
            throw new UnauthorizedAccessException("Chat is available to active staff in their current organisation.");
        return (user.UserId, user.BusinessEntityID, org);
    }
    private static IQueryable<StaffChatConversation> Owned(HrmTeContext db, int user, int org) => db.StaffChatConversations
        .Where(c => c.OrganisationId == org && c.Members.Any(m => m.UserId == user && m.LeftAtUtc == null));
    private static async Task<string> NameAsync(HrmTeContext db, int user) => await db.Users.Where(u => u.UserId == user)
        .Select(u => u.FullName ?? db.Individuals.Where(i => i.BusinessEntityId == u.BusinessEntityID)
            .Select(i => (i.FirstNameEnglish ?? "") + " " + (i.LastNameEnglish ?? "")).FirstOrDefault() ?? "Staff member").SingleAsync();
    private static int Other(StaffChatConversation c, int user) => c.FirstUserId == user ? c.SecondUserId : c.FirstUserId;

    public async Task<List<ChatContact>> SearchContactsAsync(string? search, string filter = "all")
    {
        var text = search?.Trim() ?? "";
        if (text.Length > 100 || filter is not ("all" or "hr" or "supervisor")) throw new InvalidOperationException("Enter a shorter search and select a valid staff filter.");
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        var supervisors = new HashSet<int>();
        foreach (var job in await db.Jobs.Where(j => j.IndividualID == actor.Person && j.OrganisationID == actor.Org && j.JobStateId == 4 &&
            j.JoinedDate <= Today && (!j.TerminatedDate.HasValue || j.TerminatedDate > Today)).Select(j => j.JobId).ToListAsync())
            supervisors.UnionWith(await Leave.LeaveHandoverService.SupervisorsAsync(db, job));
        var people = Eligible(db, actor.Org).AsNoTracking().Where(u => u.UserId != actor.User);
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            people = people.Where(u => (u.FullName ?? "").Contains(word) || db.Individuals.Any(i => i.BusinessEntityId == u.BusinessEntityID &&
                ((i.FirstNameEnglish ?? "") + " " + (i.MiddleNameEnglish ?? "") + " " + (i.LastNameEnglish ?? "")).Contains(word)));
        var rows = await people.Select(u => new { u.UserId, u.BusinessEntityID, Name = u.FullName ?? db.Individuals.Where(i => i.BusinessEntityId == u.BusinessEntityID)
            .Select(i => (i.FirstNameEnglish ?? "") + " " + (i.LastNameEnglish ?? "")).FirstOrDefault() ?? "Staff member",
            IsHr = u.UserAssignedUserGroups.Any(g => (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisationId == u.UserOrganisationId) &&
                (g.UserGroup.GroupName == "HR" || g.UserGroup.GroupName.StartsWith("Human Resource"))) })
            .Where(u => filter == "all" || filter == "hr" && u.IsHr || filter == "supervisor" && supervisors.Contains(u.BusinessEntityID))
            .OrderByDescending(u => supervisors.Contains(u.BusinessEntityID)).ThenByDescending(u => u.IsHr).ThenBy(u => u.Name).Take(30).ToListAsync();
        return rows.Select(u => new ChatContact(u.UserId, u.Name.Trim(), u.IsHr, supervisors.Contains(u.BusinessEntityID)))
            .Where(u => filter == "all" || filter == "hr" && u.IsHr || filter == "supervisor" && u.IsSupervisor)
            .OrderByDescending(u => u.IsSupervisor).ThenByDescending(u => u.IsHr).ThenBy(u => u.Name).Take(30).ToList();
    }
    public async Task<long> StartAsync(int recipientUserId)
    {
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        if (recipientUserId == actor.User || !await Eligible(db, actor.Org).AnyAsync(u => u.UserId == recipientUserId))
            throw new UnauthorizedAccessException("Choose another active staff member in your organisation.");
        var first = Math.Min(actor.User, recipientUserId); var second = Math.Max(actor.User, recipientUserId);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var row = await db.StaffChatConversations.SingleOrDefaultAsync(c => c.OrganisationId == actor.Org && !c.IsGroup && c.FirstUserId == first && c.SecondUserId == second);
        if (row == null)
        {
            row = new() { OrganisationId = actor.Org, FirstUserId = first, SecondUserId = second, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow,
                Members = new[] { first, second }.Select(id => new StaffChatMembership { UserId = id, JoinedAtUtc = DateTime.UtcNow, AddedByUserId = actor.User }).ToList() };
            db.StaffChatConversations.Add(row); await db.SaveChangesAsync();
        }
        await tx.CommitAsync(); return row.Id;
    }
    public async Task<List<ChatThread>> ListAsync()
    {
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        var rows = await Owned(db, actor.User, actor.Org).AsNoTracking().OrderByDescending(c => c.UpdatedAtUtc).Take(100)
            .Select(c => new { c.Id, c.IsGroup, c.Title, Other = c.FirstUserId == actor.User ? c.SecondUserId : c.FirstUserId, c.UpdatedAtUtc,
                Unread = c.Messages.Count(m => m.SenderUserId != actor.User && m.Id > c.Members.Where(member => member.UserId == actor.User && member.LeftAtUtc == null)
                    .Select(member => member.LastReadMessageId).FirstOrDefault()) }).ToListAsync();
        var names = await NamesAsync(db, rows.Where(r => !r.IsGroup).Select(r => r.Other));
        return rows.Select(r => new ChatThread(r.Id, r.IsGroup ? r.Title ?? "Group chat" : names.GetValueOrDefault(r.Other, "Staff member"), r.Unread, r.UpdatedAtUtc) { IsGroup = r.IsGroup }).ToList();
    }
    private static Task<Dictionary<int, string>> NamesAsync(HrmTeContext db, IEnumerable<int> ids)
    {
        var users = ids.Distinct().ToArray();
        return db.Users.AsNoTracking().Where(u => users.Contains(u.UserId)).Select(u => new { u.UserId,
            Name = u.FullName ?? db.Individuals.Where(i => i.BusinessEntityId == u.BusinessEntityID)
                .Select(i => (i.FirstNameEnglish ?? "") + " " + (i.LastNameEnglish ?? "")).FirstOrDefault() ?? "Staff member" })
            .ToDictionaryAsync(u => u.UserId, u => u.Name);
    }
    private static IQueryable<StaffChatMessage> VisibleMessages(HrmTeContext db, long chatId, int user) => db.StaffChatMessages.Where(m => m.ConversationId == chatId &&
        db.StaffChatMemberships.Any(member => member.ConversationId == chatId && member.UserId == user && m.Id > member.JoinedAfterMessageId &&
            (!member.LeftAfterMessageId.HasValue || m.Id <= member.LeftAfterMessageId.Value)));
    private async Task<bool> CanReferenceAsync(HrmTeContext db, Request request, int userId, int org, bool? broadAccess = null)
    {
        if (request.OrganisationBusinessEntityID != org) return false;
        var user = await Eligible(db, org).AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;
        if (request.ApplicantBusinessEntityId == user.BusinessEntityID) return true;
        if (broadAccess ?? await HasBroadAccessAsync(userId)) return true;
        if (await CanOpenRequestAsync(db, request, userId, org)) return true;
        // A historical approval assignment alone must not survive removal from its User Group.
        var stageIds = await db.WfRequestApprovals.Where(a => a.ApproverUserId == userId &&
            a.RequestStageInstance.WorkflowRequestInstance.RequestId == request.RequestId)
            .Select(a => a.RequestStageInstance.WorkflowStageId).Distinct().ToListAsync();
        if (stageIds.Count == 0) return false;
        var jobId = await db.Jobs.Where(j => j.IndividualID == request.ApplicantBusinessEntityId && j.OrganisationID == org &&
            j.JobStateId == 4 && j.JoinedDate <= Today && (!j.TerminatedDate.HasValue || j.TerminatedDate > Today))
            .OrderByDescending(j => j.JoinedDate).Select(j => (int?)j.JobId).FirstOrDefaultAsync();
        var context = new HRM.DTOs.ApprovalFlow.WorkflowRequestContextDto { RequestId = request.RequestId, RequestTypeId = request.RequestTypeId,
            OrganisationBusinessEntityId = org, ApplicantBusinessEntityId = request.ApplicantBusinessEntityId ?? 0, SubjectJobId = jobId,
            EffectiveDate = DateOnly.FromDateTime(request.RequestEffectiveDate ?? request.ApplicationDate) };
        var resolver = new HRM.Services.Interfaces.AppovalWF.WorkflowApproverResolver(factory);
        foreach (var stage in stageIds)
        {
            try { if ((await resolver.ResolveAsync(stage, context)).Any(c => c.UserId == userId)) return true; }
            catch (InvalidOperationException) { /* No valid current approver configuration. */ }
        }
        return false;
    }
    private async Task<bool> HasBroadAccessAsync(int userId) => (await new EffectivePermissionService(factory).GetEffectivePermissionsAsync(userId))
        .Any(p => p.RoleKey is "Admin" or "Requests.View" or "Super.Admin.Manage");
    private static ChatReference Reference(Request request, long conversationId, bool canOpen) =>
        new(request.RequestId, request.ReferenceNumber ?? $"#{request.RequestId}", request.RequestType.TypeName,
            canOpen ? $"{PageUrl}/{conversationId}/requests/{request.RequestId}" : null);
    public async Task<List<ChatRequestStatus>> GetRequestStatusesAsync(long conversationId)
    {
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        if (!await Owned(db, actor.User, actor.Org).AnyAsync(c => c.Id == conversationId)) throw new UnauthorizedAccessException("Conversation unavailable.");
        return await db.RequestStates.AsNoTracking().Where(s => !s.IsFinalState).OrderBy(s => s.StateName)
            .Select(s => new ChatRequestStatus(s.RequestStateId, s.StateName.Trim())).ToListAsync();
    }
    public async Task<List<ChatReference>> SearchRequestsAsync(long conversationId, string? search,
        DateTime? fromDate = null, DateTime? toDate = null, int? statusId = null)
    {
        var text = search?.Trim() ?? ""; if (text.Length > 100) throw new InvalidOperationException("Search by reference, request type or staff name using up to 100 characters.");
        if (fromDate.HasValue && (fromDate.Value.Year < 1900 || fromDate.Value.Year > 2200) ||
            toDate.HasValue && (toDate.Value.Year < 1900 || toDate.Value.Year > 2200) ||
            fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            throw new InvalidOperationException("Enter a valid submitted-date range, with the end on or after the start.");
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        var chat = await Owned(db, actor.User, actor.Org).AsNoTracking().SingleOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        var actorBroad = await HasBroadAccessAsync(actor.User);
        IQueryable<Request> source = db.Requests.AsNoTracking().Include(r => r.RequestType).Where(r => r.OrganisationBusinessEntityID == actor.Org && !r.RequestState.IsFinalState);
        if (statusId.HasValue)
        {
            if (statusId.Value <= 0 || !await db.RequestStates.AnyAsync(s => s.RequestStateId == statusId && !s.IsFinalState))
                throw new InvalidOperationException("Select an open request status.");
            source = source.Where(r => r.RequestStateId == statusId.Value);
        }
        // ApplicationDate is stored in UTC. Date controls use the organisation's Maldives local day.
        if (fromDate.HasValue) { var start = fromDate.Value.Date.AddHours(-5); source = source.Where(r => r.ApplicationDate >= start); }
        if (toDate.HasValue) { var end = toDate.Value.Date.AddDays(1).AddHours(-5); source = source.Where(r => r.ApplicationDate < end); }
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            source = source.Where(r => (r.ReferenceNumber ?? "").Contains(word) || r.RequestType.TypeName.Contains(word) ||
                db.Individuals.Any(i => i.BusinessEntityId == r.ApplicantBusinessEntityId &&
                    ((i.FirstNameEnglish ?? "") + " " + (i.MiddleNameEnglish ?? "") + " " + (i.LastNameEnglish ?? "")).Contains(word)) ||
                db.Users.Any(u => u.BusinessEntityID == r.ApplicantBusinessEntityId && u.UserOrganisation!.BusinessEntityID == actor.Org && (u.FullName ?? "").Contains(word)));
        var candidates = await source.OrderByDescending(r => r.ApplicationDate).ThenByDescending(r => r.RequestId).Take(100).ToListAsync();
        var result = new List<ChatReference>();
        foreach (var r in candidates)
        {
            if (await CanReferenceAsync(db, r, actor.User, actor.Org, actorBroad))
            {
                result.Add(Reference(r, chat.Id, await CanOpenRequestAsync(db, r, actor.User, actor.Org)));
                if (result.Count == 20) break;
            }
        }
        return result;
    }
    public async Task<long> SendAsync(long conversationId, string body, int? requestId, Guid clientMessageId)
    {
        body = body.Trim();
        if (body.Length is < 1 or > 4000 || body.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')) || clientMessageId == Guid.Empty)
            throw new InvalidOperationException("Enter a message of 1–4,000 characters.");
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var chat = await Owned(db, actor.User, actor.Org).SingleOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        var memberIds = await db.StaffChatMemberships.Where(m => m.ConversationId == chat.Id && m.LeftAtUtc == null).Select(m => m.UserId).ToListAsync();
        var recipients = await Eligible(db, actor.Org).Where(u => memberIds.Contains(u.UserId) && u.UserId != actor.User).ToListAsync();
        if (recipients.Count == 0) throw new InvalidOperationException("No other active staff members are available in this conversation.");
        var replay = await db.StaffChatMessages.AsNoTracking().SingleOrDefaultAsync(m => m.SenderUserId == actor.User && m.ClientMessageId == clientMessageId);
        if (replay != null)
        {
            if (replay.ConversationId != conversationId || replay.Body != body || replay.RequestId != requestId) throw new InvalidOperationException("Refresh the chat before sending this message.");
            return replay.Id;
        }
        if (requestId.HasValue)
        {
            var request = await db.Requests.AsNoTracking().Include(r => r.RequestState).SingleOrDefaultAsync(r => r.RequestId == requestId);
            if (request == null || !await CanReferenceAsync(db, request, actor.User, actor.Org))
                throw new UnauthorizedAccessException("You do not have permission to attach this request.");
            if (request.RequestState.IsFinalState)
                throw new InvalidOperationException("This request is already final. Select a request that is still open.");
        }
        var recent = DateTime.UtcNow.AddMinutes(-1);
        if (await db.StaffChatMessages.CountAsync(m => m.SenderUserId == actor.User && m.SentAtUtc >= recent) >= 30)
            throw new InvalidOperationException("Please wait a moment before sending more messages.");
        var message = new StaffChatMessage { ConversationId = chat.Id, SenderUserId = actor.User, Body = body, RequestId = requestId,
            ClientMessageId = clientMessageId, SentAtUtc = DateTime.UtcNow };
        db.StaffChatMessages.Add(message); chat.UpdatedAtUtc = message.SentAtUtc;
        var url = $"{PageUrl}?conversationId={chat.Id}";
        var senderName = await NameAsync(db, actor.User);
        foreach (var recipient in recipients)
        {
            var notification = await db.StaffNotifications.FirstOrDefaultAsync(n => n.UserId == recipient.UserId && n.OrganisationId == actor.Org && n.Kind == "Chat" && n.Url == url && n.ReadAtUtc == null);
            if (notification == null)
                db.StaffNotifications.Add(new() { UserId = recipient.UserId, IndividualId = recipient.BusinessEntityID, OrganisationId = actor.Org,
                    Kind = "Chat", Message = chat.IsGroup ? $"New message in {chat.Title} from {senderName}." : $"New chat message from {senderName}.", Url = url, CreatedAtUtc = message.SentAtUtc });
            else notification.CreatedAtUtc = message.SentAtUtc;
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return message.Id;
    }
    public async Task<ChatPage> ReadAsync(long conversationId, long? beforeId = null, bool markRead = true)
    {
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var chat = await Owned(db, actor.User, actor.Org).SingleOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new UnauthorizedAccessException("Conversation unavailable.");
        var member = await db.StaffChatMemberships.SingleAsync(m => m.ConversationId == chat.Id && m.UserId == actor.User && m.LeftAtUtc == null);
        var visible = VisibleMessages(db, chat.Id, actor.User);
        var rows = await visible.AsNoTracking().Where(m => !beforeId.HasValue || m.Id < beforeId.Value)
            .OrderByDescending(m => m.Id).Take(50).ToListAsync();
        var senderNames = await NamesAsync(db, rows.Select(m => m.SenderUserId));
        var result = new List<ChatMessage>();
        var references = new Dictionary<int, ChatReference?>();
        foreach (var message in rows.OrderBy(m => m.Id))
        {
            ChatReference? reference = null;
            if (message.RequestId.HasValue)
            {
                if (!references.TryGetValue(message.RequestId.Value, out reference))
                {
                    var request = await db.Requests.AsNoTracking().Include(r => r.RequestType).SingleOrDefaultAsync(r => r.RequestId == message.RequestId);
                    if (request?.OrganisationBusinessEntityID == actor.Org)
                        reference = Reference(request, chat.Id, await CanOpenRequestAsync(db, request, actor.User, actor.Org));
                    references[message.RequestId.Value] = reference;
                }
            }
            result.Add(new(message.Id, message.SenderUserId == actor.User, message.Body, message.SentAtUtc, reference, message.RequestId.HasValue && reference == null) { SenderName = senderNames.GetValueOrDefault(message.SenderUserId, "Staff member") });
        }
        if (markRead && !beforeId.HasValue && rows.Count > 0)
        {
            var last = rows.Max(m => m.Id);
            member.LastReadMessageId = Math.Max(last, member.LastReadMessageId);
            var url = $"{PageUrl}?conversationId={chat.Id}";
            foreach (var notification in await db.StaffNotifications.Where(n => n.UserId == actor.User && n.OrganisationId == actor.Org && n.Kind == "Chat" && n.Url == url && n.ReadAtUtc == null).ToListAsync())
                notification.ReadAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        var oldestId = rows.Count > 0 ? rows.Min(m => m.Id) : 0;
        var older = oldestId > 0 && await visible.AnyAsync(m => m.Id < oldestId);
        var memberRows = await db.StaffChatMemberships.AsNoTracking().Where(m => m.ConversationId == chat.Id && (m.LeftAtUtc == null || member.IsManager))
            .OrderBy(m => m.JoinedAtUtc).ToListAsync();
        var names = await NamesAsync(db, memberRows.Select(m => m.UserId));
        var userIds = memberRows.Select(m => m.UserId).ToArray();
        var eligible = await Eligible(db, actor.Org).Where(u => userIds.Contains(u.UserId)).Select(u => u.UserId).ToListAsync();
        return new(chat.Id, chat.IsGroup ? chat.Title ?? "Group chat" : await NameAsync(db, Other(chat, actor.User)), result, older,
            memberRows.Any(m => m.LeftAtUtc == null && m.UserId != actor.User && eligible.Contains(m.UserId)))
        {
            IsGroup = chat.IsGroup, CanManage = chat.IsGroup && member.IsManager,
            Members = memberRows.Select(m => new ChatMember(m.Id, m.UserId, names.GetValueOrDefault(m.UserId, "Staff member"),
                m.IsManager, m.UserId == actor.User, eligible.Contains(m.UserId), m.JoinedAtUtc, m.LeftAtUtc)).ToList()
        };
    }
}
