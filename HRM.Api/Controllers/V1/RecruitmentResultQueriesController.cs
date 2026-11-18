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
        var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!await access.HasPermissionAsync("Recruitment.Manage"))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        if(!await db.RecruitmentVacancies.AnyAsync(x=>x.Id==vacancyId&&x.OrganisationId==org,ct))return NotFound();
        var p=await db.RecruitmentResultPublications.AsNoTracking().SingleOrDefaultAsync(x=>x.RecruitmentVacancyId==vacancyId&&x.OrganisationId==org,ct);
        if(p is null)return Ok(new RecruitmentResultWorkspace(null,false,null,[],[]));
        var results=await db.RecruitmentMeritResults.AsNoTracking().Where(x=>x.RecruitmentResultPublicationId==p.Id).OrderBy(x=>x.Rank).Select(x=>new RecruitmentResultRow(x.RecruitmentApplicationId,x.Score,x.Rank)).ToListAsync(ct);
        var objections=await db.RecruitmentObjections.AsNoTracking().Where(x=>x.RecruitmentResultPublicationId==p.Id).Select(x=>new RecruitmentObjectionRow(x.Id,x.RecruitmentApplicationId,x.Grounds,x.Status,x.Resolution)).ToListAsync(ct);
        return Ok(new RecruitmentResultWorkspace(p.Id,p.IsFinal,p.ObjectionClosesAtUtc,results,objections));
    }
}
