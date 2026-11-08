// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/employee-addresses")]
public sealed class EmployeeAddressesController(global::HRM.Application.Contracts.Pages.IEmployeeAddresses service) : HrmApiController
{
    [HttpGet("get-directory")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.AddressDirectory>> GetDirectory([FromQuery] global::System.Int32? atollId = null)
    {
        return Ok(await service.GetDirectoryAsync(atollId, HttpContext.RequestAborted));
    }

    [HttpGet("get-leave-addresses")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.AddressCardDto>>> GetLeaveAddresses([FromQuery] global::System.Int32? applicantJobId = null)
    {
        return Ok(await service.GetLeaveAddressesAsync(applicantJobId));
    }

    [HttpPost("create-address")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreateAddress([FromBody] EmployeeAddressesCreateAddressRequest request)
    {
        return Ok(await service.CreateAddressAsync(request.application, request.applicantJobId));
    }

    public sealed class EmployeeAddressesCreateAddressRequest
    {
        public global::HRM.DTOs.AddressCreateDto application { get; set; } = default!;
        public global::System.Int32? applicantJobId { get; set; } = null;
    }

}
