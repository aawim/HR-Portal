using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/result-workspace")]
public sealed class RecruitmentResultQueriesController(IDbContextFactory<HrmTeContext> factory,IUserAccessService access):HrmApiController
{
    [HttpGet("{vacancyId:int}")]
    public async Task<ActionResult<RecruitmentResultWorkspace>> Load(int vacancyId,CancellationToken ct)
    {
        var actor=await access.RequireContextAsync();
        var org=await access.GetCurrentOrganisationIdAsync();
        var canView=actor.IsSuperAdministrator||await access.HasPermissionAsync("Recruitment.Manage")||await access.HasPermissionAsync("Recruitment.View")||await access.HasPermissionAsync("Admin");
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!canView)return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        if(!await db.RecruitmentVacancies.AnyAsync(x=>x.Id==vacancyId&&x.OrganisationId==org,ct))return NotFound();
        var p=await db.RecruitmentResultPublications.AsNoTracking().SingleOrDefaultAsync(x=>x.RecruitmentVacancyId==vacancyId&&x.OrganisationId==org,ct);
        if(p is null)return Ok(new RecruitmentResultWorkspace(null,false,null,[],[]));
        var results=await(from m in db.RecruitmentMeritResults.AsNoTracking()
            join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id into apps
            from a in apps.DefaultIfEmpty()
            join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id into people
            from person in people.DefaultIfEmpty()
            where m.RecruitmentResultPublicationId==p.Id
            orderby m.Rank
            select new RecruitmentResultRow(m.RecruitmentApplicationId,m.Score,m.Rank,a!=null?a.Reference:null,person!=null?person.FullName:null)).ToListAsync(ct);
        var objections=await db.RecruitmentObjections.AsNoTracking().Where(x=>x.RecruitmentResultPublicationId==p.Id).Select(x=>new RecruitmentObjectionRow(x.Id,x.RecruitmentApplicationId,x.Grounds,x.Status,x.Resolution)).ToListAsync(ct);
        return Ok(new RecruitmentResultWorkspace(p.Id,p.IsFinal,p.ObjectionClosesAtUtc,results,objections));
    }

    [HttpGet("{vacancyId:int}/a2-sheet")]
    public async Task<ActionResult<A2EvaluationSheetDto>> A2Sheet(int vacancyId, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        var canView = actor.IsSuperAdministrator || await access.HasPermissionAsync("Recruitment.Manage") || await access.HasPermissionAsync("Recruitment.View") || await access.HasPermissionAsync("Admin");
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !canView) return Forbid();
        await using var db = await factory.CreateDbContextAsync(ct);
        var dto = await BuildA2SheetAsync(db, vacancyId, org, anonymizeNames: false, requirePublished: false, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    internal static async Task<A2EvaluationSheetDto?> BuildA2SheetAsync(
        HrmTeContext db,
        int vacancyId,
        int? expectedOrgId,
        bool anonymizeNames,
        bool requirePublished,
        CancellationToken ct)
    {
        if (requirePublished && !await db.RecruitmentResultPublications.AsNoTracking().AnyAsync(p => p.RecruitmentVacancyId == vacancyId, ct))
            return null;

        var vacancy = await (from v in db.RecruitmentVacancies.AsNoTracking()
                             join o in db.Organisations.AsNoTracking() on v.OrganisationId equals o.BusinessEntityID into os
                             from o in os.DefaultIfEmpty()
                             join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
                             from pm in pms.DefaultIfEmpty()
                             join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
                             from jt in jts.DefaultIfEmpty()
                             join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
                             from jm in jms.DefaultIfEmpty()
                             where v.Id == vacancyId && (!expectedOrgId.HasValue || v.OrganisationId == expectedOrgId.Value)
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

        if (vacancy is null) return null;

        string positionTitle = vacancy.FallbackTitle;
        string? basicReqs = null;
        if (!string.IsNullOrWhiteSpace(vacancy.AdvertisementSnapshotJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(vacancy.AdvertisementSnapshotJson);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("Title", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(t.GetString()))
                        positionTitle = t.GetString()!.Trim();
                    foreach (var propName in new[] { "Eligibility", "Requirements", "BasicRequirements", "Criteria" })
                    {
                        if (doc.RootElement.TryGetProperty(propName, out var reqProp) && reqProp.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(reqProp.GetString()))
                        {
                            basicReqs = reqProp.GetString()!.Trim();
                            break;
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException) { }
        }

        var apps = await (from a in db.RecruitmentApplications.AsNoTracking()
                          join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id
                          join dec in db.RecruitmentScreeningDecisions.AsNoTracking() on a.Id equals dec.RecruitmentApplicationId into decs
                          from dec in decs.DefaultIfEmpty()
                          where a.RecruitmentVacancyId == vacancyId && a.Status != "Draft" && a.Status != "Withdrawn"
                          orderby a.SubmittedAtUtc, a.Id
                          select new
                          {
                              a.Id,
                              a.Reference,
                              person.FullName,
                              a.Status,
                              HasDecision = dec != null,
                              IsEligible = dec != null && dec.IsEligible
                          }).ToListAsync(ct);

        var scorecards = await (from s in db.RecruitmentPanelScorecards.AsNoTracking()
                                join assess in db.RecruitmentAssessments.AsNoTracking() on s.RecruitmentAssessmentId equals assess.Id
                                where assess.RecruitmentVacancyId == vacancyId && s.IsLocked
                                orderby s.SubmittedAtUtc
                                select new
                                {
                                    s.RecruitmentApplicationId,
                                    s.ScorerUserId,
                                    s.CriterionScoresJson,
                                    s.TotalScore,
                                    assess.Type
                                }).ToListAsync(ct);

        var scorecardsByApp = scorecards.GroupBy(x => x.RecruitmentApplicationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<A2EvaluationCandidateRow>(apps.Count);
        foreach (var a in apps)
        {
            var meets = a.HasDecision ? a.IsEligible : a.Status is "Shortlisted" or "EligibleNotShortlisted";
            decimal basicPts = 0, corePts = 0, higherPts = 0, expPts = 0;
            decimal? directExam = null, directInterview = null;
            decimal? p1 = null, p2 = null, p3 = null;

            if (scorecardsByApp.TryGetValue(a.Id, out var scList) && scList.Count > 0)
            {
                var basicVals = new List<decimal>();
                var coreVals = new List<decimal>();
                var higherVals = new List<decimal>();
                var expVals = new List<decimal>();
                var examVals = new List<decimal>();
                var interviewVals = new List<decimal>();

                foreach (var sc in scList)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(sc.CriterionScoresJson);
                        if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                        if (TryReadDecimal(doc.RootElement, "basicQualification", out var vBasic)) basicVals.Add(vBasic);
                        if (TryReadDecimal(doc.RootElement, "coreDiscipline", out var vCore)) coreVals.Add(vCore);
                        if (TryReadDecimal(doc.RootElement, "higherQualification", out var vHigher)) higherVals.Add(vHigher);
                        if (TryReadDecimal(doc.RootElement, "additionalExperience", out var vExp)) expVals.Add(vExp);
                        if (TryReadDecimal(doc.RootElement, "assessment", out var vExam)) examVals.Add(vExam);
                        if (TryReadDecimal(doc.RootElement, "interview", out var vInt)) interviewVals.Add(vInt);
                    }
                    catch (System.Text.Json.JsonException) { }
                }

                if (basicVals.Count > 0) basicPts = Math.Min(30m, Math.Round(basicVals.Average(), 2));
                if (coreVals.Count > 0) corePts = Math.Min(5m, Math.Round(coreVals.Average(), 2));
                if (higherVals.Count > 0) higherPts = Math.Min(5m, Math.Round(higherVals.Average(), 2));
                if (expVals.Count > 0) expPts = Math.Min(5m, Math.Round(expVals.Average(), 2));
                if (examVals.Count > 0) directExam = Math.Min(20m, Math.Round(examVals.Average(), 2));
                if (interviewVals.Count > 0)
                {
                    directInterview = Math.Min(35m, Math.Round(interviewVals.Average(), 2));
                    // Also populate panelist raw equivalent (/75) if up to 3 panelists scored
                    if (interviewVals.Count >= 1) p1 = Math.Round(interviewVals[0] * 75m / 35m, 2);
                    if (interviewVals.Count >= 2) p2 = Math.Round(interviewVals[1] * 75m / 35m, 2);
                    if (interviewVals.Count >= 3) p3 = Math.Round(interviewVals[2] * 75m / 35m, 2);
                }
            }

            rows.Add(new A2EvaluationCandidateRow(
                a.Id,
                a.Reference,
                anonymizeNames ? a.Reference : a.FullName,
                meets,
                basicPts,
                corePts,
                higherPts,
                "",
                expPts,
                p1,
                p2,
                p3,
                directExam.HasValue ? Math.Round(directExam.Value * 50m / 20m, 2) : null,
                directExam,
                directInterview));
        }

        return new A2EvaluationSheetDto(
            vacancy.Id,
            vacancy.Reference,
            positionTitle,
            vacancy.BasicSalary,
            vacancy.ClosingAtUtc,
            basicReqs,
            vacancy.AgencyName,
            vacancy.AgencyNameDhivehi,
            rows);
    }

    private static bool TryReadDecimal(System.Text.Json.JsonElement root, string key, out decimal value)
    {
        value = 0;
        if (root.TryGetProperty(key, out var prop) && prop.TryGetDecimal(out var v))
        {
            value = v;
            return true;
        }
        return false;
    }
}
