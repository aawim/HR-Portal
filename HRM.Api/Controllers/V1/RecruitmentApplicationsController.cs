using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/applications")]
public sealed class RecruitmentApplicationsController(global::HRM.Application.Contracts.Recruitment.IRecruitmentApplicationService service):HrmApiController
{
    [HttpGet("vacancies")]
    public async Task<ActionResult<IReadOnlyList<global::HRM.Application.Contracts.Recruitment.PublishedRecruitmentVacancyRow>>> Vacancies(CancellationToken ct)=>Ok(await service.ListPublishedVacanciesAsync(ct));
    [HttpPost("applicants")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Register([FromBody] global::HRM.Application.Contracts.Recruitment.ApplicantRegistrationInput input,CancellationToken ct)=>Ok(await service.RegisterAsync(input,ct));
    [HttpPost]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] global::HRM.Application.Contracts.Recruitment.SubmitRecruitmentApplicationInput input,CancellationToken ct)=>Ok(await service.SubmitAsync(input,ct));
}
