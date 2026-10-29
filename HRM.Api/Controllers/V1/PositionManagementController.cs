// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/position-management")]
public sealed class PositionManagementController(global::HRM.Application.Contracts.Pages.IPositionManagement service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.PositionManagementData>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpPost("save")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Save([FromBody] PositionManagementSaveRequest request)
    {
        return Ok(await service.SaveAsync(request.input));
    }

    public sealed class PositionManagementSaveRequest
    {
        public global::HRM.Application.Contracts.Pages.PositionInput input { get; set; } = default!;
    }

}
