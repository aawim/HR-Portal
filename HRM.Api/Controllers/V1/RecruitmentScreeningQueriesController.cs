using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/screening/applications")]
public sealed class RecruitmentScreeningQueriesController(IDbContextFactory<HrmTeContext> factory,IUserAccessService access):HrmApiController
{
    [HttpGet("{vacancyId:int}")]
    public async Task<ActionResult<List<RecruitmentScreeningApplicationRow>>> List(int vacancyId,CancellationToken ct)
    {
        var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!await access.HasPermissionAsync("Recruitment.Manage"))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        if(!await db.RecruitmentVacancies.AnyAsync(x=>x.Id==vacancyId&&x.OrganisationId==org,ct))return NotFound();
        return Ok(await(from app in db.RecruitmentApplications.AsNoTracking() join person in db.RecruitmentApplicants on app.RecruitmentApplicantId equals person.Id where app.RecruitmentVacancyId==vacancyId&&app.OrganisationId==org orderby app.SubmittedAtUtc select new RecruitmentScreeningApplicationRow(app.Id,app.Reference,person.FullName,app.Status,app.SubmittedAtUtc)).ToListAsync(ct));
    }
}
