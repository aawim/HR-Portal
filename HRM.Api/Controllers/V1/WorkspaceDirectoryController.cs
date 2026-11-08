// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/workspace-directory")]
public sealed class WorkspaceDirectoryController(global::HRM.Services.Interfaces.IWorkspaceDirectoryService service) : HrmApiController
{
    [ApiPermission("Employee.View")]
    [HttpGet("get-staff-identity")]
    public async Task<ActionResult<global::HRM.DTOs.StaffWorkspaceIdentity?>> GetStaffIdentity([FromQuery] global::System.Int32 staffId)
    {
        return Ok(await service.GetStaffIdentityAsync(staffId, HttpContext.RequestAborted));
    }

    [ApiPermission("Employee.View")]
    [HttpPost("query/search")]
    public async Task<ActionResult<global::HRM.DTOs.WorkspaceSearchPage>> Search([FromBody] WorkspaceDirectorySearchRequest request)
    {
        return Ok(await service.SearchAsync(request.filter, HttpContext.RequestAborted));
    }

    public sealed class WorkspaceDirectorySearchRequest
    {
        public global::HRM.DTOs.WorkspaceSearchFilter filter { get; set; } = default!;
    }

}
