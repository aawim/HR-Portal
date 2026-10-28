// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[AllowMissingEmployee]
[Route("api/v1/user-context")]
public sealed class UserContextController(global::HRM.Services.Interfaces.IUserContext service) : HrmApiController
{
    [HttpGet("get-user-id")]
    public async Task<ActionResult<global::System.Int32>> GetUserId()
    {
        return Ok(await service.GetUserIdAsync());
    }

    [HttpGet("get-individual-id")]
    public async Task<ActionResult<global::System.Int32>> GetIndividualId()
    {
        return Ok(await service.GetIndividualIdAsync());
    }

    [HttpGet("get-job-id")]
    public async Task<ActionResult<global::System.Int32>> GetJobId()
    {
        return Ok(await service.GetJobIdAsync());
    }

    [HttpGet("get-username")]
    public async Task<ActionResult<global::System.String?>> GetUsername()
    {
        return Ok(await service.GetUsernameAsync());
    }

    [HttpGet("get-session")]
    public async Task<ActionResult<global::HRM.DTOs.UserSessionDto?>> GetSession()
    {
        return Ok(await service.GetSessionAsync());
    }

}
