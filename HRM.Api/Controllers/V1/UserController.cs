// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/user")]
public sealed class UserController(global::HRM.Services.Contracts.UserServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-by-id")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserProfileDto?>> GetById([FromQuery] global::System.Int32 userId)
    {
        return Ok(await service.GetByIdAsync(userId));
    }

    [HttpGet("get-by-username")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserProfileDto?>> GetByUsername([FromQuery] global::System.String username)
    {
        return Ok(await service.GetByUsernameAsync(username));
    }

    [HttpGet("get-by-business-entity")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserProfileDto?>> GetByBusinessEntity([FromQuery] global::System.Int32 businessEntityId)
    {
        return Ok(await service.GetByBusinessEntityAsync(businessEntityId));
    }

    [HttpGet("get-my-profile")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserProfileDto?>> GetMyProfile()
    {
        return Ok(await service.GetMyProfileAsync());
    }

    [HttpPost("update-user")]
    public async Task<ActionResult<global::System.Boolean>> UpdateUser([FromBody] UserUpdateUserRequest request)
    {
        return Ok(await service.UpdateUserAsync(request.profile));
    }

    public sealed class UserUpdateUserRequest
    {
        public global::HRM.DTOs.UserContext.UserProfileDto profile { get; set; } = default!;
    }

}
