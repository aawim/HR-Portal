using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/assessment-workspace")]
public sealed class RecruitmentAssessmentQueriesController(IDbContextFactory<HrmTeContext> factory,IUserAccessService access):HrmApiController
{
    [HttpGet("{vacancyId:int}")]
    public async Task<ActionResult<RecruitmentAssessmentWorkspace>> Load(int vacancyId,CancellationToken ct)
    {
        var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!await access.HasPermissionAsync("Recruitment.Manage"))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        var vacancy=await db.RecruitmentVacancies.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==vacancyId&&x.OrganisationId==org,ct);
        if(vacancy is null)return NotFound();
        var assessments=await db.RecruitmentAssessments.AsNoTracking().Where(x=>x.RecruitmentVacancyId==vacancyId&&x.OrganisationId==org).OrderBy(x=>x.ScheduledAtUtc).Select(x=>new RecruitmentAssessmentRow(x.Id,x.Type,x.TemplateCode,x.ScheduledAtUtc,x.Status)).ToListAsync(ct);
        var templates=await db.RecruitmentOfficialTemplates.AsNoTracking().Where(x=>x.RecruitmentPolicyVersionId==vacancy.RecruitmentPolicyVersionId&&x.IsActive&&x.VerificationStatus=="Verified").Select(x=>new RecruitmentScoringTemplateRow(x.TemplateCode,x.Name,x.TemplateDefinitionJson)).ToListAsync(ct);
        return Ok(new RecruitmentAssessmentWorkspace(assessments,templates));
    }
}
