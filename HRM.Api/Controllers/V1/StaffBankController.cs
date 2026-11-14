// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-bank")]
public sealed class StaffBankController(global::HRM.Services.Contracts.StaffBankServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-accounts")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Staff.StaffBankAccountDto>>> GetAccounts([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetAccountsAsync(individualId));
    }

    [HttpPost("save-account")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveAccount([FromBody] StaffBankSaveAccountRequest request)
    {
        return Ok(await service.SaveAccountAsync(request.input));
    }

    public sealed class StaffBankSaveAccountRequest
    {
        public global::HRM.DTOs.Staff.StaffBankAccountInputDto input { get; set; } = default!;
    }

    [HttpPost("delete-account")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteAccount([FromBody] StaffBankDeleteAccountRequest request)
    {
        return Ok(await service.DeleteAccountAsync(request.id));
    }

    public sealed class StaffBankDeleteAccountRequest
    {
        public global::System.Int32 id { get; set; }
    }

    [HttpPost("set-primary")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SetPrimary([FromBody] StaffBankSetPrimaryRequest request)
    {
        return Ok(await service.SetPrimaryAsync(request.id, request.individualId));
    }

    public sealed class StaffBankSetPrimaryRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 individualId { get; set; }
    }

}
