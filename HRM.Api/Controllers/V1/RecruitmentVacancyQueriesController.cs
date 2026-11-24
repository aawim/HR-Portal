using System.Text.Json;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/recruitment/vacancies")]
public sealed class RecruitmentVacancyQueriesController(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<List<RecruitmentVacancyRow>>> List(CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !(await access.HasPermissionAsync("Recruitment.View") || await access.HasPermissionAsync("Recruitment.Manage") || await access.HasPermissionAsync("Admin")))
            return Forbid();

        await using var db = await factory.CreateDbContextAsync(ct);
        var raw = await (from v in db.RecruitmentVacancies.AsNoTracking()
                         join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
                         from pm in pms.DefaultIfEmpty()
                         join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
                         from jt in jts.DefaultIfEmpty()
                         join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
                         from jm in jms.DefaultIfEmpty()
                         where v.OrganisationId == org
                         orderby v.Id descending
                         select new
                         {
                             v.Id,
                             v.Reference,
                             v.RecruitmentRequisitionId,
                             v.Status,
                             v.VacancySlots,
                             v.ClosingAtUtc,
                             v.AdvertisementSnapshotJson,
                             FallbackTitle = jt != null ? jt.Name : (jm != null ? jm.Name : $"Position #{v.PositionMatrixId}")
                         }).ToListAsync(ct);

        var rows = raw.Select(x => new RecruitmentVacancyRow(
            x.Id,
            x.Reference,
            x.RecruitmentRequisitionId,
            x.Status,
            x.VacancySlots,
            x.ClosingAtUtc,
            ExtractAdvertisedTitle(x.AdvertisementSnapshotJson) ?? x.FallbackTitle)).ToList();

        return Ok(rows);
    }

    private static string? ExtractAdvertisedTitle(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(snapshotJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("Title", out var t)
                && t.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(t.GetString()))
            {
                return t.GetString()!.Trim();
            }
        }
        catch { }
        return null;
    }
}
