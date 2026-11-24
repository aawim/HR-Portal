using System.Security.Claims;
using System.Text.Json;
using HRM.Application.Contracts.Recruitment;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Recruitment;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class RecruitmentApplicationService(IDbContextFactory<HrmTeContext> factory, IOperationLogService logs, IHttpContextAccessor http) : IRecruitmentApplicationService
{
    string EfaasId() => http.HttpContext?.User.FindFirstValue("idnumber")?.Trim() ?? string.Empty;

    public async Task<IReadOnlyList<PublishedRecruitmentVacancyRow>> ListPublishedVacanciesAsync(CancellationToken ct = default)
    {
        await using var d = await factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var raw = await (from x in d.RecruitmentVacancies.AsNoTracking()
                         join pm in d.PositionMatrices.AsNoTracking() on x.PositionMatrixId equals pm.PositionMatrixId into pms
                         from pm in pms.DefaultIfEmpty()
                         join jt in d.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
                         from jt in jts.DefaultIfEmpty()
                         join jm in d.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
                         from jm in jms.DefaultIfEmpty()
                         where x.Status == "Published" && x.ClosingAtUtc > now
                         orderby x.ClosingAtUtc
                         select new
                         {
                             x.Id,
                             x.Reference,
                             x.VacancySlots,
                             ClosingAtUtc = x.ClosingAtUtc!.Value,
                             x.AdvertisementSnapshotJson,
                             FallbackTitle = jt != null ? jt.Name : (jm != null ? jm.Name : x.Reference)
                         }).ToListAsync(ct);

        return raw.Select(x => new PublishedRecruitmentVacancyRow(
            x.Id,
            x.Reference,
            x.VacancySlots,
            x.ClosingAtUtc,
            ExtractTitle(x.AdvertisementSnapshotJson) ?? x.FallbackTitle)).ToList();
    }

    private static string? ExtractTitle(string? snapshotJson)
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

    public async Task<ServiceResult> RegisterAsync(ApplicantRegistrationInput i, CancellationToken ct = default)
    {
        var id = EfaasId();
        if (string.IsNullOrWhiteSpace(id)) return ServiceResult.Fail("Sign in through eFaas before registering an applicant profile.");
        if (string.IsNullOrWhiteSpace(i.FullName) || i.FullName.Length > 250 || string.IsNullOrWhiteSpace(i.Email) || i.Email.Length > 320) return ServiceResult.Fail("Provide a name and email address.");
        await using var d = await factory.CreateDbContextAsync(ct);
        if (await d.RecruitmentApplicants.AnyAsync(x => x.EfaasIdNumber == id || x.Email == i.Email.Trim(), ct)) return ServiceResult.Fail("An applicant account already exists for this eFaas identity or email.");
        d.Add(new RecruitmentApplicant { Reference = $"APP-{Guid.NewGuid():N}"[..24], EfaasIdNumber = id, FullName = i.FullName.Trim(), Email = i.Email.Trim(), Mobile = i.Mobile?.Trim(), CreatedAtUtc = DateTime.UtcNow });
        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok("Applicant profile registered.");
    }

    public async Task<ServiceResult> SubmitAsync(SubmitRecruitmentApplicationInput i, CancellationToken ct = default)
    {
        var id = EfaasId();
        if (string.IsNullOrWhiteSpace(id)) return ServiceResult.Fail("Sign in through eFaas before applying.");
        if (string.IsNullOrWhiteSpace(i.ProfileSnapshotJson) || i.ProfileSnapshotJson.Length > 16000) return ServiceResult.Fail("A complete applicant profile snapshot is required.");
        await using var d = await factory.CreateDbContextAsync(ct);
        var v = await d.RecruitmentVacancies.SingleOrDefaultAsync(x => x.Id == i.VacancyId && x.Status == "Published" && x.ClosingAtUtc > DateTime.UtcNow, ct) ?? throw new InvalidOperationException("This vacancy is not accepting applications.");
        if (!await d.RecruitmentApplicants.AnyAsync(x => x.Id == i.ApplicantId && x.EfaasIdNumber == id && x.Status == "Active", ct)) return ServiceResult.Fail("Applicant profile is unavailable.");
        if (await d.RecruitmentApplications.AnyAsync(x => x.RecruitmentApplicantId == i.ApplicantId && x.RecruitmentVacancyId == i.VacancyId, ct)) return ServiceResult.Fail("An application already exists for this vacancy.");
        var l = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"Recruitment application submitted for vacancy {v.Reference}.");
        d.Add(new RecruitmentApplication { Reference = $"RA-{Guid.NewGuid():N}"[..24], RecruitmentApplicantId = i.ApplicantId, RecruitmentVacancyId = v.Id, OrganisationId = v.OrganisationId, Status = "Submitted", ProfileSnapshotJson = i.ProfileSnapshotJson, SubmittedAtUtc = DateTime.UtcNow, OperationLogId = l.OperationLogId });
        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok("Application submitted.");
    }
}
