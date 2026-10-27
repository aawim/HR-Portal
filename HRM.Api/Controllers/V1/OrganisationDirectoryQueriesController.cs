// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-directory-queries")]
public sealed class OrganisationDirectoryQueriesController(global::HRM.Application.Contracts.Pages.IOrganisationDirectoryQueries service) : HrmApiController
{
    [HttpGet("get-approved-structure")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.OrgNodeDto>>> GetApprovedStructure([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetApprovedStructureAsync(organisationId, HttpContext.RequestAborted));
    }

    [HttpGet("get-teams")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Application.Contracts.Pages.OrganisationUnitSummary>>> GetTeams([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetTeamsAsync(organisationId, HttpContext.RequestAborted));
    }

    [HttpPost("query/get-details")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.OrganisationUnitDetails?>> GetDetails([FromBody] OrganisationDirectoryQueriesGetDetailsRequest request)
    {
        return Ok(await service.GetDetailsAsync(request.unit, HttpContext.RequestAborted));
    }

    public sealed class OrganisationDirectoryQueriesGetDetailsRequest
    {
        public global::HRM.Application.Contracts.Pages.OrganisationUnitKey unit { get; set; } = default!;
    }

}
