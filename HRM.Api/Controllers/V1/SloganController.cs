// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/slogan")]
public sealed class SloganController(global::HRM.Services.Contracts.SloganServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Services.Contracts.SloganServiceContracts.SloganWorkspaceDto>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpPost("save-slogan")]
    public async Task<ActionResult<global::HRM.Services.Contracts.SloganServiceContracts.SloganDto>> SaveSlogan([FromBody] SloganSaveSloganRequest request)
    {
        return Ok(await service.SaveSloganAsync(request.request));
    }

    public sealed class SloganSaveSloganRequest
    {
        public global::HRM.Services.Contracts.SloganServiceContracts.SloganSaveRequest request { get; set; } = default!;
    }

    [HttpPost("delete-slogan")]
    public async Task<IActionResult> DeleteSlogan([FromBody] SloganDeleteSloganRequest request)
    {
        await service.DeleteSloganAsync(request.sloganId);
        return NoContent();
    }

    public sealed class SloganDeleteSloganRequest
    {
        public global::System.Int32 sloganId { get; set; }
    }

    [HttpPost("set-default-slogan")]
    public async Task<IActionResult> SetDefaultSlogan([FromBody] SloganSetDefaultSloganRequest request)
    {
        await service.SetDefaultSloganAsync(request.sloganId);
        return NoContent();
    }

    public sealed class SloganSetDefaultSloganRequest
    {
        public global::System.Int32? sloganId { get; set; }
    }

    [HttpPost("save-document-assignment")]
    public async Task<IActionResult> SaveDocumentAssignment([FromBody] SloganSaveDocumentAssignmentRequest request)
    {
        await service.SaveDocumentAssignmentAsync(request.documentType, request.sloganId);
        return NoContent();
    }

    public sealed class SloganSaveDocumentAssignmentRequest
    {
        public global::System.String documentType { get; set; } = default!;
        public global::System.Int32? sloganId { get; set; }
    }

    [HttpGet("resolve-for-document")]
    public async Task<ActionResult<global::HRM.Services.Contracts.SloganServiceContracts.SloganDto?>> ResolveForDocument([FromQuery] global::System.String documentType)
    {
        return Ok(await service.ResolveForDocumentAsync(documentType));
    }

}
