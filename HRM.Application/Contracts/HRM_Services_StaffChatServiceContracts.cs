using System.Data;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class StaffChatServiceContracts
{
    public const string PageUrl = "/online/chat";

    public const int MaximumGroupMembers = 50;

public interface IOperations
{
Task<List<ChatContact>> SearchContactsAsync(string? search, string filter = "all")
;
Task<long> StartAsync(int recipientUserId)
;
Task<List<ChatThread>> ListAsync()
;
Task<List<ChatRequestStatus>> GetRequestStatusesAsync(long conversationId)
;
Task<List<ChatReference>> SearchRequestsAsync(long conversationId, string? search,
        DateTime? fromDate = null, DateTime? toDate = null, int? statusId = null)
;
Task<long> SendAsync(long conversationId, string body, int? requestId, Guid clientMessageId)
;
Task<ChatPage> ReadAsync(long conversationId, long? beforeId = null, bool markRead = true)
;
Task<long> CreateGroupAsync(string title, IEnumerable<int> selectedUsers)
;
Task ConvertToGroupAsync(long conversationId, string title)
;
Task AddMemberAsync(long conversationId, int userId)
;
Task RemoveMemberAsync(long conversationId, int userId)
;
Task SetManagerAsync(long conversationId, int userId, bool isManager)
;
Task RenameGroupAsync(long conversationId, string title)
;
Task<ChatRequestDetails> GetRequestDetailsAsync(long conversationId, int requestId)
;
}
}
