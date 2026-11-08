// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-resource-settings")]
public sealed class OrganisationResourceSettingsController(global::HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Organisation.OrganisationResourceData>> Load([FromQuery] global::HRM.DTOs.Organisation.OrganisationResourceKind kind, [FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.LoadAsync(kind, organisationId));
    }

    [HttpPost("save-type")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveType([FromBody] OrganisationResourceSettingsSaveTypeRequest request)
    {
        return Ok(await service.SaveTypeAsync(request.kind, request.id, request.name, request.expectedName));
    }

    public sealed class OrganisationResourceSettingsSaveTypeRequest
    {
        public global::HRM.DTOs.Organisation.OrganisationResourceKind kind { get; set; }
        public global::System.Int32 id { get; set; }
        public global::System.String name { get; set; } = default!;
        public global::System.String? expectedName { get; set; }
    }

    [HttpPost("link-calendar")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> LinkCalendar([FromBody] OrganisationResourceSettingsLinkCalendarRequest request)
    {
        return Ok(await service.LinkCalendarAsync(request.organisationId, request.calendarId, request.from, request.to));
    }

    public sealed class OrganisationResourceSettingsLinkCalendarRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Int32 calendarId { get; set; }
        public global::System.DateTime from { get; set; }
        public global::System.DateTime? to { get; set; }
    }

    [HttpGet("load-document-types")]
    public async Task<ActionResult<global::HRM.Services.OrganisationDocumentTypesData>> LoadDocumentTypes()
    {
        return Ok(await service.LoadDocumentTypesAsync());
    }

    [HttpPost("save-document-type")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveDocumentType([FromBody] OrganisationResourceSettingsSaveDocumentTypeRequest request)
    {
        return Ok(await service.SaveDocumentTypeAsync(request.model, request.delete));
    }

    public sealed class OrganisationResourceSettingsSaveDocumentTypeRequest
    {
        public global::HRM.Services.OrganisationDocumentType model { get; set; } = default!;
        public global::System.Boolean delete { get; set; } = false;
    }

}
