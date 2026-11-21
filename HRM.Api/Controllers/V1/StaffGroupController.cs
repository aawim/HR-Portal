// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-group")]
public sealed class StaffGroupController(global::HRM.Services.Contracts.StaffGroupServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Services.Contracts.StaffGroupServiceContracts.StaffGroupWorkspaceDto>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpGet("get-detail")]
    public async Task<ActionResult<global::HRM.Services.Contracts.StaffGroupServiceContracts.StaffGroupDetailDto>> GetDetail([FromQuery] global::System.Int32 staffGroupId)
    {
        return Ok(await service.GetDetailAsync(staffGroupId));
    }

    [HttpPost("save-group")]
    public async Task<ActionResult<global::HRM.Services.Contracts.StaffGroupServiceContracts.StaffGroupDto>> SaveGroup([FromBody] StaffGroupSaveGroupRequest request)
    {
        return Ok(await service.SaveGroupAsync(request.request));
    }

    public sealed class StaffGroupSaveGroupRequest
    {
        public global::HRM.Services.Contracts.StaffGroupServiceContracts.StaffGroupSaveRequest request { get; set; } = default!;
    }

    [HttpPost("add-members")]
    public async Task<IActionResult> AddMembers([FromBody] StaffGroupAddMembersRequest request)
    {
        await service.AddMembersAsync(request.request);
        return NoContent();
    }

    public sealed class StaffGroupAddMembersRequest
    {
        public global::HRM.Services.Contracts.StaffGroupServiceContracts.StaffGroupMembersRequest request { get; set; } = default!;
    }

    [HttpPost("remove-member")]
    public async Task<IActionResult> RemoveMember([FromBody] StaffGroupRemoveMemberRequest request)
    {
        await service.RemoveMemberAsync(request.staffGroupId, request.individualId);
        return NoContent();
    }

    public sealed class StaffGroupRemoveMemberRequest
    {
        public global::System.Int32 staffGroupId { get; set; }
        public global::System.Int32 individualId { get; set; }
    }

    [HttpPost("set-admin-portal-access")]
    public async Task<IActionResult> SetAdminPortalAccess([FromBody] StaffGroupSetAdminPortalAccessRequest request)
    {
        await service.SetAdminPortalAccessAsync(request.staffGroupId, request.enabled);
        return NoContent();
    }

    public sealed class StaffGroupSetAdminPortalAccessRequest
    {
        public global::System.Int32 staffGroupId { get; set; }
        public global::System.Boolean enabled { get; set; }
    }

    [HttpPost("dissolve-group")]
    public async Task<IActionResult> DissolveGroup([FromBody] StaffGroupDissolveGroupRequest request)
    {
        await service.DissolveGroupAsync(request.staffGroupId, request.reason);
        return NoContent();
    }

    public sealed class StaffGroupDissolveGroupRequest
    {
        public global::System.Int32 staffGroupId { get; set; }
        public global::System.String? reason { get; set; }
    }

}
