// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-emergency-contact")]
public sealed class StaffEmergencyContactController(global::HRM.Services.Contracts.StaffEmergencyContactServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-contacts")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Staff.StaffEmergencyContactDto>>> GetContacts([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetContactsAsync(individualId));
    }

    [HttpPost("save-contact")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveContact([FromBody] StaffEmergencyContactSaveContactRequest request)
    {
        return Ok(await service.SaveContactAsync(request.input));
    }

    public sealed class StaffEmergencyContactSaveContactRequest
    {
        public global::HRM.DTOs.Staff.StaffEmergencyContactInputDto input { get; set; } = default!;
    }

    [HttpPost("delete-contact")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteContact([FromBody] StaffEmergencyContactDeleteContactRequest request)
    {
        return Ok(await service.DeleteContactAsync(request.id));
    }

    public sealed class StaffEmergencyContactDeleteContactRequest
    {
        public global::System.Int32 id { get; set; }
    }

    [HttpPost("set-primary")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SetPrimary([FromBody] StaffEmergencyContactSetPrimaryRequest request)
    {
        return Ok(await service.SetPrimaryAsync(request.id, request.individualId));
    }

    public sealed class StaffEmergencyContactSetPrimaryRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 individualId { get; set; }
    }

}
