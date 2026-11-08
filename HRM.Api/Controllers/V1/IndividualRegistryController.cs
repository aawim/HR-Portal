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

}
