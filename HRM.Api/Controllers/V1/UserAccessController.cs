// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/user-access")]
public sealed class UserAccessController(global::HRM.Services.Interfaces.IUserAccessService service) : HrmApiController
{
    [HttpGet("get-context")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserContextDto?>> GetContext()
    {
        return Ok(await service.GetContextAsync());
    }

    [HttpGet("require-context")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserContextDto>> RequireContext()
    {
        return Ok(await service.RequireContextAsync());
    }

    [HttpGet("has-permission")]
    public async Task<ActionResult<global::System.Boolean>> HasPermission([FromQuery] global::System.String permissionKey)
    {
        return Ok(await service.HasPermissionAsync(permissionKey));
    }

    [HttpGet("has-user-group")]
    public async Task<ActionResult<global::System.Boolean>> HasUserGroup([FromQuery] global::System.String userGroupName)
    {
        return Ok(await service.HasUserGroupAsync(userGroupName));
    }

    [HttpPost("query/has-any-user-group")]
    public async Task<ActionResult<global::System.Boolean>> HasAnyUserGroup([FromBody] UserAccessHasAnyUserGroupRequest request)
    {
        return Ok(await service.HasAnyUserGroupAsync(request.groups));
    }

    public sealed class UserAccessHasAnyUserGroupRequest
    {
        public global::System.String[] groups { get; set; } = default!;
    }

    [HttpPost("query/has-all-user-groups")]
    public async Task<ActionResult<global::System.Boolean>> HasAllUserGroups([FromBody] UserAccessHasAllUserGroupsRequest request)
    {
        return Ok(await service.HasAllUserGroupsAsync(request.groups));
    }

    public sealed class UserAccessHasAllUserGroupsRequest
    {
        public global::System.String[] groups { get; set; } = default!;
    }

    [HttpGet("has-organisation-access")]
    public async Task<ActionResult<global::System.Boolean>> HasOrganisationAccess([FromQuery] global::System.Int32 organisationId)
    {
        return Ok(await service.HasOrganisationAccessAsync(organisationId));
    }

    [HttpGet("is-eligible")]
    public async Task<ActionResult<global::System.Boolean>> IsEligible()
    {
        return Ok(await service.IsEligibleAsync());
    }

    [HttpGet("has-active-job")]
    public async Task<ActionResult<global::System.Boolean>> HasActiveJob()
    {
        return Ok(await service.HasActiveJobAsync());
    }

    [HttpGet("validate-user")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserValidationResultDto>> ValidateUser()
    {
        return Ok(await service.ValidateUserAsync());
    }

    [HttpGet("get-current-organisation-id")]
    public async Task<ActionResult<global::System.Int32>> GetCurrentOrganisationId()
    {
        return Ok(await service.GetCurrentOrganisationIdAsync());
    }

    [HttpGet("get-current-job-id")]
    public async Task<ActionResult<global::System.Int32>> GetCurrentJobId()
    {
        return Ok(await service.GetCurrentJobIdAsync());
    }

    [HttpGet("get-current-individual-id")]
    public async Task<ActionResult<global::System.Int32>> GetCurrentIndividualId()
    {
        return Ok(await service.GetCurrentIndividualIdAsync());
    }

}
