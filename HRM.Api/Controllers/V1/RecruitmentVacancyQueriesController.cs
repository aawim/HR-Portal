using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/vacancies")]
public sealed class RecruitmentVacancyQueriesController(IDbContextFactory<HrmTeContext> factory,IUserAccessService access):HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<List<RecruitmentVacancyRow>>> List(CancellationToken ct)
    {
        var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0 || !await access.HasOrganisationAccessAsync(org) || !(await access.HasPermissionAsync("Recruitment.View") || await access.HasPermissionAsync("Recruitment.Manage") || await access.HasPermissionAsync("Admin"))) return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        return Ok(await db.RecruitmentVacancies.AsNoTracking().Where(x=>x.OrganisationId==org).OrderByDescending(x=>x.Id).Select(x=>new RecruitmentVacancyRow(x.Id,x.Reference,x.RecruitmentRequisitionId,x.Status,x.VacancySlots,x.ClosingAtUtc)).ToListAsync(ct));
    }
}
