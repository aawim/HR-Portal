using System.Security.Claims;
using System.Text.Json;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Recruitment;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/my-applications")]
[AllowMissingEmployee]
public sealed class RecruitmentLifecycleController(IDbContextFactory<HrmTeContext> factory,IOperationLogService logs,IDocumentStorageProvider storage):HrmApiController
{
    private string Identity()=>User.Identity?.IsAuthenticated==true?User.FindFirstValue("idnumber")?.Trim()??throw new UnauthorizedAccessException():throw new UnauthorizedAccessException();
    private async Task<RecruitmentApplication> Own(HrmTeContext db,int id,CancellationToken ct)
    {
        var identity=Identity();
        return await(from a in db.RecruitmentApplications join p in db.RecruitmentApplicants on a.RecruitmentApplicantId equals p.Id where a.Id==id&&p.EfaasIdNumber==identity&&p.Status=="Active" select a).SingleOrDefaultAsync(ct)??throw new UnauthorizedAccessException();
    }
    private static void Event(HrmTeContext db,RecruitmentApplication app,string kind,string message)=>db.Set<RecruitmentApplicationEvent>().Add(new(){ApplicationId=app.Id,Kind=kind,Message=message,SnapshotJson=app.ProfileSnapshotJson,CreatedAtUtc=DateTime.UtcNow});
    private static async Task<bool> Open(HrmTeContext db,RecruitmentApplication app,CancellationToken ct)=>await db.RecruitmentVacancies.AnyAsync(x=>x.Id==app.RecruitmentVacancyId&&x.Status=="Published"&&x.ClosingAtUtc>DateTime.UtcNow,ct);
    private static async Task<(string? Comment, DateTime? DeadlineUtc)> GetLatestReturnAsync(HrmTeContext db, int applicationId, CancellationToken ct)
    {
        var evt = await db.Set<RecruitmentApplicationEvent>().AsNoTracking()
            .Where(x => x.ApplicationId == applicationId && x.Kind == "ReturnedForCorrection")
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (evt is null) return (null, null);
        if (string.IsNullOrWhiteSpace(evt.SnapshotJson)) return (evt.Message, null);
        try
        {
            using var doc = JsonDocument.Parse(evt.SnapshotJson);
            var root = doc.RootElement;
            string? comment = root.TryGetProperty("Comment", out var c) ? c.GetString() : evt.Message;
            DateTime? deadline = null;
            if (root.TryGetProperty("CorrectionDeadlineUtc", out var d) && d.ValueKind == JsonValueKind.String && DateTime.TryParse(d.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                deadline = parsed.Kind == DateTimeKind.Utc ? parsed : DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return (comment, deadline);
        }
        catch (JsonException)
        {
            return (evt.Message, null);
        }
    }
    private static async Task<bool> IsWithinCorrectionWindowAsync(HrmTeContext db, RecruitmentApplication app, CancellationToken ct)
    {
        if (app.Status != "ReturnedForCorrection") return false;
        var (_, deadlineUtc) = await GetLatestReturnAsync(db, app.Id, ct);
        return deadlineUtc.HasValue && deadlineUtc.Value > DateTime.UtcNow;
    }
    [HttpGet("vacancy/{vacancyId:int}")]
    public async Task<ActionResult<RecruitmentDraftDetail?>> Load(int vacancyId,CancellationToken ct)
    {
        var identity=Identity();await using var db=await factory.CreateDbContextAsync(ct);
        var app=await(from a in db.RecruitmentApplications join p in db.RecruitmentApplicants on a.RecruitmentApplicantId equals p.Id where a.RecruitmentVacancyId==vacancyId&&p.EfaasIdNumber==identity select a).SingleOrDefaultAsync(ct);
        if(app is null)return NoContent();
        var documents=await db.Set<RecruitmentApplicationDocument>().Where(x=>x.ApplicationId==app.Id).Select(x=>new RecruitmentDocumentRow(x.Id,x.Category,x.FileName)).ToListAsync(ct);
        var events=await db.Set<RecruitmentApplicationEvent>().Where(x=>x.ApplicationId==app.Id).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new RecruitmentEventRow(x.Kind,x.Message,x.CreatedAtUtc)).ToListAsync(ct);
        string? returnComment=null;
        DateTime? correctionDeadlineUtc=null;
        if(app.Status=="ReturnedForCorrection")
            (returnComment,correctionDeadlineUtc)=await GetLatestReturnAsync(db,app.Id,ct);
        return Ok(new RecruitmentDraftDetail(app.Id,vacancyId,app.Reference,app.Status,app.Version,app.ProfileSnapshotJson,documents,events,returnComment,correctionDeadlineUtc));
    }
    [HttpPost("draft")]
    public async Task<ActionResult<ServiceResult>> Save(SaveRecruitmentDraftInput input,CancellationToken ct)
    {
        if(input.Qualifications?.Length>4000||input.Experience?.Length>4000||input.SupportingStatement?.Length>4000)return BadRequest();
        var identity=Identity();await using var db=await factory.CreateDbContextAsync(ct);
        var person=await db.RecruitmentApplicants.SingleOrDefaultAsync(x=>x.EfaasIdNumber==identity&&x.Status=="Active",ct)??throw new UnauthorizedAccessException();
        var vacancy=await db.RecruitmentVacancies.SingleOrDefaultAsync(x=>x.Id==input.VacancyId&&x.Status=="Published",ct);
        if(vacancy is null)return Ok(ServiceResult.Fail("This vacancy is no longer available."));
        var app=await db.RecruitmentApplications.SingleOrDefaultAsync(x=>x.RecruitmentApplicantId==person.Id&&x.RecruitmentVacancyId==vacancy.Id,ct);
        var isReturned=app is not null&&app.Status=="ReturnedForCorrection";
        if(isReturned)
        {
            if(!await IsWithinCorrectionWindowAsync(db,app!,ct))
                return Ok(ServiceResult.Fail("The correction deadline granted by HR has expired."));
        }
        else if(vacancy.ClosingAtUtc<=DateTime.UtcNow)
        {
            return Ok(ServiceResult.Fail("This vacancy is closed."));
        }
        if(app is not null&&(app.Status is not ("Draft" or "Submitted" or "ReturnedForCorrection")||app.Version!=input.ExpectedVersion))return Ok(ServiceResult.Fail("Application changed or is locked. Reload before editing."));
        if(app is not null&&app.Status=="Submitted"&&await db.RecruitmentScreeningDecisions.AnyAsync(x=>x.RecruitmentApplicationId==app.Id,ct))return Ok(ServiceResult.Fail("Screening has begun; corrections are closed."));
        if(app is null){if(input.ExpectedVersion!=0)return Conflict();var log=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,"Recruitment draft created.");app=new(){Reference=$"RA-{Guid.NewGuid():N}"[..24],RecruitmentApplicantId=person.Id,RecruitmentVacancyId=vacancy.Id,OrganisationId=vacancy.OrganisationId,OperationLogId=log.OperationLogId};db.Add(app);await db.SaveChangesAsync(ct);}
        var correction=app.Status is "Submitted" or "ReturnedForCorrection";
        // Retain the previous submitted snapshot as evidence before replacing it.
        if(correction)Event(db,app,"PreviousSubmission","Previous submitted application retained before applicant correction.");
        app.ProfileSnapshotJson=JsonSerializer.Serialize(new{person.FullName,person.Email,person.Mobile,input.Qualifications,input.Experience,input.SupportingStatement});app.Version++;
        Event(db,app,correction?"CorrectionReceipt":"DraftSaved",correction?$"Correction saved for {app.Reference}.":$"Draft saved for {app.Reference}.");
        await db.SaveChangesAsync(ct);return Ok(ServiceResult.Ok(isReturned?"Correction draft saved. Upload any missing documents and click Resubmit Application before the deadline.":correction?"Correction saved and receipt recorded.":"Draft saved. You can resume it later."));
    }
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<ServiceResult>> Submit(int id,RecruitmentLifecycleActionInput input,CancellationToken ct)
    {
        await using var db=await factory.CreateDbContextAsync(ct);var app=await Own(db,id,ct);
        var wasReturned=app.Status=="ReturnedForCorrection";
        var canSubmit=wasReturned
            ? await IsWithinCorrectionWindowAsync(db,app,ct)
            : app.Status=="Draft"&&await Open(db,app,ct);
        if(!input.Confirmed||app.Version!=input.ExpectedVersion||!canSubmit)
            return Ok(ServiceResult.Fail(wasReturned?"The correction deadline has expired or the application changed. Reload and try again.":"Review the current draft before submitting while the vacancy is open."));
        using var json=JsonDocument.Parse(app.ProfileSnapshotJson);
        foreach(var field in new[]{"Qualifications","Experience","SupportingStatement"})if(!json.RootElement.TryGetProperty(field,out var value)||string.IsNullOrWhiteSpace(value.GetString()))return Ok(ServiceResult.Fail("Complete all application fields."));
        var categories=await db.Set<RecruitmentApplicationDocument>().Where(x=>x.ApplicationId==app.Id).Select(x=>x.Category).ToListAsync(ct);
        if(!categories.Contains("Identity")||!categories.Contains("Qualification"))return Ok(ServiceResult.Fail("Upload identity and qualification documents before submission."));
        app.Status="Submitted";app.SubmittedAtUtc=DateTime.UtcNow;app.Version++;
        Event(db,app,wasReturned?"ResubmittedAfterCorrection":"SubmissionReceipt",wasReturned?$"Application {app.Reference} corrected and resubmitted at {app.SubmittedAtUtc:O}.":$"Application {app.Reference} received at {app.SubmittedAtUtc:O}.");
        var applicantName=await db.RecruitmentApplicants.AsNoTracking().Where(x=>x.Id==app.RecruitmentApplicantId).Select(x=>x.FullName).SingleOrDefaultAsync(ct)??"An applicant";
        var vacancyRef=await db.RecruitmentVacancies.AsNoTracking().Where(x=>x.Id==app.RecruitmentVacancyId).Select(x=>x.Reference).SingleOrDefaultAsync(ct)??$"#{app.RecruitmentVacancyId}";
        await HRM.Services.RecruitmentNotificationHelper.NotifyHrApplicationSubmittedAsync(db,app.OrganisationId,app.Reference,vacancyRef,applicantName,wasReturned,ct);
        await db.SaveChangesAsync(ct);return Ok(ServiceResult.Ok(wasReturned?$"Corrected application resubmitted. Receipt: {app.Reference}":$"Application submitted. Receipt: {app.Reference}"));
    }
    [HttpPost("{id:int}/withdraw")]
    public async Task<ActionResult<ServiceResult>> Withdraw(int id,RecruitmentLifecycleActionInput input,CancellationToken ct)
    {
        await using var db=await factory.CreateDbContextAsync(ct);var app=await Own(db,id,ct);
        if(!input.Confirmed||app.Version!=input.ExpectedVersion||app.Status is not ("Draft" or "Submitted")||!await Open(db,app,ct)||await db.RecruitmentScreeningDecisions.AnyAsync(x=>x.RecruitmentApplicationId==app.Id,ct))return Ok(ServiceResult.Fail("Withdrawal is permitted only before closing and before screening begins."));
        app.Status="Withdrawn";app.Version++;Event(db,app,"WithdrawalReceipt",$"Application {app.Reference} withdrawn.");await db.SaveChangesAsync(ct);return Ok(ServiceResult.Ok("Application withdrawn; receipt recorded."));
    }
    [HttpPost("{id:int}/documents")]
    [RequestSizeLimit(6*1024*1024)]
    public async Task<ActionResult<ServiceResult>> Upload(int id,[FromForm]string category,[FromForm]IFormFile file,CancellationToken ct)
    {
        if(category is not ("Identity" or "Qualification" or "Experience")||file.Length<=0||file.Length>5*1024*1024)return BadRequest();
        await using var db=await factory.CreateDbContextAsync(ct);var app=await Own(db,id,ct);
        var canUpload=(app.Status=="Draft"&&await Open(db,app,ct))||(app.Status=="ReturnedForCorrection"&&await IsWithinCorrectionWindowAsync(db,app,ct));
        if(!canUpload)return Ok(ServiceResult.Fail("Documents can only be added to an open draft or within an active HR correction window."));
        if(await db.Set<RecruitmentApplicationDocument>().CountAsync(x=>x.ApplicationId==id,ct)>=10)return Ok(ServiceResult.Fail("Maximum ten documents per application."));
        using var bytes=new MemoryStream();await file.CopyToAsync(bytes,ct);
        var stored=await storage.StoreAsync(new(){OrganisationId=app.OrganisationId,OriginalFileName=file.FileName,DeclaredContentType=file.ContentType,Content=bytes.ToArray(),PermittedExtensionsCsv=".pdf,.png,.jpg,.jpeg",MaxFileSizeMb=5},ct);
        if(!stored.Success)return Ok(ServiceResult.Fail(stored.ErrorMessage??"Upload rejected."));
        db.Set<RecruitmentApplicationDocument>().Add(new(){ApplicationId=id,StoredObjectId=stored.StoredObjectId,Category=category,FileName=stored.SafeFileName,CreatedAtUtc=DateTime.UtcNow});app.Version++;Event(db,app,"DocumentAdded",$"{category} document added.");await db.SaveChangesAsync(ct);return Ok(ServiceResult.Ok("Document stored privately."));
    }
}
