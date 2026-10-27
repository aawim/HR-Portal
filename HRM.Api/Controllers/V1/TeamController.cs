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

    [HttpGet("get-my-team-members")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamDto>>> GetMyTeamMembers([FromQuery] global::System.Int32 TeamId)
    {
        return Ok(await service.GetMyTeamMembersAsync(TeamId));
    }

}
