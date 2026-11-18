using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/recruitment/requisitions")]
public sealed class RecruitmentRequisitionsController(global::HRM.Application.Contracts.Recruitment.IRecruitmentRequisitionService service) : HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<global::HRM.Application.Contracts.Recruitment.RecruitmentRequisitionRow>>> List(CancellationToken ct) => Ok(await service.ListAsync(ct));

    [HttpGet("available-positions")]
    public async Task<ActionResult<IReadOnlyList<global::HRM.Application.Contracts.Recruitment.AvailablePositionMatrixOption>>> AvailablePositions(CancellationToken ct) => Ok(await service.ListAvailablePositionsAsync(ct));

    [HttpPost]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Create([FromBody] CreateRequest request,CancellationToken ct) => Ok(await service.CreateAsync(request.Input,ct));

    public sealed record CreateRequest(global::HRM.Application.Contracts.Recruitment.RecruitmentRequisitionInput Input);
    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Approve(int id,CancellationToken ct) => Ok(await service.ApproveAsync(id,ct));
    [HttpPost("vacancies")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreateVacancy([FromBody] VacancyRequest request,CancellationToken ct) => Ok(await service.CreateVacancyAsync(request.Input,ct));
    public sealed record VacancyRequest(global::HRM.Application.Contracts.Recruitment.RecruitmentVacancyInput Input);
    [HttpPost("vacancies/{id:int}/publish")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> PublishVacancy(int id,CancellationToken ct) => Ok(await service.PublishVacancyAsync(id,ct));
}
