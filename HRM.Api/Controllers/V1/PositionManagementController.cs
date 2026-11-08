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

    [HttpGet("load-pay-frameworks")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.PayFrameworkData>> LoadPayFrameworks()
    {
        return Ok(await service.LoadPayFrameworksAsync());
    }

    [HttpPost("save-framework")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveFramework([FromBody] PositionManagementSaveFrameworkRequest request)
    {
        return Ok(await service.SaveFrameworkAsync(request.input));
    }

    public sealed class PositionManagementSaveFrameworkRequest
    {
        public global::HRM.Application.Contracts.Pages.PayFrameworkInput input { get; set; } = default!;
    }

    [HttpPost("save-job-matrix")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveJobMatrix([FromBody] PositionManagementSaveJobMatrixRequest request)
    {
        return Ok(await service.SaveJobMatrixAsync(request.input));
    }

    public sealed class PositionManagementSaveJobMatrixRequest
    {
        public global::HRM.Application.Contracts.Pages.JobMatrixInput input { get; set; } = default!;
    }

    [HttpPost("create-position-matrix")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreatePositionMatrix([FromBody] PositionManagementCreatePositionMatrixRequest request)
    {
        return Ok(await service.CreatePositionMatrixAsync(request.input));
    }

    public sealed class PositionManagementCreatePositionMatrixRequest
    {
        public global::HRM.Application.Contracts.Pages.PositionMatrixInput input { get; set; } = default!;
    }

}
