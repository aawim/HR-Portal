// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-chat")]
public sealed class StaffChatController(global::HRM.Services.Contracts.StaffChatServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("search-contacts")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.ChatContact>>> SearchContacts([FromQuery] global::System.String? search, [FromQuery] global::System.String filter = "all")
    {
        return Ok(await service.SearchContactsAsync(search, filter));
    }

    [HttpPost("start")]
    public async Task<ActionResult<global::System.Int64>> Start([FromBody] StaffChatStartRequest request)
    {
        return Ok(await service.StartAsync(request.recipientUserId));
    }

    public sealed class StaffChatStartRequest
    {
        public global::System.Int32 recipientUserId { get; set; }
    }

    [HttpPost("list")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.ChatThread>>> List()
    {
        return Ok(await service.ListAsync());
    }

    [HttpGet("get-request-statuses")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.ChatRequestStatus>>> GetRequestStatuses([FromQuery] global::System.Int64 conversationId)
    {
        return Ok(await service.GetRequestStatusesAsync(conversationId));
    }

    [HttpGet("search-requests")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.ChatReference>>> SearchRequests([FromQuery] global::System.Int64 conversationId, [FromQuery] global::System.String? search, [FromQuery] global::System.DateTime? fromDate = null, [FromQuery] global::System.DateTime? toDate = null, [FromQuery] global::System.Int32? statusId = null)
    {
        return Ok(await service.SearchRequestsAsync(conversationId, search, fromDate, toDate, statusId));
    }

    [HttpPost("send")]
    public async Task<ActionResult<global::System.Int64>> Send([FromBody] StaffChatSendRequest request)
    {
        return Ok(await service.SendAsync(request.conversationId, request.body, request.requestId, request.clientMessageId));
    }

    public sealed class StaffChatSendRequest
    {
        public global::System.Int64 conversationId { get; set; }
        public global::System.String body { get; set; } = default!;
        public global::System.Int32? requestId { get; set; }
        public global::System.Guid clientMessageId { get; set; }
    }

    [HttpPost("read")]
    public async Task<ActionResult<global::HRM.Services.ChatPage>> Read([FromBody] StaffChatReadRequest request)
    {
        return Ok(await service.ReadAsync(request.conversationId, request.beforeId, request.markRead));
    }

    public sealed class StaffChatReadRequest
    {
        public global::System.Int64 conversationId { get; set; }
        public global::System.Int64? beforeId { get; set; } = null;
        public global::System.Boolean markRead { get; set; } = true;
    }

    [HttpPost("create-group")]
    public async Task<ActionResult<global::System.Int64>> CreateGroup([FromBody] StaffChatCreateGroupRequest request)
    {
        return Ok(await service.CreateGroupAsync(request.title, request.selectedUsers));
    }

    public sealed class StaffChatCreateGroupRequest
    {
        public global::System.String title { get; set; } = default!;
        public global::System.Collections.Generic.IEnumerable<global::System.Int32> selectedUsers { get; set; } = default!;
    }

    [HttpPost("convert-to-group")]
    public async Task<IActionResult> ConvertToGroup([FromBody] StaffChatConvertToGroupRequest request)
    {
        await service.ConvertToGroupAsync(request.conversationId, request.title);
        return NoContent();
    }

    public sealed class StaffChatConvertToGroupRequest
    {
        public global::System.Int64 conversationId { get; set; }
        public global::System.String title { get; set; } = default!;
    }

    [HttpPost("add-member")]
    public async Task<IActionResult> AddMember([FromBody] StaffChatAddMemberRequest request)
    {
        await service.AddMemberAsync(request.conversationId, request.userId);
        return NoContent();
    }

    public sealed class StaffChatAddMemberRequest
    {
        public global::System.Int64 conversationId { get; set; }
        public global::System.Int32 userId { get; set; }
    }

    [HttpPost("remove-member")]
    public async Task<IActionResult> RemoveMember([FromBody] StaffChatRemoveMemberRequest request)
    {
        await service.RemoveMemberAsync(request.conversationId, request.userId);
        return NoContent();
    }

    public sealed class StaffChatRemoveMemberRequest
    {
        public global::System.Int64 conversationId { get; set; }
        public global::System.Int32 userId { get; set; }
    }

    [HttpPost("set-manager")]
    public async Task<IActionResult> SetManager([FromBody] StaffChatSetManagerRequest request)
    {
        await service.SetManagerAsync(request.conversationId, request.userId, request.isManager);
        return NoContent();
    }

    public sealed class StaffChatSetManagerRequest
    {
        public global::System.Int64 conversationId { get; set; }
        public global::System.Int32 userId { get; set; }
        public global::System.Boolean isManager { get; set; }
    }

    [HttpPost("rename-group")]
    public async Task<IActionResult> RenameGroup([FromBody] StaffChatRenameGroupRequest request)
    {
        await service.RenameGroupAsync(request.conversationId, request.title);
        return NoContent();
    }

    public sealed class StaffChatRenameGroupRequest
    {
        public global::System.Int64 conversationId { get; set; }
        public global::System.String title { get; set; } = default!;
    }

    [HttpGet("get-request-details")]
    public async Task<ActionResult<global::HRM.Services.ChatRequestDetails>> GetRequestDetails([FromQuery] global::System.Int64 conversationId, [FromQuery] global::System.Int32 requestId)
    {
        return Ok(await service.GetRequestDetailsAsync(conversationId, requestId));
    }

}
