// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-permission")]
public sealed class OrganisationPermissionController(global::HRM.Services.Contracts.OrganisationPermissionServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Organisation.OrganisationPermissionData>> Load([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.LoadAsync(organisationId));
    }

    [HttpPost("save")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Save([FromBody] OrganisationPermissionSaveRequest request)
    {
        return Ok(await service.SaveAsync(request.organisationId, request.selectedIds, request.expectedIds, request.reason));
    }

    public sealed class OrganisationPermissionSaveRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Collections.Generic.IEnumerable<global::System.Int32> selectedIds { get; set; } = default!;
        public global::System.Collections.Generic.IEnumerable<global::System.Int32> expectedIds { get; set; } = default!;
        public global::System.String reason { get; set; } = default!;
    }

}
