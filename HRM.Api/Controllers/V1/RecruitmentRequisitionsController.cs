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

    [HttpGet("{id:int}/vacancy-draft-context")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Recruitment.VacancyDraftContextDto>> VacancyDraftContext(int id, CancellationToken ct) => Ok(await service.GetVacancyDraftContextAsync(id, ct));

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

    [HttpGet("policies")]
    public async Task<ActionResult<global::HRM.Application.Contracts.Recruitment.RecruitmentPolicyWorkspaceDto>> GetPolicies(CancellationToken ct) => Ok(await service.GetPoliciesAsync(ct));

    [HttpPost("policies")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreatePolicy([FromBody] global::HRM.Application.Contracts.Recruitment.CreateRecruitmentPolicyInput input,CancellationToken ct) => Ok(await service.CreatePolicyAsync(input,ct));

    [HttpPost("policies/{versionId:int}/verify")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> VerifyPolicy(int versionId,CancellationToken ct) => Ok(await service.VerifyApprovePolicyAsync(versionId,ct));

    [HttpPost("policies/assign")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AssignPolicy([FromBody] global::HRM.Application.Contracts.Recruitment.AssignRecruitmentPolicyInput input,CancellationToken ct) => Ok(await service.AssignPolicyAsync(input,ct));

    [HttpPost("policies/assignments/{assignmentId:int}/toggle")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> TogglePolicyAssignment(int assignmentId,CancellationToken ct) => Ok(await service.TogglePolicyAssignmentAsync(assignmentId,ct));
}
