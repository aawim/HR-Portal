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

    public async Task<ServiceResult> AcceptMyOfferAsync(int offerId, AcceptRecruitmentOfferInput? input = null, CancellationToken ct = default)
    {
        var id = http.HttpContext?.User.FindFirstValue("idnumber")?.Trim();
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult.Fail("Sign in through eFaas before accepting an offer.");

        if (input is null)
            return ServiceResult.Fail("Complete and sign the Job Offer Acceptance Form before submitting.");

        if (!input.AgreedToTerms || !input.AgreedToCodeOfConduct || !input.ConfirmedInformationAccuracy)
            return ServiceResult.Fail("You must review and tick all mandatory appointment declarations before submitting.");

        if (input.ConfirmedJoiningDate.Year is < 2000 or > 2100)
            return ServiceResult.Fail("Provide a valid confirmed joining date.");

        if (input.IsCurrentlyEmployed && string.IsNullOrWhiteSpace(input.CurrentEmployer))
            return ServiceResult.Fail("Specify your current workplace/employer since you indicated you are currently employed.");

        var sigDataUrl = input.SignatureDataUrl?.Trim() ?? string.Empty;
        var validSig = !string.IsNullOrWhiteSpace(sigDataUrl)
            && sigDataUrl.Length <= 450_000
            && (sigDataUrl.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase)
                || sigDataUrl.StartsWith("data:image/webp;base64,", StringComparison.OrdinalIgnoreCase)
                || sigDataUrl.StartsWith("data:image/jpeg;base64,", StringComparison.OrdinalIgnoreCase));
        if (!validSig)
            return ServiceResult.Fail("Draw or upload your signature on the Job Offer Acceptance Form before submitting.");

        await using var d = await factory.CreateDbContextAsync(ct);
        var info = await (from x in d.RecruitmentOffers
                          join m in d.RecruitmentMeritResults on x.RecruitmentMeritResultId equals m.Id
                          join app in d.RecruitmentApplications on m.RecruitmentApplicationId equals app.Id
                          join v in d.RecruitmentVacancies on app.RecruitmentVacancyId equals v.Id
                          join applicant in d.RecruitmentApplicants on app.RecruitmentApplicantId equals applicant.Id
                          where x.Id == offerId && applicant.EfaasIdNumber == id
                          select new { Offer = x, ApplicantName = applicant.FullName, VacancyRef = v.Reference, ApplicantId = applicant.Id }).SingleOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("You may accept only your own offer.");

        if (info.Offer.Status != "Offered")
            return ServiceResult.Fail("Only an issued offer may be accepted.");

        var now = DateTime.UtcNow;
        var verificationCode = $"SIG-{now:yyyy}-{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        // Merge original HR terms + appointee acceptance form into TermsSnapshotJson
        var merged = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(info.Offer.TermsSnapshotJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(info.Offer.TermsSnapshotJson);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        merged[prop.Name] = prop.Value.Clone();
                }
            }
            catch (System.Text.Json.JsonException)
            {
                merged["Terms"] = info.Offer.TermsSnapshotJson;
            }
        }

        var acceptancePayload = new
        {
            ConfirmedJoiningDate = input.ConfirmedJoiningDate.ToString("yyyy-MM-dd"),
            IsCurrentlyEmployed = input.IsCurrentlyEmployed,
            CurrentEmployer = string.IsNullOrWhiteSpace(input.CurrentEmployer) ? null : input.CurrentEmployer.Trim(),
            RequiresWrittenNotice = input.RequiresWrittenNotice,
            NoticePeriod = string.IsNullOrWhiteSpace(input.NoticePeriod) ? null : input.NoticePeriod.Trim(),
            EmergencyContactName = string.IsNullOrWhiteSpace(input.EmergencyContactName) ? null : input.EmergencyContactName.Trim(),
            EmergencyContactPhone = string.IsNullOrWhiteSpace(input.EmergencyContactPhone) ? null : input.EmergencyContactPhone.Trim(),
            BankName = string.IsNullOrWhiteSpace(input.BankName) ? null : input.BankName.Trim(),
            BankAccountNumber = string.IsNullOrWhiteSpace(input.BankAccountNumber) ? null : input.BankAccountNumber.Trim(),
            AppointeeNotes = string.IsNullOrWhiteSpace(input.AppointeeNotes) ? null : input.AppointeeNotes.Trim(),
            input.AgreedToTerms,
            input.AgreedToCodeOfConduct,
            input.ConfirmedInformationAccuracy,
            VerificationCode = verificationCode,
            SignedAtUtc = now
        };
        merged["AcceptanceForm"] = acceptancePayload;

        var mergedJson = System.Text.Json.JsonSerializer.Serialize(merged);
        var docHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(mergedJson)));

        // Resolve UserId/IndividualId if applicant has an internal user/idcard record
        var signerUserId = await d.Users.AsNoTracking()
            .Where(u => u.Username == id && u.IsActive)
            .Select(u => u.UserId)
            .FirstOrDefaultAsync(ct);
        var signerIndividualId = await d.Idcards.AsNoTracking()
            .Where(c => c.IdcardNumber == id)
            .Select(c => c.BusinessEntityId)
            .FirstOrDefaultAsync(ct);

        var l = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Recruitment offer {info.Offer.Id} signed & accepted by {info.ApplicantName} ({verificationCode}).");

        // Ensure DS_DocumentSignatures exists and record frozen appointee signature
        await HRM.Api.Controllers.V1.DocumentSignaturesController.EnsureTablesPublicAsync(d, ct);
        d.DocumentSignatures.Add(new HRM.Models.Signatures.DocumentSignature
        {
            OrganisationId = info.Offer.OrganisationId,
            DocumentType = "RecruitmentOffer",
            DocumentId = info.Offer.Id.ToString(),
            SlotKey = "AppointeeAcceptance",
            SlotLabel = "Appointee Acceptance",
            SignerUserId = signerUserId,
            SignerIndividualId = signerIndividualId,
            SignerName = info.ApplicantName,
            SignerPosition = "Appointee",
            SignatureImageDataUrl = sigDataUrl,
            DocumentContentHash = docHash,
            VerificationCode = verificationCode,
            SignedAtUtc = now,
            IpAddress = http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Status = "Signed",
            OperationLogId = l.OperationLogId
        });

        info.Offer.TermsSnapshotJson = mergedJson.Length <= 16000 ? mergedJson : info.Offer.TermsSnapshotJson;
        info.Offer.Status = "Accepted";
        info.Offer.AcceptedAtUtc = now;
        info.Offer.OperationLogId = l.OperationLogId;

        await RecruitmentNotificationHelper.NotifyHrOfferResponseAsync(d, info.Offer.OrganisationId, info.ApplicantName, info.VacancyRef, accepted: true, declineReason: null, ct);

        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok($"Job Offer Acceptance Form signed & submitted ({verificationCode}). HR has been notified to proceed with onboarding.");
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

    public async Task<ServiceResult> RecordHrDecisionAsync(int offerId, HrOfferDecisionInput input, CancellationToken ct = default)
    {
        var (o, u) = await Auth();
        var decision = input.Decision?.Trim() ?? string.Empty;
        var comment = input.Comment?.Trim() ?? string.Empty;
        if (decision is not ("ReturnForCorrection" or "RejectOffer"))
            return ServiceResult.Fail("Specify a valid HR decision (ReturnForCorrection or RejectOffer).");
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > 1000)
            return ServiceResult.Fail("Provide a clear comment/reason (up to 1,000 characters).");

        await using var d = await factory.CreateDbContextAsync(ct);
        var info = await (from x in d.RecruitmentOffers
                          join m in d.RecruitmentMeritResults on x.RecruitmentMeritResultId equals m.Id
                          join app in d.RecruitmentApplications on m.RecruitmentApplicationId equals app.Id
                          join v in d.RecruitmentVacancies on app.RecruitmentVacancyId equals v.Id
                          join applicant in d.RecruitmentApplicants on app.RecruitmentApplicantId equals applicant.Id
                          where x.Id == offerId && x.OrganisationId == o
                          select new { Offer = x, ApplicantName = applicant.FullName, applicant.EfaasIdNumber, VacancyRef = v.Reference }).SingleOrDefaultAsync(ct);

        if (info is null) return ServiceResult.Fail("Offer not found.");
        if (info.Offer.Status != "Accepted") return ServiceResult.Fail($"Only Accepted offers can be reviewed here (current status: {info.Offer.Status}).");
        if (info.Offer.OnboardingRequestId.HasValue) return ServiceResult.Fail("This offer is already linked to an onboarding request.");

        var now = DateTime.UtcNow;
        var merged = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(info.Offer.TermsSnapshotJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(info.Offer.TermsSnapshotJson);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        merged[prop.Name] = prop.Value.Clone();
                }
            }
            catch (System.Text.Json.JsonException) { merged["Terms"] = info.Offer.TermsSnapshotJson; }
        }

        merged["HrDecision"] = new
        {
            Decision = decision,
            Comment = comment,
            DecidedByUserId = u,
            DecidedAtUtc = now
        };
        var updatedJson = System.Text.Json.JsonSerializer.Serialize(merged);
        if (updatedJson.Length <= 16000)
            info.Offer.TermsSnapshotJson = updatedJson;

        if (decision == "ReturnForCorrection")
        {
            info.Offer.Status = "Offered";
            info.Offer.AcceptedAtUtc = null;

            await HRM.Api.Controllers.V1.DocumentSignaturesController.EnsureTablesPublicAsync(d, ct);
            var activeSigs = await d.DocumentSignatures
                .Where(s => s.DocumentType == "RecruitmentOffer" && s.DocumentId == info.Offer.Id.ToString() && s.SlotKey == "AppointeeAcceptance" && s.Status == "Signed")
                .ToListAsync(ct);
            foreach (var sig in activeSigs)
            {
                sig.Status = "Revoked";
                sig.RevokedAtUtc = now;
                sig.RevokedByUserId = u;
                sig.RevokeReason = $"Returned by HR: {comment}";
            }
        }
        else
        {
            info.Offer.Status = "Declined";
        }

        var logMsg = decision == "ReturnForCorrection"
            ? $"HR returned Job Offer Acceptance Form #{info.Offer.Id} to {info.ApplicantName} for correction."
            : $"HR rejected/withdrew Job Offer #{info.Offer.Id} for {info.ApplicantName}; slot reopened.";
        var log = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, logMsg);
        info.Offer.OperationLogId = log.OperationLogId;

        if (!string.IsNullOrWhiteSpace(info.EfaasIdNumber))
        {
            var targetUsers = await d.Users.AsNoTracking()
                .Where(usr => usr.Username == info.EfaasIdNumber && usr.IsActive)
                .Select(usr => new { usr.UserId, usr.BusinessEntityID })
                .ToListAsync(ct);

            var notifMsg = decision == "ReturnForCorrection"
                ? $"HR returned your Job Offer Form for vacancy {info.VacancyRef} for correction: {comment}"
                : $"Your Job Offer for vacancy {info.VacancyRef} was withdrawn by HR: {comment}";

            foreach (var target in targetUsers)
            {
                d.StaffNotifications.Add(new StaffNotification
                {
                    UserId = target.UserId,
                    IndividualId = target.BusinessEntityID,
                    OrganisationId = o,
                    Kind = "RecruitmentOfferDecision",
                    Message = notifMsg,
                    Url = "/online/recruitment",
                    CreatedAtUtc = now
                });
            }
        }

        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok(decision == "ReturnForCorrection"
            ? $"Offer form returned to {info.ApplicantName} for correction."
            : $"Offer withdrawn for {info.ApplicantName}. The vacancy slot is now available for the next ranked candidate.");
    }
}
