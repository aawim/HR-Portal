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

    [HttpPost("add-staff")]
    public async Task<ActionResult<global::System.Int32>> AddStaff([FromBody] TeamAddStaffRequest request)
    {
        return Ok(await service.AddStaffAsync(request.teamId, request.staffId, request.startDate, request.isSupervisor, HttpContext.RequestAborted));
    }

    public sealed class TeamAddStaffRequest
    {
        public global::System.Int32 teamId { get; set; }
        public global::System.Int32 staffId { get; set; }
        public global::System.DateTime startDate { get; set; }
        public global::System.Boolean isSupervisor { get; set; } = false;
    }

    [HttpPost("remove-staff")]
    public async Task<ActionResult<global::System.Boolean>> RemoveStaff([FromBody] TeamRemoveStaffRequest request)
    {
        return Ok(await service.RemoveStaffAsync(request.teamId, request.staffId, HttpContext.RequestAborted));
    }

    public sealed class TeamRemoveStaffRequest
    {
        public global::System.Int32 teamId { get; set; }
        public global::System.Int32 staffId { get; set; }
    }

}
