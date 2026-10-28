// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/team")]
public sealed class TeamController(global::HRM.Services.Interfaces.ITeam service) : HrmApiController
{
    [HttpGet("get-my-teams")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamDto>>> GetMyTeams()
    {
        return Ok(await service.GetMyTeamsAsync());
    }

    [HttpPost("create")]
    public async Task<ActionResult<global::System.Int32>> Create([FromBody] TeamCreateRequest request)
    {
        return Ok(await service.CreateAsync(request.organisationId, request.name, request.nameDhivehi, request.startDate, HttpContext.RequestAborted));
    }

    public sealed class TeamCreateRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.String name { get; set; } = default!;
        public global::System.String? nameDhivehi { get; set; }
        public global::System.DateTime startDate { get; set; }
    }

    [HttpGet("get-my-team-members")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamDto>>> GetMyTeamMembers([FromQuery] global::System.Int32 TeamId)
    {
        return Ok(await service.GetMyTeamMembersAsync(TeamId));
    }

}
