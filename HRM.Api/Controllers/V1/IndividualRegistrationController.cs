// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/individual-registration")]
public sealed class IndividualRegistrationController(global::HRM.Services.Contracts.IndividualRegistrationServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Individual.Create")]
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.Services.RegistrationOptions>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [ApiPermission("Individual.Create")]
    [HttpPost("register")]
    public async Task<ActionResult<global::System.Int32>> Register([FromBody] IndividualRegistrationRegisterRequest request)
    {
        return Ok(await service.RegisterAsync(request.model));
    }

    public sealed class IndividualRegistrationRegisterRequest
    {
        public global::HRM.Services.IndividualRegistrationModel model { get; set; } = default!;
    }

}
