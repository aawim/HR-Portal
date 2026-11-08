// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/individual")]
public sealed class IndividualController(global::HRM.Services.Interfaces.IIndividual service) : HrmApiController
{
    [HttpGet("get-individual-id")]
    public async Task<ActionResult<global::System.Int32>> GetIndividualID()
    {
        return Ok(await service.GetIndividualID());
    }

}
