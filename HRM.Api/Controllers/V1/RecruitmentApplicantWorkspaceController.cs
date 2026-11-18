using System.Security.Claims;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/my-workspace")]
[AllowMissingEmployee]
public sealed class RecruitmentApplicantWorkspaceController(IDbContextFactory<HrmTeContext> factory,global::HRM.Application.Contracts.Recruitment.IRecruitmentOfferService offers,IRecruitmentApplicationService applicationsService):HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<MyRecruitmentWorkspace>> Load(CancellationToken ct)
    {
        var identity=User.FindFirstValue("idnumber")?.Trim();
        if(string.IsNullOrWhiteSpace(identity))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        var applications=await(from a in db.RecruitmentApplications join person in db.RecruitmentApplicants on a.RecruitmentApplicantId equals person.Id join v in db.RecruitmentVacancies on a.RecruitmentVacancyId equals v.Id where person.EfaasIdNumber==identity orderby a.SubmittedAtUtc descending select new MyRecruitmentApplicationRow(a.Reference,v.Reference,a.Status,a.SubmittedAtUtc)).ToListAsync(ct);
        var mine=await(from o in db.RecruitmentOffers join m in db.RecruitmentMeritResults on o.RecruitmentMeritResultId equals m.Id join a in db.RecruitmentApplications on m.RecruitmentApplicationId equals a.Id join person in db.RecruitmentApplicants on a.RecruitmentApplicantId equals person.Id join v in db.RecruitmentVacancies on a.RecruitmentVacancyId equals v.Id where person.EfaasIdNumber==identity select new MyRecruitmentOfferRow(o.Id,v.Reference,o.Status,o.TermsSnapshotJson)).ToListAsync(ct);
        var applicantId=await db.RecruitmentApplicants.Where(x=>x.EfaasIdNumber==identity&&x.Status=="Active").Select(x=>(int?)x.Id).SingleOrDefaultAsync(ct);
        return Ok(new MyRecruitmentWorkspace(applications,mine,applicantId));
    }
    [HttpPost("offers/{id:int}/accept")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Accept(int id,CancellationToken ct)=>Ok(await offers.AcceptMyOfferAsync(id,ct));
    [HttpPost("apply")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Apply([FromBody]ApplyToRecruitmentVacancyInput input,CancellationToken ct)
    {
        var identity=User.FindFirstValue("idnumber")?.Trim();
        if(string.IsNullOrWhiteSpace(identity))return Forbid();
        if(string.IsNullOrWhiteSpace(input.Qualifications)||input.Qualifications.Length>4000||string.IsNullOrWhiteSpace(input.Experience)||input.Experience.Length>4000||string.IsNullOrWhiteSpace(input.SupportingStatement)||input.SupportingStatement.Length>4000)return BadRequest();
        await using var db=await factory.CreateDbContextAsync(ct);
        var person=await db.RecruitmentApplicants.AsNoTracking().SingleOrDefaultAsync(x=>x.EfaasIdNumber==identity&&x.Status=="Active",ct);
        if(person is null)return Ok(global::HRM.DTOs.ServiceResult.Fail("Register your applicant profile before applying."));
        var snapshot=System.Text.Json.JsonSerializer.Serialize(new{person.FullName,person.Email,person.Mobile,input.Qualifications,input.Experience,input.SupportingStatement});
        return Ok(await applicationsService.SubmitAsync(new(person.Id,input.VacancyId,snapshot),ct));
    }
}
