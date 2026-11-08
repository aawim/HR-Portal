// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/user-app-data")]
public sealed class UserAppDataController(global::HRM.Services.Interfaces.IUserAppDataService service) : HrmApiController
{
    [HttpGet("get-job-leave-types")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>>> GetJobLeaveTypes()
    {
        return Ok(await service.GetJobLeaveTypesAsync());
    }

}
