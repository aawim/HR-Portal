// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/position-overview")]
public sealed class PositionOverviewController(global::HRM.Services.Contracts.PositionOverviewServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Position.Details", "Staff.Positions.Manage")]
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.PositionOverviewData>> Load()
    {
        return Ok(await service.LoadAsync());
    }

}
