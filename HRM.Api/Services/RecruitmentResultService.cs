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

public sealed class RecruitmentResultService(
    IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService logs, IHttpContextAccessor http) : IRecruitmentResultService
{
    private async Task<int> OrganisationAsync()
    {
        var organisationId = await access.GetCurrentOrganisationIdAsync();
        if (organisationId <= 0 || !await access.HasOrganisationAccessAsync(organisationId) || !await access.HasPermissionAsync("Recruitment.Manage"))
            throw new UnauthorizedAccessException("Recruitment management permission is required.");
        return organisationId;
    }

    public async Task<ServiceResult> PublishAsync(PublishRecruitmentResultsInput input, CancellationToken ct = default)
    {
        var organisationId = await OrganisationAsync();
        if (input.ObjectionClosesAtUtc <= DateTime.UtcNow) return ServiceResult.Fail("Objection closing time must be in the future.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var vacancy = await db.RecruitmentVacancies.SingleOrDefaultAsync(x => x.Id == input.VacancyId && x.OrganisationId == organisationId, ct) ?? throw new UnauthorizedAccessException();
        if (await db.RecruitmentResultPublications.AnyAsync(x => x.RecruitmentVacancyId == vacancy.Id, ct)) return ServiceResult.Fail("Results are already published.");
        var scores = await (from scorecard in db.RecruitmentPanelScorecards
                            join assessment in db.RecruitmentAssessments on scorecard.RecruitmentAssessmentId equals assessment.Id
                            join application in db.RecruitmentApplications on scorecard.RecruitmentApplicationId equals application.Id
                            where assessment.RecruitmentVacancyId == vacancy.Id && application.Status == "Shortlisted" && scorecard.IsLocked
                            group scorecard by scorecard.RecruitmentApplicationId into grouped
                            select new { ApplicationId = grouped.Key, Score = grouped.Average(x => x.TotalScore), Cards = grouped.Count() }).ToListAsync(ct);
        if (scores.Count == 0) return ServiceResult.Fail("No locked scorecards exist for shortlisted applicants.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"Recruitment results published for vacancy {vacancy.Reference}.");
        var publication = new RecruitmentResultPublication { RecruitmentVacancyId = vacancy.Id, OrganisationId = organisationId, PublishedAtUtc = DateTime.UtcNow, ObjectionClosesAtUtc = input.ObjectionClosesAtUtc, OperationLogId = log.OperationLogId };
        db.RecruitmentResultPublications.Add(publication); await db.SaveChangesAsync(ct);
        foreach (var row in scores.OrderByDescending(x => x.Score).ThenBy(x => x.ApplicationId).Select((x, index) => new { Value = x, Rank = index + 1 }))
            db.RecruitmentMeritResults.Add(new RecruitmentMeritResult { RecruitmentResultPublicationId = publication.Id, RecruitmentApplicationId = row.Value.ApplicationId, Score = row.Value.Score, Rank = row.Rank, CalculationSnapshotJson = JsonSerializer.Serialize(new { row.Value.Cards, row.Value.Score, TieBreak = "applicationIdAscending" }) });
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Merit results published and frozen for the objection period.");
    }

    public async Task<ServiceResult> SubmitObjectionAsync(SubmitRecruitmentObjectionInput input, CancellationToken ct = default)
    {
        var idNumber = http.HttpContext?.User.FindFirstValue("idnumber")?.Trim();
        if (string.IsNullOrWhiteSpace(idNumber) || string.IsNullOrWhiteSpace(input.Grounds) || input.Grounds.Length > 4000) return ServiceResult.Fail("Sign in through eFaas and provide objection grounds.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var publication = await db.RecruitmentResultPublications.SingleOrDefaultAsync(x => x.Id == input.PublicationId && !x.IsFinal && x.ObjectionClosesAtUtc > DateTime.UtcNow, ct) ?? throw new InvalidOperationException("The objection period is closed.");
        var application = await (from app in db.RecruitmentApplications join applicant in db.RecruitmentApplicants on app.RecruitmentApplicantId equals applicant.Id where app.Id == input.ApplicationId && app.RecruitmentVacancyId == publication.RecruitmentVacancyId && applicant.EfaasIdNumber == idNumber select app).SingleOrDefaultAsync(ct) ?? throw new UnauthorizedAccessException("You may object only to your own application.");
        if (await db.RecruitmentObjections.AnyAsync(x => x.RecruitmentResultPublicationId == publication.Id && x.RecruitmentApplicationId == application.Id, ct)) return ServiceResult.Fail("An objection is already recorded for this application.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"Recruitment objection submitted for publication {publication.Id}.");
        db.RecruitmentObjections.Add(new RecruitmentObjection { RecruitmentResultPublicationId = publication.Id, RecruitmentApplicationId = application.Id, Grounds = input.Grounds.Trim(), SubmittedAtUtc = DateTime.UtcNow, OperationLogId = log.OperationLogId });
        await db.SaveChangesAsync(ct); return ServiceResult.Ok("Objection submitted.");
    }

    public async Task<ServiceResult> ResolveObjectionAsync(ResolveRecruitmentObjectionInput input, CancellationToken ct = default)
    {
        if (input.ObjectionId <= 0 || string.IsNullOrWhiteSpace(input.Resolution) || input.Resolution.Length > 4000) return ServiceResult.Fail("Provide an objection resolution.");
        var organisationId = await OrganisationAsync(); var actor = await access.RequireContextAsync(); await using var db = await factory.CreateDbContextAsync(ct);
        var objection = await (from item in db.RecruitmentObjections join publication in db.RecruitmentResultPublications on item.RecruitmentResultPublicationId equals publication.Id where item.Id == input.ObjectionId && publication.OrganisationId == organisationId select item).SingleOrDefaultAsync(ct) ?? throw new UnauthorizedAccessException();
        if (objection.Status != "Submitted") return ServiceResult.Fail("This objection is already resolved.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Recruitment objection {objection.Id} resolved.");
        objection.Status = input.IsUpheld ? "Upheld" : "Rejected"; objection.Resolution = input.Resolution.Trim(); objection.ResolvedByUserId = actor.UserId; objection.ResolvedAtUtc = DateTime.UtcNow; objection.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(ct); return ServiceResult.Ok("Objection resolution recorded.");
    }

    public async Task<ServiceResult> FinalizeAsync(int publicationId, CancellationToken ct = default)
    {
        var organisationId = await OrganisationAsync(); await using var db = await factory.CreateDbContextAsync(ct);
        var publication = await db.RecruitmentResultPublications.SingleOrDefaultAsync(x => x.Id == publicationId && x.OrganisationId == organisationId, ct) ?? throw new UnauthorizedAccessException();
        if (publication.IsFinal) return ServiceResult.Fail("Results are already final.");
        if (await db.RecruitmentObjections.AnyAsync(x => x.RecruitmentResultPublicationId == publication.Id && x.Status == "Upheld", ct)) return ServiceResult.Fail("An upheld objection requires corrected results and republication before finalisation.");
        if (publication.ObjectionClosesAtUtc > DateTime.UtcNow) return ServiceResult.Fail("The objection period is still open.");
        if (await db.RecruitmentObjections.AnyAsync(x => x.RecruitmentResultPublicationId == publication.Id && x.Status == "Submitted", ct)) return ServiceResult.Fail("Resolve all submitted objections before finalising results.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Recruitment results publication {publication.Id} finalised.");
        publication.IsFinal = true; publication.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(ct); return ServiceResult.Ok("Results finalised; offers may now be issued.");
    }
}
