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
[Route("api/v1/recruitment/screening/applications")]
public sealed class RecruitmentScreeningQueriesController(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IDocumentStorageProvider storage,
    IOperationLogService logs) : HrmApiController
{
    private async Task<bool> CanViewRecruitmentAsync() =>
        await access.HasUserGroupAsync("SuperAdministrator")
        || await access.HasUserGroupAsync("Admin")
        || await access.HasPermissionAsync("Recruitment.Manage")
        || await access.HasPermissionAsync("Recruitment.View");

    private async Task<bool> CanManageRecruitmentAsync() =>
        await access.HasUserGroupAsync("SuperAdministrator")
        || await access.HasUserGroupAsync("Admin")
        || await access.HasPermissionAsync("Recruitment.Manage");

    private static (string? Comment, DateTime? DeadlineUtc) ParseReturnEvent(RecruitmentApplicationEvent? evt)
    {
        if (evt is null || string.IsNullOrWhiteSpace(evt.SnapshotJson))
            return (evt?.Message, null);
        try
        {
            using var doc = JsonDocument.Parse(evt.SnapshotJson);
            var root = doc.RootElement;
            string? comment = root.TryGetProperty("Comment", out var c) ? c.GetString() : evt.Message;
            DateTime? deadline = null;
            if (root.TryGetProperty("CorrectionDeadlineUtc", out var d) && d.ValueKind == JsonValueKind.String && DateTime.TryParse(d.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                deadline = parsed.Kind == DateTimeKind.Utc ? parsed : DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }
            return (comment, deadline);
        }
        catch (JsonException)
        {
            return (evt.Message, null);
        }
    }

    [HttpGet("/api/v1/recruitment/screening/pending-count")]
    public async Task<ActionResult<int>> PendingCount(CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !await CanViewRecruitmentAsync()) return Ok(0);
        await using var db = await factory.CreateDbContextAsync(ct);
        var count = await db.RecruitmentApplications.AsNoTracking()
            .CountAsync(x => x.OrganisationId == org && x.Status == "Submitted", ct);
        return Ok(count);
    }

    [HttpGet("{vacancyId:int}")]
    public async Task<ActionResult<List<RecruitmentScreeningApplicationRow>>> List(int vacancyId, CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !await CanViewRecruitmentAsync()) return Forbid();
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.RecruitmentVacancies.AnyAsync(x => x.Id == vacancyId && x.OrganisationId == org, ct)) return NotFound();
        return Ok(await (from app in db.RecruitmentApplications.AsNoTracking()
                         join person in db.RecruitmentApplicants on app.RecruitmentApplicantId equals person.Id
                         where app.RecruitmentVacancyId == vacancyId && app.OrganisationId == org
                         orderby app.SubmittedAtUtc
                         select new RecruitmentScreeningApplicationRow(app.Id, app.Reference, person.FullName, app.Status, app.SubmittedAtUtc)).ToListAsync(ct));
    }

    [HttpGet("{applicationId:int}/detail")]
    public async Task<ActionResult<HrRecruitmentApplicationDetail>> Detail(int applicationId, CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !await CanViewRecruitmentAsync()) return Forbid();
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await (from app in db.RecruitmentApplications.AsNoTracking()
                         join person in db.RecruitmentApplicants.AsNoTracking() on app.RecruitmentApplicantId equals person.Id
                         where app.Id == applicationId && app.OrganisationId == org
                         select new
                         {
                             app.Id,
                             app.Reference,
                             app.Status,
                             app.SubmittedAtUtc,
                             app.ProfileSnapshotJson,
                             person.FullName,
                             person.Email,
                             person.Mobile,
                             person.EfaasIdNumber
                         }).SingleOrDefaultAsync(ct);
        if (row is null) return NotFound();

        var docs = await db.Set<RecruitmentApplicationDocument>().AsNoTracking()
            .Where(x => x.ApplicationId == row.Id)
            .OrderBy(x => x.Category).ThenBy(x => x.CreatedAtUtc)
            .Select(x => new RecruitmentDocumentRow(x.Id, x.Category, x.FileName))
            .ToListAsync(ct);

        var rawEvents = await db.Set<RecruitmentApplicationEvent>().AsNoTracking()
            .Where(x => x.ApplicationId == row.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        var events = rawEvents
            .Select(x => new RecruitmentEventRow(x.Kind, x.Message, x.CreatedAtUtc))
            .ToList();

        var latestReturn = row.Status == "ReturnedForCorrection"
            ? rawEvents.FirstOrDefault(x => x.Kind == "ReturnedForCorrection")
            : null;
        var (returnComment, deadlineUtc) = ParseReturnEvent(latestReturn);

        return Ok(new HrRecruitmentApplicationDetail(
            row.Id,
            row.Reference,
            row.Status,
            row.SubmittedAtUtc,
            row.FullName,
            row.Email,
            row.Mobile,
            row.EfaasIdNumber,
            row.ProfileSnapshotJson,
            docs,
            events,
            returnComment,
            deadlineUtc));
    }

    [HttpGet("documents/{documentId:int}")]
    public async Task<IActionResult> DownloadDocument(int documentId, CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !await CanViewRecruitmentAsync()) return Forbid();
        await using var db = await factory.CreateDbContextAsync(ct);
        var doc = await (from d in db.Set<RecruitmentApplicationDocument>().AsNoTracking()
                         join app in db.RecruitmentApplications.AsNoTracking() on d.ApplicationId equals app.Id
                         where d.Id == documentId && app.OrganisationId == org
                         select new { d.StoredObjectId, d.FileName, app.OrganisationId }).SingleOrDefaultAsync(ct);
        if (doc is null) return NotFound();

        var read = await storage.RetrieveAsync(doc.StoredObjectId, doc.OrganisationId, ct);
        if (!read.Success || read.Content.Length == 0)
            return NotFound(read.ErrorMessage ?? "Document file could not be retrieved.");

        var contentType = string.IsNullOrWhiteSpace(read.ContentType) ? "application/octet-stream" : read.ContentType;
        return File(read.Content, contentType, doc.FileName);
    }

    [HttpPost("{applicationId:int}/return")]
    public async Task<ActionResult<ServiceResult>> ReturnForCorrection(int applicationId, [FromBody] ReturnRecruitmentApplicationInput input, CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !await CanManageRecruitmentAsync()) return Forbid();

        var comment = input.Comment?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > 2000)
            return Ok(ServiceResult.Fail("Provide a clear comment (up to 2,000 characters) explaining what information or document is missing."));

        var deadlineUtc = input.CorrectionDeadlineUtc.Kind == DateTimeKind.Utc
            ? input.CorrectionDeadlineUtc
            : DateTime.SpecifyKind(input.CorrectionDeadlineUtc, DateTimeKind.Utc);
        if (deadlineUtc <= DateTime.UtcNow)
            return Ok(ServiceResult.Fail("Correction deadline must be in the future."));

        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var app = await db.RecruitmentApplications.SingleOrDefaultAsync(x => x.Id == applicationId && x.OrganisationId == org, ct);
        if (app is null) return NotFound();

        if (app.Status != "Submitted")
            return Ok(ServiceResult.Fail($"Only Submitted applications can be returned for correction. Current status is {app.Status}."));

        if (await db.RecruitmentScreeningDecisions.AnyAsync(x => x.RecruitmentApplicationId == app.Id, ct))
            return Ok(ServiceResult.Fail("Screening has already been locked for this application; it cannot be returned."));

        var applicant = await db.RecruitmentApplicants.AsNoTracking().SingleOrDefaultAsync(x => x.Id == app.RecruitmentApplicantId, ct);

        app.Status = "ReturnedForCorrection";
        app.Version++;

        var snapshot = JsonSerializer.Serialize(new
        {
            Comment = comment,
            CorrectionDeadlineUtc = deadlineUtc,
            ReturnedByUserId = actor.UserId,
            ReturnedAtUtc = DateTime.UtcNow
        });

        db.Set<RecruitmentApplicationEvent>().Add(new RecruitmentApplicationEvent
        {
            ApplicationId = app.Id,
            Kind = "ReturnedForCorrection",
            Message = $"Returned by HR for correction (deadline {deadlineUtc.AddHours(5):dd MMM yyyy HH:mm} MVT): {comment}",
            SnapshotJson = snapshot,
            CreatedAtUtc = DateTime.UtcNow
        });

        await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Returned recruitment application {app.Reference} for correction until {deadlineUtc:O}.");

        if (applicant is not null && !string.IsNullOrWhiteSpace(applicant.EfaasIdNumber))
        {
            var targetUsers = await db.Users.AsNoTracking()
                .Where(u => u.Username == applicant.EfaasIdNumber && u.IsActive)
                .Select(u => new { u.UserId, u.BusinessEntityID })
                .ToListAsync(ct);

            foreach (var target in targetUsers)
            {
                db.StaffNotifications.Add(new StaffNotification
                {
                    UserId = target.UserId,
                    IndividualId = target.BusinessEntityID,
                    OrganisationId = app.OrganisationId,
                    Kind = "RecruitmentCorrection",
                    Message = $"Your application ({app.Reference}) was returned by HR for correction. Please update and resubmit before {deadlineUtc.AddHours(5):dd MMM yyyy HH:mm} MVT.",
                    Url = "/online/recruitment",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok($"Application {app.Reference} returned to applicant for correction (deadline {deadlineUtc.AddHours(5):dd MMM yyyy HH:mm} MVT)."));
    }

    [HttpGet("/api/v1/recruitment/screening/conflicts/{vacancyId:int}")]
    public async Task<ActionResult<List<RecruitmentConflictDeclarationRow>>> ListConflicts(int vacancyId, CancellationToken ct)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || !await CanViewRecruitmentAsync()) return Forbid();
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.RecruitmentVacancies.AnyAsync(x => x.Id == vacancyId && x.OrganisationId == org, ct)) return NotFound();
        var rows = await (from c in db.RecruitmentPanelConflictDeclarations.AsNoTracking()
                          join u in db.Users.AsNoTracking() on c.DeclaredByUserId equals u.UserId into us
                          from u in us.DefaultIfEmpty()
                          where c.RecruitmentVacancyId == vacancyId && c.OrganisationId == org
                          orderby c.DeclaredAtUtc descending
                          select new RecruitmentConflictDeclarationRow(
                              c.Id,
                              c.DeclaredByUserId,
                              u != null && !string.IsNullOrWhiteSpace(u.Username) ? u.Username : $"User #{c.DeclaredByUserId}",
                              c.HasConflict,
                              c.Declaration,
                              c.Mitigation,
                              c.DeclaredAtUtc,
                              c.DeclaredByUserId == actor.UserId)).ToListAsync(ct);
        return Ok(rows);
    }
}
