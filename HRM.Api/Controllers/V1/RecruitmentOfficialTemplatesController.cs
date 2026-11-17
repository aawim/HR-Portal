using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/official-templates")]
public sealed class RecruitmentOfficialTemplatesController(global::HRM.Application.Contracts.Recruitment.IRecruitmentOfficialTemplateService service) : HrmApiController
{
    [HttpGet("{policyVersionId:int}")] public async Task<ActionResult<IReadOnlyList<global::HRM.Application.Contracts.Recruitment.RecruitmentOfficialTemplateRow>>> List(int policyVersionId, CancellationToken ct) => Ok(await service.ListAsync(policyVersionId, ct));
    [HttpPost] public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Register([FromBody] global::HRM.Application.Contracts.Recruitment.RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct) => Ok(await service.RegisterAsync(input, ct));
}
