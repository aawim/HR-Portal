using System.Security.Claims;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/my-workspace")]
[AllowMissingEmployee]
public sealed class RecruitmentApplicantWorkspaceController(IDbContextFactory<HrmTeContext> factory,global::HRM.Application.Contracts.Recruitment.IRecruitmentOfferService offers,IRecruitmentApplicationService applicationsService,IRecruitmentResultService resultsService):HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<MyRecruitmentWorkspace>> Load(CancellationToken ct)
    {
        var identity=User.FindFirstValue("idnumber")?.Trim();
        if(string.IsNullOrWhiteSpace(identity))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        var applications=await(from a in db.RecruitmentApplications join person in db.RecruitmentApplicants on a.RecruitmentApplicantId equals person.Id join v in db.RecruitmentVacancies on a.RecruitmentVacancyId equals v.Id where person.EfaasIdNumber==identity orderby a.SubmittedAtUtc descending select new MyRecruitmentApplicationRow(a.Reference,v.Reference,a.Status,a.SubmittedAtUtc)).ToListAsync(ct);
        var mine=await(from o in db.RecruitmentOffers join m in db.RecruitmentMeritResults on o.RecruitmentMeritResultId equals m.Id join a in db.RecruitmentApplications on m.RecruitmentApplicationId equals a.Id join person in db.RecruitmentApplicants on a.RecruitmentApplicantId equals person.Id join v in db.RecruitmentVacancies on a.RecruitmentVacancyId equals v.Id where person.EfaasIdNumber==identity select new MyRecruitmentOfferRow(o.Id,v.Reference,o.Status,o.TermsSnapshotJson)).ToListAsync(ct);
        var rawResults=await(from m in db.RecruitmentMeritResults.AsNoTracking()
            join p in db.RecruitmentResultPublications.AsNoTracking() on m.RecruitmentResultPublicationId equals p.Id
            join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id
            join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id
            join v in db.RecruitmentVacancies.AsNoTracking() on p.RecruitmentVacancyId equals v.Id
            join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
            from pm in pms.DefaultIfEmpty()
            join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
            from jt in jts.DefaultIfEmpty()
            join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
            from jm in jms.DefaultIfEmpty()
            where person.EfaasIdNumber==identity
            orderby p.Id descending
            select new { p.Id, ApplicationId=a.Id, ApplicationReference=a.Reference, VacancyReference=v.Reference, PositionName=jt!=null?jt.Name:(jm!=null?jm.Name:null), m.Rank, m.Score, p.IsFinal, p.ObjectionClosesAtUtc }).ToListAsync(ct);
        var pubIds=rawResults.Select(x=>x.Id).Distinct().ToList();
        var appIds=rawResults.Select(x=>x.ApplicationId).Distinct().ToList();
        var myObjections=pubIds.Count==0?[]:await db.RecruitmentObjections.AsNoTracking()
            .Where(o=>pubIds.Contains(o.RecruitmentResultPublicationId)&&appIds.Contains(o.RecruitmentApplicationId))
            .ToListAsync(ct);
        var results=rawResults.Select(r=>
        {
            var obj=myObjections.FirstOrDefault(o=>o.RecruitmentResultPublicationId==r.Id&&o.RecruitmentApplicationId==r.ApplicationId);
            return new MyRecruitmentResultRow(r.Id,r.ApplicationId,r.ApplicationReference,r.VacancyReference,r.PositionName,r.Rank,r.Score,r.IsFinal,r.ObjectionClosesAtUtc,obj?.Id,obj?.Status,obj?.Grounds,obj?.Resolution);
        }).ToList();
        var applicantId=await db.RecruitmentApplicants.Where(x=>x.EfaasIdNumber==identity&&x.Status=="Active").Select(x=>(int?)x.Id).SingleOrDefaultAsync(ct);
        return Ok(new MyRecruitmentWorkspace(applications,mine,applicantId,results));
    }
    [HttpPost("objections")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitObjection([FromBody]SubmitRecruitmentObjectionInput input,CancellationToken ct)=>Ok(await resultsService.SubmitObjectionAsync(input,ct));
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
