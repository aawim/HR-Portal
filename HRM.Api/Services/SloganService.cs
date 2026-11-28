using HRM.Models;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static HRM.Services.Contracts.SloganServiceContracts;

namespace HRM.Services;

public sealed class SloganService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService logs) : IOperations
{
    private async Task<(int OrgId, int UserId)> RequireHrWriteAsync()
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator &&
            !await access.HasPermissionAsync("Admin") &&
            !await access.HasUserGroupAsync("Human Resource: Staff") &&
            !await access.HasUserGroupAsync("Human Resource :Head"))
        {
            throw new UnauthorizedAccessException("HR or Administrator access is required to manage Slogans.");
        }
        return (await access.GetCurrentOrganisationIdAsync(), actor.UserId);
    }

    public async Task<SloganWorkspaceDto> LoadAsync()
    {
        await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync();

        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == orgId)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? "Current Organisation";

        var slogans = await db.Slogans.AsNoTracking()
            .Where(s => s.OrganisationId == orgId)
            .OrderByDescending(s => s.IsDefault)
            .ThenByDescending(s => s.IsActive)
            .ThenBy(s => s.Name)
            .ToListAsync();

        var defaultSlogan = slogans.FirstOrDefault(s => s.IsActive && s.IsDefault);
        var explicitAssignments = await db.DocumentSloganAssignments.AsNoTracking()
            .Where(a => a.OrganisationId == orgId)
            .ToDictionaryAsync(a => a.DocumentType);

        var sloganDtos = slogans.Select(ToDto).ToList();

        var assignments = DocumentTypes.All.Select(dt =>
        {
            var hasExplicit = explicitAssignments.TryGetValue(dt.Code, out var row);
            int? effectiveId = hasExplicit ? row!.SloganId : defaultSlogan?.SloganId;
            var effectiveName = effectiveId.HasValue
                ? slogans.FirstOrDefault(s => s.SloganId == effectiveId.Value && s.IsActive)?.Name
                : null;

            return new DocumentSloganAssignmentDto
            {
                DocumentType = dt.Code,
                DocumentLabel = dt.Label,
                SloganId = hasExplicit ? row!.SloganId : null,
                HasExplicitAssignment = hasExplicit,
                EffectiveSloganName = hasExplicit && row!.SloganId == null ? "None" : effectiveName ?? "None"
            };
        }).ToList();

        return new SloganWorkspaceDto
        {
            OrganisationName = orgName,
            Slogans = sloganDtos,
            Assignments = assignments
        };
    }

    public async Task<SloganDto> SaveSloganAsync(SloganSaveRequest request)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Slogan name is required.");

        var sloganType = request.SloganType == "Image" ? "Image" : "Text";
        if (sloganType == "Text" && string.IsNullOrWhiteSpace(request.TextContent))
            throw new InvalidOperationException("Text content is required for a text slogan.");

        byte[]? imageBytes = null;
        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            var raw = request.ImageBase64;
            var comma = raw.IndexOf(',');
            if (comma >= 0) raw = raw[(comma + 1)..];
            imageBytes = Convert.FromBase64String(raw);
            if (imageBytes.Length > 2 * 1024 * 1024)
                throw new InvalidOperationException("Slogan image must be 2 MB or smaller.");
        }
        else if (sloganType == "Image" && request.SloganId == 0)
        {
            throw new InvalidOperationException("Please upload an image for an image slogan.");
        }

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var logId = await logs.CreateLogAsync(1, request.SloganId == 0 ? "CreateSlogan" : "UpdateSlogan");

        Slogan entity;
        if (request.SloganId == 0)
        {
            entity = new Slogan
            {
                OrganisationId = orgId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Slogans.Add(entity);
        }
        else
        {
            entity = await db.Slogans.SingleOrDefaultAsync(s => s.SloganId == request.SloganId && s.OrganisationId == orgId)
                ?? throw new KeyNotFoundException("Slogan not found.");
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        entity.Name = request.Name.Trim();
        entity.SloganType = sloganType;
        entity.TextContent = string.IsNullOrWhiteSpace(request.TextContent) ? null : request.TextContent.Trim();
        entity.TextColor = string.IsNullOrWhiteSpace(request.TextColor) ? "#1e3a8a" : request.TextColor.Trim();
        entity.BackgroundColor = string.IsNullOrWhiteSpace(request.BackgroundColor) ? "#eff6ff" : request.BackgroundColor.Trim();
        entity.BorderColor = string.IsNullOrWhiteSpace(request.BorderColor) ? "#93c5fd" : request.BorderColor.Trim();
        entity.BoxStyle = request.BoxStyle == "None" ? "None" : "RoundedRectangle";
        if (imageBytes != null)
        {
            entity.ImageData = imageBytes;
            entity.ImageContentType = string.IsNullOrWhiteSpace(request.ImageContentType) ? "image/png" : request.ImageContentType.Trim();
        }
        entity.Placement = request.Placement == "Header" ? "Header" : "Footer";
        entity.Alignment = request.Alignment is "Left" or "Right" ? request.Alignment : "Center";
        entity.IsActive = request.IsActive;
        entity.OperationLogId = logId;

        if (request.IsDefault)
        {
            var others = await db.Slogans.Where(s => s.OrganisationId == orgId && s.SloganId != entity.SloganId && s.IsDefault).ToListAsync();
            foreach (var o in others) o.IsDefault = false;
            entity.IsDefault = true;
        }
        else
        {
            entity.IsDefault = false;
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return ToDto(entity);
    }

    public async Task DeleteSloganAsync(int sloganId)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        await using var db = await factory.CreateDbContextAsync();

        var entity = await db.Slogans.SingleOrDefaultAsync(s => s.SloganId == sloganId && s.OrganisationId == orgId)
            ?? throw new KeyNotFoundException("Slogan not found.");

        await logs.CreateLogAsync(1, "DeleteSlogan");
        db.Slogans.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task SetDefaultSloganAsync(int? sloganId)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        await using var db = await factory.CreateDbContextAsync();
        await logs.CreateLogAsync(1, "SetDefaultSlogan");

        var all = await db.Slogans.Where(s => s.OrganisationId == orgId).ToListAsync();
        foreach (var s in all)
        {
            s.IsDefault = sloganId.HasValue && s.SloganId == sloganId.Value;
        }
        await db.SaveChangesAsync();
    }

    public async Task SaveDocumentAssignmentAsync(string documentType, int? sloganId)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        if (string.IsNullOrWhiteSpace(documentType))
            throw new InvalidOperationException("Document type is required.");

        await using var db = await factory.CreateDbContextAsync();
        var logId = await logs.CreateLogAsync(1, "AssignDocumentSlogan");

        var row = await db.DocumentSloganAssignments
            .SingleOrDefaultAsync(a => a.OrganisationId == orgId && a.DocumentType == documentType);

        if (row == null)
        {
            row = new DocumentSloganAssignment
            {
                OrganisationId = orgId,
                DocumentType = documentType
            };
            db.DocumentSloganAssignments.Add(row);
        }

        row.SloganId = sloganId;
        row.UpdatedByUserId = userId;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.OperationLogId = logId;

        await db.SaveChangesAsync();
    }

    public async Task<SloganDto?> ResolveForDocumentAsync(string documentType)
    {
        await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync();

        var assignment = await db.DocumentSloganAssignments.AsNoTracking()
            .Include(a => a.Slogan)
            .SingleOrDefaultAsync(a => a.OrganisationId == orgId && a.DocumentType == documentType);

        if (assignment != null)
        {
            if (assignment.SloganId == null) return null; // Explicitly None
            if (assignment.Slogan is { IsActive: true }) return ToDto(assignment.Slogan);
        }

        var defaultSlogan = await db.Slogans.AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrganisationId == orgId && s.IsActive && s.IsDefault);

        return defaultSlogan == null ? null : ToDto(defaultSlogan);
    }

    private static SloganDto ToDto(Slogan s) => new()
    {
        SloganId = s.SloganId,
        OrganisationId = s.OrganisationId,
        Name = s.Name,
        SloganType = s.SloganType,
        TextContent = s.TextContent,
        TextColor = s.TextColor,
        BackgroundColor = s.BackgroundColor,
        BorderColor = s.BorderColor,
        BoxStyle = s.BoxStyle,
        ImageBase64 = s.ImageData != null ? $"data:{s.ImageContentType ?? "image/png"};base64,{Convert.ToBase64String(s.ImageData)}" : null,
        ImageContentType = s.ImageContentType,
        Placement = s.Placement,
        Alignment = s.Alignment,
        IsDefault = s.IsDefault,
        IsActive = s.IsActive,
        CreatedAtUtc = s.CreatedAtUtc
    };
}
