// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/groups-management")]
public sealed class GroupsManagementController(global::HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Services.OperationalGroupsData>> Load([FromQuery] global::System.Int32? selected = null)
    {
        return Ok(await service.LoadAsync(selected));
    }

    [HttpGet("require-group")]
    public async Task<ActionResult<global::System.Int32>> RequireGroup([FromQuery] global::System.Int32 id, [FromQuery] global::System.Boolean write = false)
    {
        return Ok(await service.RequireGroupAsync(id, write));
    }

    [HttpGet("can-manage-group")]
    public async Task<ActionResult<global::System.Boolean>> CanManageGroup([FromQuery] global::System.Int32 id)
    {
        return Ok(await service.CanManageGroupAsync(id));
    }

    [HttpPost("save-group")]
    public async Task<IActionResult> SaveGroup([FromBody] GroupsManagementSaveGroupRequest request)
    {
        await service.SaveGroupAsync(request.org, request.id, request.name, request.type, request.structure, request.expectedAudit);
        return NoContent();
    }

    public sealed class GroupsManagementSaveGroupRequest
    {
        public global::System.Int32 org { get; set; }
        public global::System.Int32 id { get; set; }
        public global::System.String name { get; set; } = default!;
        public global::System.Int32 type { get; set; }
        public global::System.Int32 structure { get; set; }
        public global::System.Int32 expectedAudit { get; set; }
    }

    [HttpPost("delete-group")]
    public async Task<IActionResult> DeleteGroup([FromBody] GroupsManagementDeleteGroupRequest request)
    {
        await service.DeleteGroupAsync(request.org, request.id, request.expectedAudit);
        return NoContent();
    }

    public sealed class GroupsManagementDeleteGroupRequest
    {
        public global::System.Int32 org { get; set; }
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedAudit { get; set; }
    }

    [HttpPost("save-type")]
    public async Task<IActionResult> SaveType([FromBody] GroupsManagementSaveTypeRequest request)
    {
        await service.SaveTypeAsync(request.org, request.id, request.name, request.originalName);
        return NoContent();
    }

    public sealed class GroupsManagementSaveTypeRequest
    {
        public global::System.Int32 org { get; set; }
        public global::System.Int32 id { get; set; }
        public global::System.String name { get; set; } = default!;
        public global::System.String? originalName { get; set; }
    }

    [HttpPost("delete-type")]
    public async Task<IActionResult> DeleteType([FromBody] GroupsManagementDeleteTypeRequest request)
    {
        await service.DeleteTypeAsync(request.org, request.id, request.originalName);
        return NoContent();
    }

    public sealed class GroupsManagementDeleteTypeRequest
    {
        public global::System.Int32 org { get; set; }
        public global::System.Int32 id { get; set; }
        public global::System.String originalName { get; set; } = default!;
    }

    [HttpPost("assign-leave-set")]
    public async Task<IActionResult> AssignLeaveSet([FromBody] GroupsManagementAssignLeaveSetRequest request)
    {
        await service.AssignLeaveSetAsync(request.groupId, request.leaveSetId);
        return NoContent();
    }

    public sealed class GroupsManagementAssignLeaveSetRequest
    {
        public global::System.Int32 groupId { get; set; }
        public global::System.Int32 leaveSetId { get; set; }
    }

}
