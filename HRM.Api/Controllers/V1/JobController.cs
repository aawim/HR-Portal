// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/job")]
public sealed class JobController(global::HRM.Services.Interfaces.IJobService service) : HrmApiController
{
    [HttpGet("get-active-job")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.ActiveJobDto?>> GetActiveJob([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetActiveJobAsync(individualId, HttpContext.RequestAborted));
    }

    [HttpGet("get-my-active-job")]
    public async Task<ActionResult<global::HRM.DTOs.UserContext.ActiveJobDto?>> GetMyActiveJob()
    {
        return Ok(await service.GetMyActiveJobAsync());
    }

    [HttpGet("get-my-job-history")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.JobDto>>> GetMyJobHistory()
    {
        return Ok(await service.GetMyJobHistoryAsync());
    }

    [HttpGet("get-organisation-id-by-individual")]
    public async Task<ActionResult<global::System.Int32?>> GetOrganisationIdByIndividual([FromQuery] global::System.Int32 individualId)
    {
        return Ok(await service.GetOrganisationIdByIndividualAsync(individualId));
    }

    [HttpGet("get-individual-id-by-job-id")]
    public async Task<ActionResult<global::System.Int32>> GetIndividualIdByJobId([FromQuery] global::System.Int32 jobId)
    {
        return Ok(await service.GetIndividualIdByJobIdAsync(jobId));
    }

}
