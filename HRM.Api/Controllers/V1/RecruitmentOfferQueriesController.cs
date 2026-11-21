using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/offer-workspace")]
public sealed class RecruitmentOfferQueriesController(IDbContextFactory<HrmTeContext> factory,IUserAccessService access):HrmApiController
{
    [HttpGet("{vacancyId:int}")]
    public async Task<ActionResult<RecruitmentOfferWorkspace>> Load(int vacancyId,CancellationToken ct)
    {
        var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!await access.HasPermissionAsync("Recruitment.Manage"))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        var v=await db.RecruitmentVacancies.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==vacancyId&&x.OrganisationId==org,ct);
        if(v is null)return NotFound();
        var candidates=await(from m in db.RecruitmentMeritResults join p in db.RecruitmentResultPublications on m.RecruitmentResultPublicationId equals p.Id join a in db.RecruitmentApplications on m.RecruitmentApplicationId equals a.Id join person in db.RecruitmentApplicants on a.RecruitmentApplicantId equals person.Id where p.RecruitmentVacancyId==v.Id&&p.OrganisationId==org&&p.IsFinal&&a.OrganisationId==org&&m.Rank<=v.VacancySlots&&!db.RecruitmentOffers.Any(x=>x.RecruitmentMeritResultId==m.Id) orderby m.Rank select new RecruitmentOfferCandidateRow(m.Id,a.Reference,person.FullName,m.Rank)).ToListAsync(ct);
        var offers=await(from o in db.RecruitmentOffers join m in db.RecruitmentMeritResults on o.RecruitmentMeritResultId equals m.Id join a in db.RecruitmentApplications on m.RecruitmentApplicationId equals a.Id join person in db.RecruitmentApplicants on a.RecruitmentApplicantId equals person.Id where o.OrganisationId==org&&a.OrganisationId==org&&a.RecruitmentVacancyId==v.Id select new RecruitmentOfferRow(o.Id,a.Reference,person.FullName,o.Status,o.OnboardingRequestId)).ToListAsync(ct);
        return Ok(new RecruitmentOfferWorkspace(candidates,offers));
    }
}
