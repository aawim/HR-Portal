using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/recruitment/results")]
public sealed class RecruitmentResultsController(global::HRM.Application.Contracts.Recruitment.IRecruitmentResultService service) : HrmApiController
{
    [HttpPost("publish")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Publish([FromBody] global::HRM.Application.Contracts.Recruitment.PublishRecruitmentResultsInput input, CancellationToken ct) => Ok(await service.PublishAsync(input, ct));

    [HttpPost("objections")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitObjection([FromBody] global::HRM.Application.Contracts.Recruitment.SubmitRecruitmentObjectionInput input, CancellationToken ct) => Ok(await service.SubmitObjectionAsync(input, ct));

    [HttpPost("objections/resolve")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ResolveObjection([FromBody] global::HRM.Application.Contracts.Recruitment.ResolveRecruitmentObjectionInput input, CancellationToken ct) => Ok(await service.ResolveObjectionAsync(input, ct));

    [HttpPost("{publicationId:int}/objection-closing")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UpdateObjectionClosing(int publicationId, [FromBody] global::HRM.Application.Contracts.Recruitment.UpdateObjectionClosingDateInput input, CancellationToken ct) => Ok(await service.UpdateObjectionClosingDateAsync(publicationId, input.ObjectionClosesAtUtc, ct));

    [HttpPost("{publicationId:int}/finalize")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> FinalizePublication(int publicationId, CancellationToken ct) => Ok(await service.FinalizeAsync(publicationId, ct));
}
