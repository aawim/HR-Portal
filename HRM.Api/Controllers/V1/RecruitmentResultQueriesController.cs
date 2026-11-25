using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/result-workspace")]
public sealed class RecruitmentResultQueriesController(IDbContextFactory<HrmTeContext> factory,IUserAccessService access):HrmApiController
{
    [HttpGet("{vacancyId:int}")]
    public async Task<ActionResult<RecruitmentResultWorkspace>> Load(int vacancyId,CancellationToken ct)
    {
        var actor=await access.RequireContextAsync();
        var org=await access.GetCurrentOrganisationIdAsync();
        var canView=actor.IsSuperAdministrator||await access.HasPermissionAsync("Recruitment.Manage")||await access.HasPermissionAsync("Recruitment.View")||await access.HasPermissionAsync("Admin");
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!canView)return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        if(!await db.RecruitmentVacancies.AnyAsync(x=>x.Id==vacancyId&&x.OrganisationId==org,ct))return NotFound();
        var p=await db.RecruitmentResultPublications.AsNoTracking().SingleOrDefaultAsync(x=>x.RecruitmentVacancyId==vacancyId&&x.OrganisationId==org,ct);
        if(p is null)return Ok(new RecruitmentResultWorkspace(null,false,null,[],[]));
        var results=await(from m in db.RecruitmentMeritResults.AsNoTracking()
            join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id into apps
            from a in apps.DefaultIfEmpty()
            join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id into people
            from person in people.DefaultIfEmpty()
            where m.RecruitmentResultPublicationId==p.Id
            orderby m.Rank
            select new RecruitmentResultRow(m.RecruitmentApplicationId,m.Score,m.Rank,a!=null?a.Reference:null,person!=null?person.FullName:null)).ToListAsync(ct);
        var objections=await db.RecruitmentObjections.AsNoTracking().Where(x=>x.RecruitmentResultPublicationId==p.Id).Select(x=>new RecruitmentObjectionRow(x.Id,x.RecruitmentApplicationId,x.Grounds,x.Status,x.Resolution)).ToListAsync(ct);
        return Ok(new RecruitmentResultWorkspace(p.Id,p.IsFinal,p.ObjectionClosesAtUtc,results,objections));
    }
}
