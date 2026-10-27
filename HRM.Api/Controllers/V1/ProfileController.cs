// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/profile")]
public sealed class ProfileController(global::HRM.Services.Interfaces.Profile.IProfileService service) : HrmApiController
{
    [HttpGet("get-profile")]
    public async Task<ActionResult<global::HRM.DTOs.Profile.ProfileOverviewDto?>> GetProfile([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetProfileAsync(individualId, HttpContext.RequestAborted));
    }

}
