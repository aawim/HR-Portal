using System.Text.Json;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Positions;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/position-contracts")]
public sealed class PositionContractsController(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access) : HrmApiController
{
    private sealed class ContractWorkflowMetadata
    {
        public string WorkflowStatus { get; set; } = "PendingAcceptance";
        public int JobPositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public DateTime? AcceptedAtUtc { get; set; }
        public int? AcceptedByUserId { get; set; }
        public string? AcceptedByName { get; set; }
        public string? UnlockRequestReason { get; set; }
        public int? UnlockRequestedByUserId { get; set; }
        public string? UnlockRequestedByName { get; set; }
        public DateTime? UnlockRequestedAtUtc { get; set; }
        public DateTime? UnlockedAtUtc { get; set; }
        public int? UnlockedByUserId { get; set; }
        public string? UnlockedByName { get; set; }
        public string? StaffDecisionNote { get; set; }
    }

    private async Task<bool> CanManagePositionsAsync()
    {
        var actor = await access.RequireContextAsync();
        return actor.IsSuperAdministrator
            || actor.IsHRStaff || actor.IsHRHead || actor.IsHRManager || actor.IsOrganisationAdmin
            || await access.HasPermissionAsync("Staff.Positions.Manage")
            || await access.HasPermissionAsync("Admin");
    }

    private async Task<string> ActorDisplayNameAsync(HrmTeContext db, int userId, int individualId, CancellationToken ct)
    {
        if (individualId > 0)
        {
            var ind = await db.Individuals.AsNoTracking()
                .Where(x => x.BusinessEntityId == individualId)
                .Select(x => (x.FirstNameEnglish + " " + (x.MiddleNameEnglish ?? "") + " " + (x.LastNameEnglish ?? "")).Trim())
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(ind)) return ind;
        }
        var uname = await db.Users.AsNoTracking().Where(u => u.UserId == userId).Select(u => u.Username).FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(uname) ? $"User #{userId}" : uname;
    }

    private static ContractWorkflowMetadata ParseMetadata(DmsDocument doc)
    {
        ContractWorkflowMetadata? meta = null;
        if (!string.IsNullOrWhiteSpace(doc.CustomMetadataJson))
        {
            try { meta = JsonSerializer.Deserialize<ContractWorkflowMetadata>(doc.CustomMetadataJson); }
            catch { }
        }
        meta ??= new ContractWorkflowMetadata();

        if (meta.JobPositionId <= 0 && !string.IsNullOrWhiteSpace(doc.ReferenceCode) && doc.ReferenceCode.StartsWith("POS-", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(doc.ReferenceCode[4..], out var parsedId))
                meta.JobPositionId = parsedId;
        }
        if (string.IsNullOrWhiteSpace(meta.WorkflowStatus))
        {
            meta.WorkflowStatus = doc.IsLockedForEdit ? "Finalised" : "PendingAcceptance";
        }
        return meta;
    }

    private static PositionContractDto ToDto(DmsDocument doc)
    {
        var meta = ParseMetadata(doc);
        var isLocked = meta.WorkflowStatus is "Finalised" or "UnlockRequested" || doc.IsLockedForEdit;
        return new PositionContractDto(
            doc.DocumentId,
            doc.DocumentNumber,
            doc.Title,
            meta.JobPositionId,
            string.IsNullOrWhiteSpace(meta.PositionName) ? doc.Title : meta.PositionName,
            doc.CurrentVersionNumber,
            meta.WorkflowStatus,
            isLocked,
            doc.CreatedAtUtc,
            meta.AcceptedAtUtc,
            meta.AcceptedByName,
            meta.UnlockRequestReason,
            meta.UnlockRequestedByName,
            meta.UnlockRequestedAtUtc,
            meta.UnlockedAtUtc,
            meta.UnlockedByName,
            meta.StaffDecisionNote);
    }

    [HttpGet("staff/{staffId:int}")]
    public async Task<ActionResult<List<PositionContractDto>>> ListForStaff(int staffId, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (actor.IndividualId != staffId && !await CanManagePositionsAsync())
            return Forbid();

        await using var db = await factory.CreateDbContextAsync(ct);
        var docs = await db.DmsDocuments.AsNoTracking()
            .Where(d => !d.IsDeleted
                && d.SubjectIndividualId == staffId
                && (org <= 0 || d.OrganisationId == org)
                && d.ReferenceCode != null
                && d.ReferenceCode.StartsWith("POS-"))
            .OrderByDescending(d => d.DocumentId)
            .ToListAsync(ct);

        return Ok(docs.Select(ToDto).ToList());
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<PositionContractDto>>> ListMine(CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        if (actor.IndividualId <= 0)
            return Ok(new List<PositionContractDto>());

        await using var db = await factory.CreateDbContextAsync(ct);
        var docs = await db.DmsDocuments.AsNoTracking()
            .Where(d => !d.IsDeleted
                && d.SubjectIndividualId == actor.IndividualId
                && d.ReferenceCode != null
                && d.ReferenceCode.StartsWith("POS-"))
            .OrderByDescending(d => d.DocumentId)
            .ToListAsync(ct);

        return Ok(docs.Select(ToDto).ToList());
    }

    [HttpPost("register-uploaded")]
    public async Task<ActionResult<ServiceResult>> RegisterUploaded([FromBody] RegisterPositionContractInput input, CancellationToken ct)
    {
        if (!await CanManagePositionsAsync())
            return Forbid();
        if (input.DocumentId <= 0 || input.JobPositionId <= 0)
            return Ok(ServiceResult.Fail("Provide a valid document and position assignment."));

        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);

        var doc = await db.DmsDocuments.SingleOrDefaultAsync(d => d.DocumentId == input.DocumentId && !d.IsDeleted, ct);
        if (doc is null || !doc.SubjectIndividualId.HasValue)
            return Ok(ServiceResult.Fail("Uploaded contract document was not found."));

        var refCode = $"POS-{input.JobPositionId}";
        var existingDocs = await db.DmsDocuments
            .Where(d => !d.IsDeleted && d.DocumentId != doc.DocumentId && d.SubjectIndividualId == doc.SubjectIndividualId && d.ReferenceCode == refCode)
            .ToListAsync(ct);

        foreach (var prev in existingDocs)
        {
            var prevMeta = ParseMetadata(prev);
            if (prevMeta.WorkflowStatus is "Finalised" or "UnlockRequested")
            {
                doc.IsDeleted = true;
                await db.SaveChangesAsync(ct);
                return Ok(ServiceResult.Fail("This position already has a finalised contract locked by the staff member. Request an unlock from the staff member before making changes."));
            }

            if (prevMeta.WorkflowStatus is "UnlockedForEdit" or "PendingAcceptance")
            {
                prevMeta.WorkflowStatus = "Superseded";
                prev.CustomMetadataJson = JsonSerializer.Serialize(prevMeta);
                prev.Status = "Superseded";
                prev.SupersededByDocumentId = doc.DocumentId;
                prev.IsLockedForEdit = true;
            }
        }

        var newMeta = new ContractWorkflowMetadata
        {
            WorkflowStatus = "PendingAcceptance",
            JobPositionId = input.JobPositionId,
            PositionName = string.IsNullOrWhiteSpace(input.PositionName) ? doc.Title : input.PositionName.Trim()
        };
        doc.ReferenceCode = refCode;
        doc.CustomMetadataJson = JsonSerializer.Serialize(newMeta);
        doc.Status = "PendingApproval";
        doc.IsLockedForEdit = false;
        doc.IsImmutable = false;

        var targetUserIds = await db.Users.AsNoTracking()
            .Where(u => u.BusinessEntityID == doc.SubjectIndividualId.Value && u.IsActive)
            .Select(u => u.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var targetUserId in targetUserIds)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = targetUserId,
                IndividualId = doc.SubjectIndividualId.Value,
                OrganisationId = org > 0 ? org : doc.OrganisationId,
                Kind = "PositionContract",
                Message = $"A position contract ({newMeta.PositionName}) has been attached and requires your acceptance to finalise.",
                Url = "/online/documents?tab=contracts",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok("Contract attached and sent to staff for acceptance."));
    }

    [HttpPost("{documentId:long}/accept")]
    public async Task<ActionResult<ServiceResult>> Accept(long documentId, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);

        var doc = await db.DmsDocuments.SingleOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, ct);
        if (doc is null || doc.SubjectIndividualId != actor.IndividualId)
            return Forbid();

        var meta = ParseMetadata(doc);
        if (meta.WorkflowStatus != "PendingAcceptance")
            return Ok(ServiceResult.Fail($"This contract cannot be accepted in its current status ({meta.WorkflowStatus})."));

        var actorName = await ActorDisplayNameAsync(db, actor.UserId, actor.IndividualId, ct);
        meta.WorkflowStatus = "Finalised";
        meta.AcceptedAtUtc = DateTime.UtcNow;
        meta.AcceptedByUserId = actor.UserId;
        meta.AcceptedByName = actorName;

        doc.CustomMetadataJson = JsonSerializer.Serialize(meta);
        doc.Status = "OfficialRecord";
        doc.IsOfficialRecord = true;
        doc.IsImmutable = true;
        doc.IsLockedForEdit = true;
        doc.IsVerified = true;
        doc.VerifiedByUserId = actor.UserId;
        doc.VerifiedByUserName = actorName;
        doc.VerifiedAtUtc = DateTime.UtcNow;

        if (doc.CreatedByUserId > 0 && doc.CreatedByUserId != actor.UserId)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = doc.CreatedByUserId,
                IndividualId = actor.IndividualId,
                OrganisationId = doc.OrganisationId,
                Kind = "PositionContract",
                Message = $"{actorName} accepted and finalised their position contract ({meta.PositionName}).",
                Url = $"/admin/my/workspace/staff/{actor.IndividualId}/position",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok("Contract accepted, finalised, and locked."));
    }

    [HttpPost("request-unlock")]
    public async Task<ActionResult<ServiceResult>> RequestUnlock([FromBody] RequestContractUnlockInput input, CancellationToken ct)
    {
        if (!await CanManagePositionsAsync())
            return Forbid();
        if (input.DocumentId <= 0 || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 1000)
            return Ok(ServiceResult.Fail("Provide a valid reason (up to 1,000 characters) to request contract unlock."));

        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);

        var doc = await db.DmsDocuments.SingleOrDefaultAsync(d => d.DocumentId == input.DocumentId && !d.IsDeleted, ct);
        if (doc is null || !doc.SubjectIndividualId.HasValue)
            return Ok(ServiceResult.Fail("Contract document not found."));

        var meta = ParseMetadata(doc);
        if (meta.WorkflowStatus != "Finalised")
            return Ok(ServiceResult.Fail("Unlock can only be requested for a finalised contract."));

        var actorName = await ActorDisplayNameAsync(db, actor.UserId, actor.IndividualId, ct);
        meta.WorkflowStatus = "UnlockRequested";
        meta.UnlockRequestReason = input.Reason.Trim();
        meta.UnlockRequestedByUserId = actor.UserId;
        meta.UnlockRequestedByName = actorName;
        meta.UnlockRequestedAtUtc = DateTime.UtcNow;
        meta.StaffDecisionNote = null;

        doc.CustomMetadataJson = JsonSerializer.Serialize(meta);

        var targetUserIds = await db.Users.AsNoTracking()
            .Where(u => u.BusinessEntityID == doc.SubjectIndividualId.Value && u.IsActive)
            .Select(u => u.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var targetUserId in targetUserIds)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = targetUserId,
                IndividualId = doc.SubjectIndividualId.Value,
                OrganisationId = org > 0 ? org : doc.OrganisationId,
                Kind = "PositionContractUnlock",
                Message = $"HR requested permission to edit your finalised contract ({meta.PositionName}). Reason: {meta.UnlockRequestReason}",
                Url = "/online/documents?tab=contracts",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok("Unlock request sent to the staff member for approval."));
    }

    [HttpPost("respond-unlock")]
    public async Task<ActionResult<ServiceResult>> RespondUnlock([FromBody] RespondContractUnlockInput input, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);

        var doc = await db.DmsDocuments.SingleOrDefaultAsync(d => d.DocumentId == input.DocumentId && !d.IsDeleted, ct);
        if (doc is null || doc.SubjectIndividualId != actor.IndividualId)
            return Forbid();

        var meta = ParseMetadata(doc);
        if (meta.WorkflowStatus != "UnlockRequested")
            return Ok(ServiceResult.Fail("There is no pending unlock request for this contract."));

        var actorName = await ActorDisplayNameAsync(db, actor.UserId, actor.IndividualId, ct);
        meta.StaffDecisionNote = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();

        if (input.Approve)
        {
            meta.WorkflowStatus = "UnlockedForEdit";
            meta.UnlockedAtUtc = DateTime.UtcNow;
            meta.UnlockedByUserId = actor.UserId;
            meta.UnlockedByName = actorName;
            doc.IsLockedForEdit = false;
            doc.IsImmutable = false;
            doc.Status = "Active";
        }
        else
        {
            meta.WorkflowStatus = "Finalised";
            doc.IsLockedForEdit = true;
            doc.IsImmutable = true;
            doc.Status = "OfficialRecord";
        }

        doc.CustomMetadataJson = JsonSerializer.Serialize(meta);

        var notifyHrUserId = meta.UnlockRequestedByUserId ?? (doc.CreatedByUserId > 0 ? doc.CreatedByUserId : null);
        if (notifyHrUserId.HasValue && notifyHrUserId.Value != actor.UserId)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = notifyHrUserId.Value,
                IndividualId = actor.IndividualId,
                OrganisationId = doc.OrganisationId,
                Kind = "PositionContractUnlock",
                Message = input.Approve
                    ? $"{actorName} unlocked their position contract ({meta.PositionName}) for HR edits."
                    : $"{actorName} declined the unlock request for their position contract ({meta.PositionName}).",
                Url = $"/admin/my/workspace/staff/{actor.IndividualId}/position",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok(input.Approve
            ? "Contract unlocked for HR to upload a revised agreement."
            : "Unlock request declined; contract remains finalised and locked."));
    }
}
