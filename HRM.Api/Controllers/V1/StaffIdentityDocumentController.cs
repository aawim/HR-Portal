// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff-identity-document")]
public sealed class StaffIdentityDocumentController(global::HRM.Services.Contracts.StaffIdentityDocumentServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-identity-documents")]
    public async Task<ActionResult<global::HRM.DTOs.Staff.StaffIdentityDocumentDto>> GetIdentityDocuments([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetIdentityDocumentsAsync(individualId));
    }

    [HttpPost("update-identity-documents")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UpdateIdentityDocuments([FromBody] StaffIdentityDocumentUpdateIdentityDocumentsRequest request)
    {
        return Ok(await service.UpdateIdentityDocumentsAsync(request.input));
    }

    public sealed class StaffIdentityDocumentUpdateIdentityDocumentsRequest
    {
        public global::HRM.DTOs.Staff.StaffIdentityDocumentUpdateInput input { get; set; } = default!;
    }

}
