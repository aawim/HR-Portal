using System.Security.Claims;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/my-workspace")]
[AllowMissingEmployee]
public sealed class RecruitmentApplicantWorkspaceController(IDbContextFactory<HrmTeContext> factory,global::HRM.Application.Contracts.Recruitment.IRecruitmentOfferService offers,IRecruitmentApplicationService applicationsService,IRecruitmentResultService resultsService):HrmApiController
{
    [HttpGet]
    public async Task<ActionResult<MyRecruitmentWorkspace>> Load(CancellationToken ct)
    {
        var identity=User.FindFirstValue("idnumber")?.Trim();
        if(string.IsNullOrWhiteSpace(identity))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        var rawApps=await(from a in db.RecruitmentApplications.AsNoTracking() join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id join v in db.RecruitmentVacancies.AsNoTracking() on a.RecruitmentVacancyId equals v.Id where person.EfaasIdNumber==identity orderby a.SubmittedAtUtc descending select new { a.Id, a.Reference, VacancyReference=v.Reference, a.Status, a.SubmittedAtUtc, a.RecruitmentVacancyId }).ToListAsync(ct);
        var appVacancyIds=rawApps.Select(x=>x.RecruitmentVacancyId).Distinct().ToList();
        var screenedVacancies=appVacancyIds.Count==0?new HashSet<int>():(await(from app in db.RecruitmentApplications.AsNoTracking()
            join dec in db.RecruitmentScreeningDecisions.AsNoTracking() on app.Id equals dec.RecruitmentApplicationId
            where appVacancyIds.Contains(app.RecruitmentVacancyId)
            select app.RecruitmentVacancyId).Distinct().ToListAsync(ct)).ToHashSet();
        var returnedIds=rawApps.Where(x=>x.Status=="ReturnedForCorrection").Select(x=>x.Id).ToList();
        var returnEvents=returnedIds.Count==0?[]:await db.Set<HRM.Models.Recruitment.RecruitmentApplicationEvent>().AsNoTracking()
            .Where(e=>returnedIds.Contains(e.ApplicationId)&&e.Kind=="ReturnedForCorrection")
            .OrderByDescending(e=>e.CreatedAtUtc)
            .ToListAsync(ct);
        var applications=rawApps.Select(a=>
        {
            string? comment=null;
            DateTime? deadline=null;
            if(a.Status=="ReturnedForCorrection")
            {
                var evt=returnEvents.FirstOrDefault(e=>e.ApplicationId==a.Id);
                if(evt is not null)
                {
                    comment=evt.Message;
                    if(!string.IsNullOrWhiteSpace(evt.SnapshotJson))
                    {
                        try
                        {
                            using var doc=System.Text.Json.JsonDocument.Parse(evt.SnapshotJson);
                            if(doc.RootElement.TryGetProperty("Comment",out var c))comment=c.GetString()??comment;
                            if(doc.RootElement.TryGetProperty("CorrectionDeadlineUtc",out var d)&&d.ValueKind==System.Text.Json.JsonValueKind.String&&DateTime.TryParse(d.GetString(),null,System.Globalization.DateTimeStyles.RoundtripKind,out var parsed))
                                deadline=parsed.Kind==DateTimeKind.Utc?parsed:DateTime.SpecifyKind(parsed,DateTimeKind.Utc);
                        }
                        catch(System.Text.Json.JsonException){}
                    }
                }
            }
            return new MyRecruitmentApplicationRow(a.Reference,a.VacancyReference,a.Status,a.SubmittedAtUtc,a.RecruitmentVacancyId,comment,deadline,screenedVacancies.Contains(a.RecruitmentVacancyId));
        }).ToList();
        var rawOffers=await(from o in db.RecruitmentOffers.AsNoTracking()
            join m in db.RecruitmentMeritResults.AsNoTracking() on o.RecruitmentMeritResultId equals m.Id
            join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id
            join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id
            join v in db.RecruitmentVacancies.AsNoTracking() on a.RecruitmentVacancyId equals v.Id
            join org in db.Organisations.AsNoTracking() on v.OrganisationId equals org.BusinessEntityID into orgs
            from org in orgs.DefaultIfEmpty()
            join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
            from pm in pms.DefaultIfEmpty()
            join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
            from jt in jts.DefaultIfEmpty()
            join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
            from jm in jms.DefaultIfEmpty()
            where person.EfaasIdNumber==identity
            orderby o.OfferedAtUtc descending
            select new { o.Id, VacancyReference=v.Reference, o.Status, o.TermsSnapshotJson, PositionName=jt!=null?jt.Name:(jm!=null?jm.Name:null), OrganisationName=org!=null?org.OrganisationName:null, o.OfferedAtUtc, o.AcceptedAtUtc }).ToListAsync(ct);

        var offerIds=rawOffers.Select(x=>x.Id.ToString()).ToList();
        var offerSigs=new Dictionary<string,(string Img,string Code)>();
        if(offerIds.Count>0)
        {
            await DocumentSignaturesController.EnsureTablesPublicAsync(db,ct);
            var sigs=await db.DocumentSignatures.AsNoTracking()
                .Where(s=>s.DocumentType=="RecruitmentOffer"&&s.SlotKey=="AppointeeAcceptance"&&s.Status=="Signed"&&offerIds.Contains(s.DocumentId))
                .Select(s=>new{s.DocumentId,s.SignatureImageDataUrl,s.VerificationCode})
                .ToListAsync(ct);
            foreach(var s in sigs) offerSigs[s.DocumentId]=(s.SignatureImageDataUrl,s.VerificationCode);
        }

        var mine=rawOffers.Select(o=>
        {
            offerSigs.TryGetValue(o.Id.ToString(),out var sig);
            return new MyRecruitmentOfferRow(o.Id,o.VacancyReference,o.Status,o.TermsSnapshotJson,o.PositionName,o.OrganisationName,o.OfferedAtUtc,o.AcceptedAtUtc,sig.Img,sig.Code);
        }).ToList();
        var rawResults=await(from a in db.RecruitmentApplications.AsNoTracking()
            join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id
            join p in db.RecruitmentResultPublications.AsNoTracking() on a.RecruitmentVacancyId equals p.RecruitmentVacancyId
            join v in db.RecruitmentVacancies.AsNoTracking() on p.RecruitmentVacancyId equals v.Id
            join m in db.RecruitmentMeritResults.AsNoTracking()
                on new { PubId = p.Id, AppId = a.Id } equals new { PubId = m.RecruitmentResultPublicationId, AppId = m.RecruitmentApplicationId } into ms
            from m in ms.DefaultIfEmpty()
            join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
            from pm in pms.DefaultIfEmpty()
            join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
            from jt in jts.DefaultIfEmpty()
            join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
            from jm in jms.DefaultIfEmpty()
            where person.EfaasIdNumber==identity
            orderby p.Id descending
            select new { p.Id, ApplicationId=a.Id, ApplicationReference=a.Reference, ApplicationStatus=a.Status, VacancyReference=v.Reference, PositionName=jt!=null?jt.Name:(jm!=null?jm.Name:null), Rank=(int?)m.Rank, Score=(decimal?)m.Score, p.IsFinal, p.PublishedAtUtc, p.ObjectionClosesAtUtc }).ToListAsync(ct);
        var pubIds=rawResults.Select(x=>x.Id).Distinct().ToList();
        var appIds=rawResults.Select(x=>x.ApplicationId).Distinct().ToList();
        var myObjections=pubIds.Count==0?[]:await db.RecruitmentObjections.AsNoTracking()
            .Where(o=>pubIds.Contains(o.RecruitmentResultPublicationId)&&appIds.Contains(o.RecruitmentApplicationId))
            .ToListAsync(ct);
        var results=rawResults.Select(r=>
        {
            var obj=myObjections.FirstOrDefault(o=>o.RecruitmentResultPublicationId==r.Id&&o.RecruitmentApplicationId==r.ApplicationId);
            return new MyRecruitmentResultRow(r.Id,r.ApplicationId,r.ApplicationReference,r.VacancyReference,r.PositionName,r.ApplicationStatus,r.Rank,r.Score,r.IsFinal,r.PublishedAtUtc,r.ObjectionClosesAtUtc,obj?.Id,obj?.Status,obj?.Grounds,obj?.Resolution);
        }).ToList();
        var applicantId=await db.RecruitmentApplicants.Where(x=>x.EfaasIdNumber==identity&&x.Status=="Active").Select(x=>(int?)x.Id).SingleOrDefaultAsync(ct);
        var userRecord=await db.Users.AsNoTracking()
            .Where(u=>u.Username==identity&&u.IsActive)
            .Select(u=>new{u.BusinessEntityID,u.FullName,u.Email,u.Mobile})
            .FirstOrDefaultAsync(ct);
        string? profileFullName=null;
        if(userRecord!=null&&userRecord.BusinessEntityID>0)
        {
            profileFullName=await db.Individuals.AsNoTracking()
                .Where(i=>i.BusinessEntityId==userRecord.BusinessEntityID)
                .Select(i=>((i.FirstNameEnglish??"")+" "+(i.MiddleNameEnglish??"")+" "+(i.LastNameEnglish??"")).Trim())
                .FirstOrDefaultAsync(ct);
        }
        if(string.IsNullOrWhiteSpace(profileFullName))
        {
            profileFullName=await db.Idcards.AsNoTracking()
                .Where(c=>c.IdcardNumber==identity)
                .Select(c=>((c.BusinessEntity.FirstNameEnglish??"")+" "+(c.BusinessEntity.MiddleNameEnglish??"")+" "+(c.BusinessEntity.LastNameEnglish??"")).Trim())
                .FirstOrDefaultAsync(ct);
        }
        profileFullName=string.IsNullOrWhiteSpace(profileFullName)
            ?(!string.IsNullOrWhiteSpace(userRecord?.FullName)?userRecord.FullName.Trim():User.FindFirstValue("name")?.Trim())
            :System.Text.RegularExpressions.Regex.Replace(profileFullName,@"\s+"," ").Trim();
        return Ok(new MyRecruitmentWorkspace(applications,mine,applicantId,results,profileFullName,userRecord?.Email,userRecord?.Mobile));
    }
    [HttpGet("results/{publicationId:int}/{applicationId:int}")]
    public async Task<ActionResult<MyPublishedResultDetail>> ResultDetail(int publicationId,int applicationId,CancellationToken ct)
    {
        var identity=User.FindFirstValue("idnumber")?.Trim();
        if(string.IsNullOrWhiteSpace(identity))return Forbid();
        await using var db=await factory.CreateDbContextAsync(ct);
        var appInfo=await(from a in db.RecruitmentApplications.AsNoTracking()
            join person in db.RecruitmentApplicants.AsNoTracking() on a.RecruitmentApplicantId equals person.Id
            join p in db.RecruitmentResultPublications.AsNoTracking() on a.RecruitmentVacancyId equals p.RecruitmentVacancyId
            join v in db.RecruitmentVacancies.AsNoTracking() on p.RecruitmentVacancyId equals v.Id
            join pm in db.PositionMatrices.AsNoTracking() on v.PositionMatrixId equals pm.PositionMatrixId into pms
            from pm in pms.DefaultIfEmpty()
            join jt in db.FrameworkJobTitles.AsNoTracking() on pm.JobTitleId equals jt.JobTitleId into jts
            from jt in jts.DefaultIfEmpty()
            join jm in db.FrameworkJobMatrices.AsNoTracking() on pm.JobMatrixId equals jm.JobMatrixId into jms
            from jm in jms.DefaultIfEmpty()
            where p.Id==publicationId&&a.Id==applicationId&&person.EfaasIdNumber==identity
            select new { p.Id, a.Reference, a.Status, VacancyId=v.Id, VacancyReference=v.Reference, PositionName=jt!=null?jt.Name:(jm!=null?jm.Name:null), p.IsFinal, p.PublishedAtUtc, p.ObjectionClosesAtUtc }).SingleOrDefaultAsync(ct);
        if(appInfo is null)return NotFound();

        var sheet=await(from m in db.RecruitmentMeritResults.AsNoTracking()
            join a in db.RecruitmentApplications.AsNoTracking() on m.RecruitmentApplicationId equals a.Id
            where m.RecruitmentResultPublicationId==publicationId
            orderby m.Rank
            select new PublishedMeritSheetEntry(m.Rank,a.Reference,m.Score,a.Id==applicationId)).ToListAsync(ct);

        var mine=sheet.FirstOrDefault(x=>x.IsMine);

        var scorecards=await(from s in db.RecruitmentPanelScorecards.AsNoTracking()
            join assess in db.RecruitmentAssessments.AsNoTracking() on s.RecruitmentAssessmentId equals assess.Id
            where s.RecruitmentApplicationId==applicationId&&assess.RecruitmentVacancyId==appInfo.VacancyId&&s.IsLocked
            orderby s.SubmittedAtUtc
            select new MyScorecardBreakdownRow(assess.Type,s.TotalScore,s.CriterionScoresJson,s.SubmittedAtUtc)).ToListAsync(ct);

        var obj=await db.RecruitmentObjections.AsNoTracking()
            .FirstOrDefaultAsync(o=>o.RecruitmentResultPublicationId==publicationId&&o.RecruitmentApplicationId==applicationId,ct);

        return Ok(new MyPublishedResultDetail(
            publicationId,
            applicationId,
            appInfo.Reference,
            appInfo.VacancyReference,
            appInfo.PositionName,
            appInfo.Status,
            mine?.Rank,
            mine?.Score,
            appInfo.IsFinal,
            appInfo.PublishedAtUtc,
            appInfo.ObjectionClosesAtUtc,
            sheet,
            scorecards,
            obj?.Id,
            obj?.Status,
            obj?.Grounds,
            obj?.Resolution));
    }
    [HttpPost("objections")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitObjection([FromBody]SubmitRecruitmentObjectionInput input,CancellationToken ct)=>Ok(await resultsService.SubmitObjectionAsync(input,ct));
    [HttpPost("offers/{id:int}/accept")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Accept(int id,[FromBody]AcceptRecruitmentOfferInput? input,CancellationToken ct)=>Ok(await offers.AcceptMyOfferAsync(id,input,ct));
    [HttpPost("offers/{id:int}/decline")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Decline(int id,[FromBody]DeclineRecruitmentOfferInput? input,CancellationToken ct)=>Ok(await offers.DeclineMyOfferAsync(id,input?.Reason,ct));
    [HttpPost("apply")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Apply([FromBody]ApplyToRecruitmentVacancyInput input,CancellationToken ct)
    {
        var identity=User.FindFirstValue("idnumber")?.Trim();
        if(string.IsNullOrWhiteSpace(identity))return Forbid();
        if(string.IsNullOrWhiteSpace(input.Qualifications)||input.Qualifications.Length>4000||string.IsNullOrWhiteSpace(input.Experience)||input.Experience.Length>4000||string.IsNullOrWhiteSpace(input.SupportingStatement)||input.SupportingStatement.Length>4000)return BadRequest();
        await using var db=await factory.CreateDbContextAsync(ct);
        var person=await db.RecruitmentApplicants.AsNoTracking().SingleOrDefaultAsync(x=>x.EfaasIdNumber==identity&&x.Status=="Active",ct);
        if(person is null)return Ok(global::HRM.DTOs.ServiceResult.Fail("Register your applicant profile before applying."));
        var snapshot=System.Text.Json.JsonSerializer.Serialize(new{person.FullName,person.Email,person.Mobile,input.Qualifications,input.Experience,input.SupportingStatement});
        return Ok(await applicationsService.SubmitAsync(new(person.Id,input.VacancyId,snapshot),ct));
    }
}
