// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/workspace-recent-search")]
public sealed class WorkspaceRecentSearchController(global::HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Employee.View")]
    [HttpGet("resolve")]
    public async Task<ActionResult<global::HRM.DTOs.WorkspaceSearchResult?>> Resolve([FromQuery] global::HRM.Enum.WorkspaceEntityType type, [FromQuery] global::System.Int32 id)
    {
        return Ok(await service.ResolveAsync(type, id));
    }

    [ApiPermission("Employee.View")]
    [HttpGet("get")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkspaceSearchResult>>> Get()
    {
        return Ok(await service.GetAsync());
    }

    [ApiPermission("Employee.View")]
    [HttpPost("remember")]
    public async Task<ActionResult<global::HRM.DTOs.WorkspaceSearchResult?>> Remember([FromBody] WorkspaceRecentSearchRememberRequest request)
    {
        return Ok(await service.RememberAsync(request.selected));
    }

    public sealed class WorkspaceRecentSearchRememberRequest
    {
        public global::HRM.DTOs.WorkspaceSearchResult selected { get; set; } = default!;
    }

}
