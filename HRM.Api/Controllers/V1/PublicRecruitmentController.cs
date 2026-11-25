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

    [HttpGet("screening-sheet/{vacancyId:int}")]
    public async Task<ActionResult<ScreeningResultSheetDto>> PublicScreeningSheet(int vacancyId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var vacancy = await (from v in db.RecruitmentVacancies.AsNoTracking()
                             join o in db.Organisations.AsNoTracking() on v.OrganisationId equals o.BusinessEntityID into os
                             from o in os.DefaultIfEmpty()
                             join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
                             from pm in pms.DefaultIfEmpty()
                             join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
                             from jt in jts.DefaultIfEmpty()
                             join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
                             from jm in jms.DefaultIfEmpty()
                             where v.Id == vacancyId && v.Status == "Published"
                             select new
                             {
                                 v.Id,
                                 v.Reference,
                                 v.ClosingAtUtc,
                                 v.AdvertisementSnapshotJson,
                                 AgencyName = o != null ? o.OrganisationName : "",
                                 AgencyNameDhivehi = o != null ? o.OrganisationNameDhivehi : null,
                                 FallbackTitle = jt != null ? jt.Name : (jm != null ? jm.Name : v.Reference),
                                 BasicSalary = jm != null ? (decimal?)jm.BasicSalary : null
                             }).SingleOrDefaultAsync(ct);

        if (vacancy is null) return NotFound();

        var rawApps = await (from app in db.RecruitmentApplications.AsNoTracking()
                             join dec in db.RecruitmentScreeningDecisions.AsNoTracking() on app.Id equals dec.RecruitmentApplicationId into decs
                             from dec in decs.DefaultIfEmpty()
                             where app.RecruitmentVacancyId == vacancyId
                                 && app.Status != "Draft"
                                 && app.Status != "Withdrawn"
                             orderby app.SubmittedAtUtc, app.Id
                             select new
                             {
                                 app.Reference,
                                 app.Status,
                                 HasDecision = dec != null,
                                 IsEligible = dec != null && dec.IsEligible
                             }).ToListAsync(ct);

        if (!rawApps.Any(a => a.HasDecision || a.Status is "Shortlisted" or "EligibleNotShortlisted" or "Ineligible"))
            return NotFound();

        string positionTitle = vacancy.FallbackTitle;
        if (!string.IsNullOrWhiteSpace(vacancy.AdvertisementSnapshotJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(vacancy.AdvertisementSnapshotJson);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("Title", out var t)
                    && t.ValueKind == System.Text.Json.JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(t.GetString()))
                {
                    positionTitle = t.GetString()!.Trim();
                }
            }
            catch (System.Text.Json.JsonException) { }
        }

        var rows = rawApps.Select((a, i) =>
        {
            var meets = a.HasDecision ? a.IsEligible : a.Status is "Shortlisted" or "EligibleNotShortlisted";
            var doesNotMeet = a.HasDecision ? !a.IsEligible : a.Status == "Ineligible";
            return new ScreeningResultSheetRow(i + 1, a.Reference, meets, doesNotMeet, a.Status);
        }).ToList();

        return Ok(new ScreeningResultSheetDto(
            vacancy.Id,
            vacancy.Reference,
            positionTitle,
            vacancy.BasicSalary,
            vacancy.ClosingAtUtc,
            vacancy.AgencyName,
            vacancy.AgencyNameDhivehi,
            null,
            null,
            rows));
    }

    [HttpGet("a2-sheet/{vacancyId:int}")]
    public async Task<ActionResult<A2EvaluationSheetDto>> PublicA2Sheet(int vacancyId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var dto = await RecruitmentResultQueriesController.BuildA2SheetAsync(db, vacancyId, expectedOrgId: null, anonymizeNames: true, requirePublished: true, ct);
        return dto is null ? NotFound() : Ok(dto);
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
