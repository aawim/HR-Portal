using HRM.Application.Contracts.Signatures;
using HRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[ApiController]
[AllowAnonymous]
[Route("api/v1/signatures")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicDocumentSignaturesController(IDbContextFactory<HrmTeContext> factory) : ControllerBase
{
    [HttpGet("public/document")]
    public async Task<ActionResult<List<DocumentSignatureDto>>> ListPublicForDocument(
        [FromQuery] string documentType,
        [FromQuery] string documentId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(documentId))
            return Ok(new List<DocumentSignatureDto>());

        var type = documentType.Trim();
        // Restrict public signature lookup to public-facing recruitment sheets only
        if (type is not ("RecruitmentScreeningSheet" or "RecruitmentA2Sheet"))
            return Ok(new List<DocumentSignatureDto>());

        await using var db = await factory.CreateDbContextAsync(ct);
        await DocumentSignaturesController.EnsureTablesPublicAsync(db, ct);

        var id = documentId.Trim();
        var rows = await db.DocumentSignatures.AsNoTracking()
            .Where(x => x.DocumentType == type && x.DocumentId == id && x.Status == "Signed")
            .OrderBy(x => x.SignedAtUtc)
            .Select(x => new DocumentSignatureDto(
                x.Id,
                x.DocumentType,
                x.DocumentId,
                x.SlotKey,
                x.SlotLabel,
                x.SignerUserId,
                x.SignerName,
                x.SignerPosition,
                x.SignatureImageDataUrl,
                x.DocumentContentHash,
                x.VerificationCode,
                x.SignedAtUtc,
                x.Status,
                false))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("verify/{code}")]
    public async Task<ActionResult<VerifySignatureDto>> Verify(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return NotFound();
        var normalized = code.Trim().ToUpperInvariant();

        await using var db = await factory.CreateDbContextAsync(ct);
        await DocumentSignaturesController.EnsureTablesPublicAsync(db, ct);

        var row = await (from s in db.DocumentSignatures.AsNoTracking()
                         join o in db.Organisations.AsNoTracking() on s.OrganisationId equals o.BusinessEntityID into os
                         from o in os.DefaultIfEmpty()
                         where s.VerificationCode == normalized
                         select new VerifySignatureDto(
                             s.Status == "Signed",
                             s.VerificationCode,
                             s.DocumentType,
                             s.DocumentId,
                             s.SlotLabel,
                             s.SignerName,
                             s.SignerPosition,
                             o != null ? o.OrganisationName : null,
                             s.SignedAtUtc,
                             s.DocumentContentHash,
                             s.Status,
                             s.RevokeReason)).SingleOrDefaultAsync(ct);

        if (row is null) return NotFound();
        return Ok(row);
    }
}
