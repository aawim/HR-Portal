using Intranet.Application.Services;
using Intranet.Contracts.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController]
[Route("api/intranet/integration")]
[Authorize]
public class IntegrationController : ControllerBase
{
    private readonly INewJoinerService _svc;
    public IntegrationController(INewJoinerService svc) => _svc = svc;
    [HttpPost("employee-joined")]
    public async Task<IActionResult> EmployeeJoined([FromBody] EmployeeJoinedIntegrationEvent evt, CancellationToken ct = default)
        => Ok(await _svc.CreateDraftFromEventAsync(evt, ct));
}