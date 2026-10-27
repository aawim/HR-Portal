// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[AllowMissingEmployee]
[Route("api/v1/current-employee-session")]
public sealed class CurrentEmployeeSessionController(global::HRM.Application.Contracts.Pages.ICurrentEmployeeSession service) : HrmApiController
{
    [HttpGet("get")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserContextDto?>> Get()
    {
        return Ok(await service.GetAsync(HttpContext.RequestAborted));
    }

}
