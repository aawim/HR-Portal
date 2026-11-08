// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/former-staff")]
public sealed class FormerStaffController(global::HRM.Services.Contracts.FormerStaffServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Employee.View", "Staff.Details")]
    [HttpPost("query/search")]
    public async Task<ActionResult<global::HRM.Services.FormerStaffData>> Search([FromBody] FormerStaffSearchRequest request)
    {
        return Ok(await service.SearchAsync(request.search, HttpContext.RequestAborted, request.filters));
    }

    public sealed class FormerStaffSearchRequest
    {
        public global::System.String? search { get; set; } = null;
        public global::HRM.Services.FormerStaffFilters? filters { get; set; } = null;
    }

    [ApiPermission("Employee.View", "Staff.Details")]
    [HttpGet("detail")]
    public async Task<ActionResult<global::HRM.DTOs.Contracts.ExportReportDto>> Detail([FromQuery] global::System.Int32 person)
    {
        return Ok(await service.DetailAsync(person, HttpContext.RequestAborted));
    }

}
