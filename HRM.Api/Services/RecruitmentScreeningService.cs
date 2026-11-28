using HRM.Application.Contracts.Recruitment;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Recruitment;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class RecruitmentScreeningService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs) : IRecruitmentScreeningService
{
    async Task<(int Org, int User)> Auth()
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        var actor = await access.RequireContextAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !await access.HasPermissionAsync("Recruitment.Manage"))
            throw new UnauthorizedAccessException("Recruitment management permission is required.");
        return (org, actor.UserId);
    }

    public async Task<ServiceResult> RecordAsync(RecordScreeningInput i, CancellationToken ct = default)
    {
        if (i.ApplicationId <= 0 || string.IsNullOrWhiteSpace(i.DecisionReason) || i.DecisionReason.Length > 2000 || string.IsNullOrWhiteSpace(i.EvidenceSnapshotJson) || i.EvidenceSnapshotJson.Length > 16000)
            return ServiceResult.Fail("A screening decision, reason and evidence snapshot are required.");

        var (o, u) = await Auth();
        await using var d = await factory.CreateDbContextAsync(ct);
        var a = await d.RecruitmentApplications.SingleOrDefaultAsync(x => x.Id == i.ApplicationId && x.OrganisationId == o, ct)
            ?? throw new UnauthorizedAccessException();

        if (a.Status == "EligibleNotShortlisted")
        {
            if (!i.IsEligible || !i.IsShortlisted)
                return ServiceResult.Fail("To proceed with an EligibleNotShortlisted application, select 'Shortlist for assessment'.");

            var existingDecision = await d.RecruitmentScreeningDecisions.SingleOrDefaultAsync(x => x.RecruitmentApplicationId == a.Id, ct);
            var promoteLog = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Eligible candidate promoted to Shortlisted for {a.Reference}.");

            if (existingDecision is not null)
            {
                existingDecision.IsEligible = true;
                existingDecision.IsShortlisted = true;
                var combinedReason = string.IsNullOrWhiteSpace(existingDecision.DecisionReason)
                    ? i.DecisionReason.Trim()
                    : $"{existingDecision.DecisionReason}\n[Shortlisted]: {i.DecisionReason.Trim()}";
                existingDecision.DecisionReason = combinedReason.Length <= 2000 ? combinedReason : combinedReason[..2000];
                existingDecision.EvidenceSnapshotJson = i.EvidenceSnapshotJson;
                existingDecision.DecidedByUserId = u;
                existingDecision.DecidedAtUtc = DateTime.UtcNow;
                existingDecision.OperationLogId = promoteLog.OperationLogId;
            }
            else
            {
                d.RecruitmentScreeningDecisions.Add(new RecruitmentScreeningDecision
                {
                    RecruitmentApplicationId = a.Id,
                    RecruitmentVacancyId = a.RecruitmentVacancyId,
                    OrganisationId = o,
                    IsEligible = true,
                    IsShortlisted = true,
                    DecisionReason = i.DecisionReason.Trim(),
                    EvidenceSnapshotJson = i.EvidenceSnapshotJson,
                    DecidedByUserId = u,
                    DecidedAtUtc = DateTime.UtcNow,
                    IsLocked = true,
                    OperationLogId = promoteLog.OperationLogId
                });
            }

            a.Status = "Shortlisted";
            await d.SaveChangesAsync(ct);
            return ServiceResult.Ok("Candidate shortlisted for assessment.");
        }

        if (a.Status != "Submitted")
            return ServiceResult.Fail($"Applications in '{a.Status}' status cannot be screened.");

        if (await d.RecruitmentScreeningDecisions.AnyAsync(x => x.RecruitmentApplicationId == a.Id, ct))
            return ServiceResult.Fail("Screening is already locked for this application.");

        var l = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Recruitment screening recorded for {a.Reference}.");
        d.RecruitmentScreeningDecisions.Add(new RecruitmentScreeningDecision
        {
            RecruitmentApplicationId = a.Id,
            RecruitmentVacancyId = a.RecruitmentVacancyId,
            OrganisationId = o,
            IsEligible = i.IsEligible,
            IsShortlisted = i.IsEligible && i.IsShortlisted,
            DecisionReason = i.DecisionReason.Trim(),
            EvidenceSnapshotJson = i.EvidenceSnapshotJson,
            DecidedByUserId = u,
            DecidedAtUtc = DateTime.UtcNow,
            IsLocked = true,
            OperationLogId = l.OperationLogId
        });
        a.Status = i.IsEligible && i.IsShortlisted ? "Shortlisted" : i.IsEligible ? "EligibleNotShortlisted" : "Ineligible";
        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok("Screening decision locked with its evidence snapshot.");
    }

    public async Task<ServiceResult> DeclareConflictAsync(DeclareRecruitmentConflictInput i, CancellationToken ct = default)
    {
        if (i.VacancyId <= 0 || string.IsNullOrWhiteSpace(i.Declaration) || i.Declaration.Length > 2000 || i.Mitigation?.Length > 2000)
            return ServiceResult.Fail("Provide a conflict declaration.");

        var (o, u) = await Auth();
        await using var d = await factory.CreateDbContextAsync(ct);
        if (!await d.RecruitmentVacancies.AnyAsync(x => x.Id == i.VacancyId && x.OrganisationId == o, ct))
            throw new UnauthorizedAccessException();

        var existing = await d.RecruitmentPanelConflictDeclarations.SingleOrDefaultAsync(x => x.RecruitmentVacancyId == i.VacancyId && x.DeclaredByUserId == u, ct);
        if (existing is not null)
        {
            var hasScored = await (from s in d.RecruitmentPanelScorecards
                                   join a in d.RecruitmentAssessments on s.RecruitmentAssessmentId equals a.Id
                                   where a.RecruitmentVacancyId == i.VacancyId && s.ScorerUserId == u
                                   select s.Id).AnyAsync(ct);
            if (hasScored)
                return ServiceResult.Fail("Your conflict declaration cannot be changed after submitting a scorecard for this vacancy.");

            var updateLog = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Recruitment conflict declaration updated for vacancy {i.VacancyId} (HasConflict={i.HasConflict}).");
            existing.HasConflict = i.HasConflict;
            existing.Declaration = i.Declaration.Trim();
            existing.Mitigation = i.Mitigation?.Trim();
            existing.DeclaredAtUtc = DateTime.UtcNow;
            existing.OperationLogId = updateLog.OperationLogId;
            await d.SaveChangesAsync(ct);
            return ServiceResult.Ok(i.HasConflict ? "Conflict declaration updated (conflict declared)." : "Conflict declaration updated (no conflict).");
        }

        var l = await logs.CreateAsync(d, SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"Recruitment conflict declaration recorded for vacancy {i.VacancyId}.");
        d.RecruitmentPanelConflictDeclarations.Add(new RecruitmentPanelConflictDeclaration
        {
            RecruitmentVacancyId = i.VacancyId,
            OrganisationId = o,
            DeclaredByUserId = u,
            HasConflict = i.HasConflict,
            Declaration = i.Declaration.Trim(),
            Mitigation = i.Mitigation?.Trim(),
            DeclaredAtUtc = DateTime.UtcNow,
            OperationLogId = l.OperationLogId
        });
        await d.SaveChangesAsync(ct);
        return ServiceResult.Ok("Conflict declaration recorded.");
    }
}
