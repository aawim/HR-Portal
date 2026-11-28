using System.Security.Claims;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/recruitment/offer-workspace")]
public sealed class RecruitmentOfferQueriesController(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access) : HrmApiController
{
    [HttpGet("{vacancyId:int}")]
    public async Task<ActionResult<RecruitmentOfferWorkspace>> Load(int vacancyId, CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        var canManage = await access.HasUserGroupAsync("SuperAdministrator")
            || await access.HasUserGroupAsync("Admin")
            || await access.HasPermissionAsync("Recruitment.Manage");
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !canManage) return Forbid();

        await using var db = await factory.CreateDbContextAsync(ct);
        var v = await (from vac in db.RecruitmentVacancies.AsNoTracking()
                       join req in db.RecruitmentRequisitions.AsNoTracking() on vac.RecruitmentRequisitionId equals req.Id into reqs
                       from req in reqs.DefaultIfEmpty()
                       join pm in db.PositionMatrices.AsNoTracking() on vac.PositionMatrixId equals pm.PositionMatrixId into pms
                       from pm in pms.DefaultIfEmpty()
                       join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
                       from jt in jts.DefaultIfEmpty()
                       join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
                       from jm in jms.DefaultIfEmpty()
                       where vac.Id == vacancyId && vac.OrganisationId == org
                       select new
                       {
                           vac.Id,
                           vac.Reference,
                           vac.VacancySlots,
                           vac.PositionMatrixId,
                           JobMatrixId = pm != null ? (int?)pm.JobMatrixId : null,
                           JobMatrixStepId = pm != null ? pm.JobMatrixStepId : null,
                           EmploymentType = req != null ? req.EmploymentType : null,
                           PositionName = jt != null ? jt.Name : (jm != null ? jm.Name : vac.Reference),
                           BasicSalary = jm != null ? (decimal?)jm.BasicSalary : null,
                           AttendanceAllowanceDaily = jm != null ? jm.AttendanceBenefitDaily : null,
                           GradeCode = jm != null ? (jm.GradeCode ?? jm.PublicServiceRank) : null,
                           DefaultStructureId = pm != null ? pm.OrganisationStructureId : null
                       }).SingleOrDefaultAsync(ct);

        if (v is null) return NotFound();

        var (effectiveBasicSalary, positionAllowance, otherAllowancesSummary) = await LoadMatrixRemunerationAsync(db, v.JobMatrixId, v.JobMatrixStepId, v.BasicSalary, ct);

        var isFinal = await db.RecruitmentResultPublications.AsNoTracking()
            .AnyAsync(p => p.RecruitmentVacancyId == v.Id && p.OrganisationId == org && p.IsFinal, ct);

        var rawOffers = await (from o in db.RecruitmentOffers.AsNoTracking()
                               join m in db.RecruitmentMeritResults.AsNoTracking() on o.RecruitmentMeritResultId equals m.Id
                               join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id
                               join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id
                               where o.OrganisationId == org && a.OrganisationId == org && a.RecruitmentVacancyId == v.Id
                               orderby o.OfferedAtUtc descending
                               select new
                               {
                                   o.Id,
                                   a.Reference,
                                   person.FullName,
                                   o.Status,
                                   o.OnboardingRequestId,
                                   person.EfaasIdNumber,
                                   o.OfferedAtUtc,
                                   o.AcceptedAtUtc,
                                   o.TermsSnapshotJson
                               }).ToListAsync(ct);

        var efaasIds = rawOffers
            .Where(x => !string.IsNullOrWhiteSpace(x.EfaasIdNumber))
            .Select(x => x.EfaasIdNumber!)
            .Distinct()
            .ToList();

        var individualByIdCard = efaasIds.Count == 0
            ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            : (await db.Idcards.AsNoTracking()
                .Where(c => efaasIds.Contains(c.IdcardNumber))
                .Select(c => new { c.IdcardNumber, c.BusinessEntityId })
                .ToListAsync(ct))
                .GroupBy(c => c.IdcardNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().BusinessEntityId, StringComparer.OrdinalIgnoreCase);

        var offerIdStrings = rawOffers.Select(x => x.Id.ToString()).ToList();
        var offerSigs = new Dictionary<string, (string Img, string Code)>();
        var agencySignedOffers = new HashSet<string>();
        if (offerIdStrings.Count > 0)
        {
            await DocumentSignaturesController.EnsureTablesPublicAsync(db, ct);
            var sigs = await db.DocumentSignatures.AsNoTracking()
                .Where(s => s.OrganisationId == org && s.DocumentType == "RecruitmentOffer" && s.Status == "Signed" && offerIdStrings.Contains(s.DocumentId))
                .Select(s => new { s.DocumentId, s.SlotKey, s.SignatureImageDataUrl, s.VerificationCode })
                .ToListAsync(ct);
            foreach (var s in sigs)
            {
                if (s.SlotKey == "AppointeeAcceptance") offerSigs[s.DocumentId] = (s.SignatureImageDataUrl, s.VerificationCode);
                if (s.SlotKey == "AgencyIssuer") agencySignedOffers.Add(s.DocumentId);
            }
        }

        var offers = rawOffers.Select(o =>
        {
            offerSigs.TryGetValue(o.Id.ToString(), out var sig);
            return new RecruitmentOfferRow(
                o.Id,
                o.Reference,
                o.FullName,
                o.Status,
                o.OnboardingRequestId,
                o.EfaasIdNumber,
                !string.IsNullOrWhiteSpace(o.EfaasIdNumber) && individualByIdCard.TryGetValue(o.EfaasIdNumber, out var indId) ? indId : null,
                o.OfferedAtUtc,
                o.AcceptedAtUtc,
                o.TermsSnapshotJson,
                sig.Img,
                sig.Code,
                agencySignedOffers.Contains(o.Id.ToString()));
        }).ToList();

        var activeOffersCount = offers.Count(x => x.Status != "Declined");
        var candidates = activeOffersCount >= v.VacancySlots
            ? new List<RecruitmentOfferCandidateRow>()
            : await (from m in db.RecruitmentMeritResults.AsNoTracking()
                     join p in db.RecruitmentResultPublications.AsNoTracking() on m.RecruitmentResultPublicationId equals p.Id
                     join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id
                     join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id
                     where p.RecruitmentVacancyId == v.Id
                         && p.OrganisationId == org
                         && p.IsFinal
                         && a.OrganisationId == org
                         && !db.RecruitmentOffers.Any(x => x.RecruitmentMeritResultId == m.Id)
                     orderby m.Rank
                     select new RecruitmentOfferCandidateRow(m.Id, a.Reference, person.FullName, m.Rank, m.Score)).ToListAsync(ct);

        // Load onboarding lookup options (approved structures, active job types, vacant positions with pay steps)
        var structures = await db.OrganisationStructures.AsNoTracking()
            .Where(x => x.OrganisationBusinessEntityID == org
                && x.OrganisationStructureStateId == 3
                && db.OrganisationStructureTypes.Any(t => t.OrganisationStructureTypeId == x.OrganisationStructureTypeId))
            .OrderBy(x => x.Name)
            .Select(x => new RecruitmentOnboardingLookupOption(x.OrganisationStructureId, x.Name))
            .ToListAsync(ct);

        var jobTypes = await db.JobTypes.AsNoTracking()
            .Where(x => x.IsActive != false)
            .OrderBy(x => x.TypeName)
            .Select(x => new RecruitmentOnboardingLookupOption(x.JobTypeId, x.TypeName))
            .ToListAsync(ct);

        var today = DateTime.UtcNow.AddHours(5).Date;
        var rawPositions = await PositionVacancy.Vacant(db, today)
            .Where(x => x.IsActive != false && (x.IsGlobal || x.OrganisationId == org))
            .OrderBy(x => x.Name)
            .Select(x => new { x.PositionId, x.Name, x.PositionMatrixId })
            .ToListAsync(ct);

        var vacantPositions = new List<RecruitmentVacantPositionOption>(rawPositions.Count);
        foreach (var p in rawPositions)
        {
            var steps = new List<RecruitmentPayStepOption>();
            if (p.PositionMatrixId.HasValue)
            {
                var summary = await PositionMatrixAssignments.SummaryAsync(db, p.PositionId);
                if (summary?.Steps is not null)
                {
                    steps.AddRange(summary.Steps.Select(s => new RecruitmentPayStepOption(s.Id, $"Step {s.Number}", s.BasicSalary)));
                }
            }
            vacantPositions.Add(new RecruitmentVacantPositionOption(p.PositionId, p.Name, p.PositionMatrixId, steps));
        }

        var defaultPosId = vacantPositions.FirstOrDefault(p => p.PositionMatrixId == v.PositionMatrixId)?.Id;

        int? defaultJobTypeId = null;
        if (!string.IsNullOrWhiteSpace(v.EmploymentType))
        {
            var targetType = v.EmploymentType.Trim();
            defaultJobTypeId = jobTypes.FirstOrDefault(jt => string.Equals(jt.Name.Trim(), targetType, StringComparison.OrdinalIgnoreCase))?.Id
                ?? jobTypes.FirstOrDefault(jt => jt.Name.Contains(targetType, StringComparison.OrdinalIgnoreCase) || targetType.Contains(jt.Name, StringComparison.OrdinalIgnoreCase))?.Id;
        }
        defaultJobTypeId ??= jobTypes.FirstOrDefault()?.Id;

        var context = new RecruitmentOfferContext(
            v.PositionName,
            effectiveBasicSalary,
            v.EmploymentType,
            v.DefaultStructureId,
            defaultPosId,
            structures,
            jobTypes,
            vacantPositions,
            defaultJobTypeId,
            positionAllowance,
            v.AttendanceAllowanceDaily,
            otherAllowancesSummary,
            v.GradeCode);

        return Ok(new RecruitmentOfferWorkspace(candidates, offers, context, v.VacancySlots, isFinal));
    }

    [HttpGet("offer/{offerId:int}")]
    [AllowMissingEmployee]
    public async Task<ActionResult<JobOfferLetterDetailDto>> OfferDetail(int offerId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var row = await (from o in db.RecruitmentOffers.AsNoTracking()
                         join m in db.RecruitmentMeritResults.AsNoTracking() on o.RecruitmentMeritResultId equals m.Id
                         join app in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals app.Id
                         join person in db.RecruitmentApplicants.AsNoTracking() on app.RecruitmentApplicantId equals person.Id
                         join v in db.RecruitmentVacancies.AsNoTracking() on app.RecruitmentVacancyId equals v.Id
                         join req in db.RecruitmentRequisitions.AsNoTracking() on v.RecruitmentRequisitionId equals req.Id into reqs
                         from req in reqs.DefaultIfEmpty()
                         join org in db.Organisations.AsNoTracking() on o.OrganisationId equals org.BusinessEntityID into orgs
                         from org in orgs.DefaultIfEmpty()
                         join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
                         from pm in pms.DefaultIfEmpty()
                         join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
                         from jt in jts.DefaultIfEmpty()
                         join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
                         from jm in jms.DefaultIfEmpty()
                         join issuer in db.Users.AsNoTracking() on o.AppointmentAuthorityUserId equals issuer.UserId into issuers
                         from issuer in issuers.DefaultIfEmpty()
                         where o.Id == offerId
                         select new
                         {
                             o.Id,
                             o.OrganisationId,
                             o.Status,
                             person.FullName,
                             person.EfaasIdNumber,
                             ApplicationCode = app.Reference,
                             PositionTitle = jt != null ? jt.Name : (jm != null ? jm.Name : v.Reference),
                             JobMatrixId = pm != null ? (int?)pm.JobMatrixId : null,
                             JobMatrixStepId = pm != null ? pm.JobMatrixStepId : null,
                             BasicSalary = jm != null ? (decimal?)jm.BasicSalary : null,
                             AttendanceAllowanceDaily = jm != null ? jm.AttendanceBenefitDaily : null,
                             GradeCode = jm != null ? (jm.GradeCode ?? jm.PublicServiceRank) : null,
                             EmploymentType = req != null ? req.EmploymentType : null,
                             AnnouncementNumber = v.Reference,
                             AgencyName = org != null ? org.OrganisationName : "",
                             AgencyNameDhivehi = org != null ? org.OrganisationNameDhivehi : null,
                             o.OfferedAtUtc,
                             o.AcceptedAtUtc,
                             o.TermsSnapshotJson,
                             IssuedByName = issuer != null ? (!string.IsNullOrWhiteSpace(issuer.FullName) ? issuer.FullName : issuer.Username) : null
                         }).SingleOrDefaultAsync(ct);

        if (row is null) return NotFound();

        // Authorize either HR with organisation access OR the eFaas-bound appointee (published offers only)
        var canManage = await access.HasOrganisationAccessAsync(row.OrganisationId)
            && (await access.HasUserGroupAsync("SuperAdministrator")
                || await access.HasUserGroupAsync("Admin")
                || await access.HasPermissionAsync("Recruitment.Manage")
                || await access.HasPermissionAsync("Recruitment.View"));

        var idNumber = User.FindFirstValue("idnumber")?.Trim();
        var isAppointee = !string.IsNullOrWhiteSpace(idNumber) && string.Equals(idNumber, row.EfaasIdNumber, StringComparison.OrdinalIgnoreCase);

        if (!canManage && (!isAppointee || row.Status == "Draft"))
            return Forbid();

        await DocumentSignaturesController.EnsureTablesPublicAsync(db, ct);
        var sigs = await db.DocumentSignatures.AsNoTracking()
            .Where(s => s.DocumentType == "RecruitmentOffer" && s.DocumentId == row.Id.ToString() && s.Status == "Signed")
            .ToListAsync(ct);

        var appointeeSig = sigs.FirstOrDefault(s => s.SlotKey == "AppointeeAcceptance");
        var agencySig = sigs.FirstOrDefault(s => s.SlotKey == "AgencyIssuer");

        var (effectiveBasicSalary, positionAllowance, otherAllowancesSummary) = await LoadMatrixRemunerationAsync(db, row.JobMatrixId, row.JobMatrixStepId, row.BasicSalary, ct);

        return Ok(new JobOfferLetterDetailDto(
            row.Id,
            row.Status,
            row.FullName,
            row.EfaasIdNumber,
            row.ApplicationCode,
            row.PositionTitle,
            effectiveBasicSalary,
            row.AnnouncementNumber,
            row.AgencyName,
            row.AgencyNameDhivehi,
            row.OfferedAtUtc,
            row.AcceptedAtUtc,
            row.TermsSnapshotJson,
            agencySig?.SignerName ?? row.IssuedByName,
            appointeeSig?.SignatureImageDataUrl,
            appointeeSig?.VerificationCode,
            agencySig?.SignatureImageDataUrl,
            agencySig?.VerificationCode,
            agencySig?.SignedAtUtc,
            positionAllowance,
            row.AttendanceAllowanceDaily,
            otherAllowancesSummary,
            row.GradeCode,
            row.EmploymentType));
    }

    private static async Task<(decimal? BasicSalary, decimal? PositionAllowance, string? OtherAllowancesSummary)> LoadMatrixRemunerationAsync(
        HrmTeContext db, int? jobMatrixId, int? jobMatrixStepId, decimal? fallbackBasicSalary, CancellationToken ct)
    {
        decimal? basic = fallbackBasicSalary;
        decimal? posAllowance = null;
        string? otherSummary = null;

        if (!jobMatrixId.HasValue) return (basic, posAllowance, otherSummary);

        var step = jobMatrixStepId.HasValue
            ? await db.FrameworkJobMatrixSteps.AsNoTracking()
                .Where(s => s.JobMatrixStepId == jobMatrixStepId.Value && s.IsActive)
                .Select(s => new { s.BasicSalaryOverride, s.JobAllowance })
                .FirstOrDefaultAsync(ct)
            : await db.FrameworkJobMatrixSteps.AsNoTracking()
                .Where(s => s.JobMatrixId == jobMatrixId.Value && s.IsActive)
                .OrderBy(s => s.StepNumber)
                .Select(s => new { s.BasicSalaryOverride, s.JobAllowance })
                .FirstOrDefaultAsync(ct);

        if (step is not null)
        {
            if (step.BasicSalaryOverride is > 0) basic = step.BasicSalaryOverride;
            if (step.JobAllowance > 0) posAllowance = step.JobAllowance;
        }

        var benefits = await db.FrameworkJobMatrixBenefits.AsNoTracking()
            .Where(b => b.JobMatrixId == jobMatrixId.Value && !string.IsNullOrWhiteSpace(b.BenefitName))
            .OrderBy(b => b.JobMatrixBenefitId)
            .Select(b => new { b.BenefitName, b.Amount, b.FrequencyCode })
            .ToListAsync(ct);

        if (benefits.Count > 0)
        {
            otherSummary = string.Join(", ", benefits.Select(b =>
                b.Amount is > 0
                    ? $"{b.BenefitName}: MVR {b.Amount.Value:N2}{(string.IsNullOrWhiteSpace(b.FrequencyCode) ? "" : $" ({b.FrequencyCode})")}"
                    : b.BenefitName));
        }

        return (basic, posAllowance, otherSummary);
    }
}
