using System.Security.Claims;
using HRM.Application.Contracts.Recruitment;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Recruitment;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class RecruitmentOfferService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService logs,
    IHttpContextAccessor http) : IRecruitmentOfferService
{
    private async Task<(int Org, int User)> Auth()
    {
        var o = await access.GetCurrentOrganisationIdAsync();
        var a = await access.RequireContextAsync();
        var canManage = await access.HasUserGroupAsync("SuperAdministrator")
            || await access.HasUserGroupAsync("Admin")
            || await access.HasPermissionAsync("Recruitment.Manage");
        if (o <= 0 || !await access.HasOrganisationAccessAsync(o) || !canManage)
            throw new UnauthorizedAccessException("Recruitment management permission is required.");
        return (o, a.UserId);
    }

    public async Task<ServiceResult> CreateAsync(CreateRecruitmentOfferInput i, CancellationToken ct = default)
    {
        if (i.MeritResultId <= 0 || string.IsNullOrWhiteSpace(i.TermsSnapshotJson) || i.TermsSnapshotJson.Length > 16000)
            return ServiceResult.Fail("Provide the selected merit result and offer terms.");

        var (o, u) = await Auth();
        await using var d = await factory.CreateDbContextAsync(ct);

        var r = await (from m in d.RecruitmentMeritResults
                       join p in d.RecruitmentResultPublications on m.RecruitmentResultPublicationId equals p.Id
                       join v in d.RecruitmentVacancies on p.RecruitmentVacancyId equals v.Id
                       join app in d.RecruitmentApplications on m.RecruitmentApplicationId equals app.Id
                       join applicant in d.RecruitmentApplicants on app.RecruitmentApplicantId equals applicant.Id
                       where m.Id == i.MeritResultId && v.OrganisationId == o
                       select new { m, p, v, app, applicant }).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException();

        if (!r.p.IsFinal)
            return ServiceResult.Fail("Results must be finalised after objections before an offer is issued.");

        var activeOffersCount = await (from off in d.RecruitmentOffers
                                       join m in d.RecruitmentMeritResults on off.RecruitmentMeritResultId equals m.Id
                                       join a in d.RecruitmentApplications on m.RecruitmentApplicationId equals a.Id
                                       where a.RecruitmentVacancyId == r.v.Id && off.OrganisationId == o && off.Status != "Declined"
                                       select off.Id).CountAsync(ct);

        if (activeOffersCount >= r.v.VacancySlots)
            return ServiceResult.Fail($"All {r.v.VacancySlots} vacancy slot(s) already have active or accepted offers.");

        if (await d.RecruitmentOffers.AnyAsync(x => x.RecruitmentMeritResultId == r.m.Id, ct))
            return ServiceResult.Fail("An offer already exists for this merit result.");

        var l = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"Recruitment offer issued for merit result {r.m.Id} ({r.app.Reference}).");
        d.RecruitmentOffers.Add(new RecruitmentOffer
        {
            RecruitmentMeritResultId = r.m.Id,
            OrganisationId = o,
            AppointmentAuthorityUserId = u,
            TermsSnapshotJson = i.TermsSnapshotJson,
            Status = "Offered",
            OfferedAtUtc = DateTime.UtcNow,
            OperationLogId = l.OperationLogId
        });

        // Notify the applicant if they have an active user account
        if (!string.IsNullOrWhiteSpace(r.applicant.EfaasIdNumber))
        {
            var targetUsers = await d.Users.AsNoTracking()
                .Where(usr => usr.Username == r.applicant.EfaasIdNumber && usr.IsActive)
                .Select(usr => new { usr.UserId, usr.BusinessEntityID })
                .ToListAsync(ct);

            foreach (var target in targetUsers)
            {
                d.StaffNotifications.Add(new StaffNotification
                {
                    UserId = target.UserId,
                    IndividualId = target.BusinessEntityID,
                    OrganisationId = o,
                    Kind = "RecruitmentOffer",
                    Message = $"You have received a job offer for vacancy {r.v.Reference}. Review and respond under My Offers.",
                    Url = "/online/recruitment",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok($"Job offer issued to {r.applicant.FullName} ({r.app.Reference}).");
    }

    public async Task<ServiceResult> AcceptMyOfferAsync(int offerId, CancellationToken ct = default)
    {
        var id = http.HttpContext?.User.FindFirstValue("idnumber")?.Trim();
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult.Fail("Sign in through eFaas before accepting an offer.");

        await using var d = await factory.CreateDbContextAsync(ct);
        var info = await (from x in d.RecruitmentOffers
                          join m in d.RecruitmentMeritResults on x.RecruitmentMeritResultId equals m.Id
                          join app in d.RecruitmentApplications on m.RecruitmentApplicationId equals app.Id
                          join v in d.RecruitmentVacancies on app.RecruitmentVacancyId equals v.Id
                          join applicant in d.RecruitmentApplicants on app.RecruitmentApplicantId equals applicant.Id
                          where x.Id == offerId && applicant.EfaasIdNumber == id
                          select new { Offer = x, ApplicantName = applicant.FullName, VacancyRef = v.Reference }).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("You may accept only your own offer.");

        if (info.Offer.Status != "Offered")
            return ServiceResult.Fail("Only an issued offer may be accepted.");

        var l = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Recruitment offer {info.Offer.Id} accepted by eFaas-bound applicant.");
        info.Offer.Status = "Accepted";
        info.Offer.AcceptedAtUtc = DateTime.UtcNow;
        info.Offer.OperationLogId = l.OperationLogId;

        await RecruitmentNotificationHelper.NotifyHrOfferResponseAsync(d, info.Offer.OrganisationId, info.ApplicantName, info.VacancyRef, accepted: true, declineReason: null, ct);

        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok("Offer accepted; HR has been notified to initiate onboarding.");
    }

    public async Task<ServiceResult> DeclineMyOfferAsync(int offerId, string? reason = null, CancellationToken ct = default)
    {
        var id = http.HttpContext?.User.FindFirstValue("idnumber")?.Trim();
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult.Fail("Sign in through eFaas before declining an offer.");

        await using var d = await factory.CreateDbContextAsync(ct);
        var info = await (from x in d.RecruitmentOffers
                          join m in d.RecruitmentMeritResults on x.RecruitmentMeritResultId equals m.Id
                          join app in d.RecruitmentApplications on m.RecruitmentApplicationId equals app.Id
                          join v in d.RecruitmentVacancies on app.RecruitmentVacancyId equals v.Id
                          join applicant in d.RecruitmentApplicants on app.RecruitmentApplicantId equals applicant.Id
                          where x.Id == offerId && applicant.EfaasIdNumber == id
                          select new { Offer = x, ApplicantName = applicant.FullName, VacancyRef = v.Reference }).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("You may decline only your own offer.");

        if (info.Offer.Status != "Offered")
            return ServiceResult.Fail("Only a pending offer may be declined.");

        var cleanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (cleanReason?.Length > 1000) cleanReason = cleanReason[..1000];

        var l = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Recruitment offer {info.Offer.Id} declined by applicant.");
        info.Offer.Status = "Declined";
        info.Offer.OperationLogId = l.OperationLogId;

        await RecruitmentNotificationHelper.NotifyHrOfferResponseAsync(d, info.Offer.OrganisationId, info.ApplicantName, info.VacancyRef, accepted: false, declineReason: cleanReason, ct);

        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok("Offer declined and HR has been notified.");
    }
}
