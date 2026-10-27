// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-settings")]
public sealed class OrganisationSettingsController(global::HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Organisation.OrganisationSettingsData>> Load()
    {
        return Ok(await service.LoadAsync());
    }

    [HttpGet("get-links")]
    public async Task<ActionResult<global::HRM.DTOs.Organisation.OrganisationLinksDto>> GetLinks([FromQuery] global::System.Int32 id)
    {
        return Ok(await service.GetLinksAsync(id));
    }

    [HttpPost("save-details")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveDetails([FromBody] OrganisationSettingsSaveDetailsRequest request)
    {
        return Ok(await service.SaveDetailsAsync(request.model));
    }

    public sealed class OrganisationSettingsSaveDetailsRequest
    {
        public global::HRM.DTOs.Organisation.OrganisationSettingsDto model { get; set; } = default!;
    }

    [HttpGet("search-addresses")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Organisation.OrganisationSettingsOption>>> SearchAddresses([FromQuery] global::System.Int32 organisationId, [FromQuery] global::System.String query)
    {
        return Ok(await service.SearchAddressesAsync(organisationId, query));
    }

    [HttpPost("save-address")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveAddress([FromBody] OrganisationSettingsSaveAddressRequest request)
    {
        return Ok(await service.SaveAddressAsync(request.id, request.model));
    }

    public sealed class OrganisationSettingsSaveAddressRequest
    {
        public global::System.Int32 id { get; set; }
        public global::HRM.DTOs.Organisation.OrganisationAddressDto model { get; set; } = default!;
    }

    [HttpGet("get-address")]
    public async Task<ActionResult<global::HRM.DTOs.Organisation.OrganisationAddressDto>> GetAddress([FromQuery] global::System.Int32 id, [FromQuery] global::System.Int32 linkId)
    {
        return Ok(await service.GetAddressAsync(id, linkId));
    }

    [HttpPost("delete-address")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteAddress([FromBody] OrganisationSettingsDeleteAddressRequest request)
    {
        return Ok(await service.DeleteAddressAsync(request.id, request.linkId));
    }

    public sealed class OrganisationSettingsDeleteAddressRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 linkId { get; set; }
    }

    [HttpPost("save-contact")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveContact([FromBody] OrganisationSettingsSaveContactRequest request)
    {
        return Ok(await service.SaveContactAsync(request.id, request.model));
    }

    public sealed class OrganisationSettingsSaveContactRequest
    {
        public global::System.Int32 id { get; set; }
        public global::HRM.DTOs.Organisation.OrganisationContactDto model { get; set; } = default!;
    }

    [HttpGet("get-staff")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Organisation.OrganisationStaffRow>>> GetStaff([FromQuery] global::System.Int32 id)
    {
        return Ok(await service.GetStaffAsync(id));
    }

    [HttpPost("delete-contact")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteContact([FromBody] OrganisationSettingsDeleteContactRequest request)
    {
        return Ok(await service.DeleteContactAsync(request.id, request.contactId));
    }

    public sealed class OrganisationSettingsDeleteContactRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 contactId { get; set; }
    }

    [HttpPost("delete-organisation")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteOrganisation([FromBody] OrganisationSettingsDeleteOrganisationRequest request)
    {
        return Ok(await service.DeleteOrganisationAsync(request.id));
    }

    public sealed class OrganisationSettingsDeleteOrganisationRequest
    {
        public global::System.Int32 id { get; set; }
    }

    [HttpGet("get-team-organisations")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Organisation.OrganisationSettingsOption>>> GetTeamOrganisations()
    {
        return Ok(await service.GetTeamOrganisationsAsync());
    }

    [HttpGet("get-team-settings")]
    public async Task<ActionResult<global::HRM.DTOs.Organisation.OrganisationTeamSettingsData>> GetTeamSettings([FromQuery] global::System.Int32 organisationId)
    {
        return Ok(await service.GetTeamSettingsAsync(organisationId));
    }

    [HttpPost("set-team-supervisor")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SetTeamSupervisor([FromBody] OrganisationSettingsSetTeamSupervisorRequest request)
    {
        return Ok(await service.SetTeamSupervisorAsync(request.organisationId, request.kind, request.teamId, request.staffId, request.assign));
    }

    public sealed class OrganisationSettingsSetTeamSupervisorRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.String kind { get; set; } = default!;
        public global::System.Int32 teamId { get; set; }
        public global::System.Int32 staffId { get; set; }
        public global::System.Boolean assign { get; set; }
    }

}
