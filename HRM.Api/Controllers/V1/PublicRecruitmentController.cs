using System.Text;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/recruitment")]
public sealed class PublicRecruitmentController(
    IDbContextFactory<HrmTeContext> factory,
    IRecruitmentApplicationService applicationService) : ControllerBase
{
    [HttpGet("vacancies")]
    public async Task<ActionResult<IReadOnlyList<PublishedRecruitmentVacancyRow>>> ListAll(CancellationToken ct)
        => Ok(await applicationService.ListPublishedVacanciesAsync(ct));

    [HttpGet("office/{officeSlug}/vacancies")]
    public async Task<ActionResult<OfficeVacanciesResponse>> ListByOffice(string officeSlug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(officeSlug))
            return NotFound();

        var rawSlug = Uri.UnescapeDataString(officeSlug).Trim();
        var normalizedTarget = NormalizeSlug(rawSlug);
        var parsedId = int.TryParse(rawSlug, out var idVal) ? idVal : (int?)null;

        await using var db = await factory.CreateDbContextAsync(ct);
        var orgs = await db.Organisations.AsNoTracking()
            .Select(o => new { o.BusinessEntityID, o.OrganisationName, o.RegistrationNumber })
            .ToListAsync(ct);

        var matched = orgs.FirstOrDefault(o =>
            (parsedId.HasValue && o.BusinessEntityID == parsedId.Value)
            || (!string.IsNullOrWhiteSpace(o.RegistrationNumber) && string.Equals(o.RegistrationNumber.Trim(), rawSlug, StringComparison.OrdinalIgnoreCase))
            || string.Equals(o.OrganisationName.Trim(), rawSlug, StringComparison.OrdinalIgnoreCase)
            || NormalizeSlug(o.OrganisationName) == normalizedTarget);

        if (matched is null)
            return NotFound();

        var all = await applicationService.ListPublishedVacanciesAsync(ct);
        var filtered = all.Where(v => v.OrganisationId == matched.BusinessEntityID).ToList();

        return Ok(new OfficeVacanciesResponse(
            matched.BusinessEntityID,
            matched.OrganisationName,
            NormalizeSlug(matched.OrganisationName),
            filtered));
    }

    [HttpGet("results/{publicationId:int}")]
    public async Task<ActionResult<PublicMeritSheetResponse>> PublicResultSheet(int publicationId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var pub = await (from p in db.RecruitmentResultPublications.AsNoTracking()
                         join v in db.RecruitmentVacancies.AsNoTracking() on p.RecruitmentVacancyId equals v.Id
                         join org in db.Organisations.AsNoTracking() on v.OrganisationId equals org.BusinessEntityID into orgs
                         from org in orgs.DefaultIfEmpty()
                         join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
                         from pm in pms.DefaultIfEmpty()
                         join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
                         from jt in jts.DefaultIfEmpty()
                         join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
                         from jm in jms.DefaultIfEmpty()
                         where p.Id == publicationId
                         select new
                         {
                             p.Id,
                             VacancyReference = v.Reference,
                             PositionName = jt != null ? jt.Name : (jm != null ? jm.Name : v.Reference),
                             OrganisationName = org != null ? org.OrganisationName : null,
                             p.IsFinal,
                             p.PublishedAtUtc,
                             p.ObjectionClosesAtUtc
                         }).SingleOrDefaultAsync(ct);

        if (pub is null) return NotFound();

        var sheet = await (from m in db.RecruitmentMeritResults.AsNoTracking()
                           join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id
                           where m.RecruitmentResultPublicationId == publicationId
                           orderby m.Rank
                           select new PublishedMeritSheetEntry(m.Rank, a.Reference, m.Score, false))
            .ToListAsync(ct);

        return Ok(new PublicMeritSheetResponse(
            pub.Id,
            pub.VacancyReference,
            pub.PositionName,
            pub.OrganisationName,
            pub.IsFinal,
            pub.PublishedAtUtc,
            pub.ObjectionClosesAtUtc,
            sheet));
    }

    private static string NormalizeSlug(string value)
    {
        var sb = new StringBuilder(value.Length);
        var prevDash = false;
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                prevDash = false;
            }
            else if (!prevDash && sb.Length > 0)
            {
                sb.Append('-');
                prevDash = true;
            }
        }
        return sb.ToString().TrimEnd('-');
    }
}
