// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation")]
public sealed class OrganisationController(global::HRM.Services.IOrganisationService service) : HrmApiController
{
    [HttpGet("search-organisations")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Organisation.OrganisationResultDto>>> SearchOrganisations([FromQuery] global::System.String searchTerm, [FromQuery] global::System.String searchType)
    {
        return Ok(await service.SearchOrganisationsAsync(searchTerm, searchType));
    }

    [HttpGet("get-organisation-details")]
    public async Task<ActionResult<global::HRM.DTOs.Organisation.OrganisationResultDto?>> GetOrganisationDetails([FromQuery] global::System.Int32 organisationId)
    {
        return Ok(await service.GetOrganisationDetailsAsync(organisationId));
    }

    [HttpGet("get-organisation-tree")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.OrgNodeDto>>> GetOrganisationTree([FromQuery] global::System.Int32 organisationId)
    {
        return Ok(await service.GetOrganisationTreeAsync(organisationId));
    }

    [HttpGet("get-organisation-types")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.DropdownItem>>> GetOrganisationTypes()
    {
        return Ok(await service.GetOrganisationTypesAsync());
    }

    [HttpGet("get-countries")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.DropdownItem>>> GetCountries()
    {
        return Ok(await service.GetCountriesAsync());
    }

    [HttpGet("get-all-organisations")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.DropdownItem>>> GetAllOrganisations()
    {
        return Ok(await service.GetAllOrganisationsAsync());
    }

    [HttpGet("get-organisation")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserOrganisationDto?>> GetOrganisation([FromQuery] global::System.Int32 businessEntityId)
    {
        return Ok(await service.GetOrganisationAsync(businessEntityId));
    }

    [HttpGet("get-user-organisation")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.UserOrganisationDto?>> GetUserOrganisation([FromQuery] global::System.Int32 userId)
    {
        return Ok(await service.GetUserOrganisationAsync(userId));
    }

}
