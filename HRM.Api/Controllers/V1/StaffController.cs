// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/staff")]
public sealed class StaffController(global::HRM.Services.Interfaces.IStaffService service) : HrmApiController
{
    [HttpGet("search-staff")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Staff.IndividualResultDto>>> SearchStaff([FromQuery] global::System.String searchTerm, [FromQuery] global::System.String searchType, [FromQuery] global::System.Int32? organisationId)
    {
        return Ok(await service.SearchStaffAsync(searchTerm, searchType, organisationId));
    }

}
