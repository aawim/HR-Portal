// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-attendance-queries")]
public sealed class OrganisationAttendanceQueriesController(global::HRM.Application.Contracts.Pages.IOrganisationAttendanceQueries service) : HrmApiController
{
    [HttpPost("query/get-daily")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Pages.OrganisationAttendancePage>> GetDaily([FromBody] OrganisationAttendanceQueriesGetDailyRequest request)
    {
        return Ok(await service.GetDailyAsync(request.filter, HttpContext.RequestAborted));
    }

    public sealed class OrganisationAttendanceQueriesGetDailyRequest
    {
        public global::HRM.Application.Contracts.Pages.OrganisationAttendanceFilter filter { get; set; } = default!;
    }

}
