// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/individual-registry")]
public sealed class IndividualRegistryController(global::HRM.Services.Interfaces.IIndividualRegistryService service) : HrmApiController
{
    [ApiPermission("Individual.View", "Employee.Create")]
    [HttpGet("search")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Registry.RegistryIndividualDto>>> Search([FromQuery] global::System.String searchTerm, [FromQuery] global::System.String searchType)
    {
        return Ok(await service.SearchAsync(searchTerm, searchType, HttpContext.RequestAborted));
    }

    [ApiPermission("Individual.View", "Employee.Create")]
    [HttpGet("get-photo")]
    public async Task<ActionResult<global::System.String?>> GetPhoto([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetPhotoAsync(individualId, HttpContext.RequestAborted));
    }

    [ApiPermission("Individual.View", "Employee.Create", "Individual.Edit", "Admin")]
    [HttpPost("update-photo")]
    public async Task<ActionResult> UpdatePhoto([FromBody] IndividualRegistryUpdatePhotoRequest request)
    {
        await service.UpdatePhotoAsync(request.individualId, request.imageData, HttpContext.RequestAborted);
        return Ok();
    }

    public sealed class IndividualRegistryUpdatePhotoRequest
    {
        public global::System.Int32 individualId { get; set; } = default!;
        public global::System.Byte[]? imageData { get; set; }
    }
}
