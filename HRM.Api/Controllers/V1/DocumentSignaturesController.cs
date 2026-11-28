using System.Security.Cryptography;
using System.Text;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Signatures;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Signatures;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/signatures")]
public sealed class DocumentSignaturesController(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService logs) : HrmApiController
{
    private static volatile bool _tablesReady;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public static Task EnsureTablesPublicAsync(HrmTeContext db, CancellationToken ct) => EnsureTablesAsync(db, ct);

    private static async Task EnsureTablesAsync(HrmTeContext db, CancellationToken ct)
    {
        if (_tablesReady) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_tablesReady) return;
            await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.DS_UserSignatures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DS_UserSignatures (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DS_UserSignatures PRIMARY KEY,
        UserId INT NOT NULL,
        IndividualId INT NOT NULL,
        SignatureDataUrl NVARCHAR(MAX) NOT NULL,
        SignatureStyle NVARCHAR(20) NOT NULL CONSTRAINT DF_DS_UserSignatures_SignatureStyle DEFAULT ('Canvas'),
        ContentSha256 NVARCHAR(64) NOT NULL,
        PinHash NVARCHAR(128) NULL,
        PinSalt NVARCHAR(64) NULL,
        PinFailedAttempts INT NOT NULL CONSTRAINT DF_DS_UserSignatures_PinFailedAttempts DEFAULT (0),
        PinLockedUntilUtc DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DS_UserSignatures_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DS_UserSignatures_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL CONSTRAINT DF_DS_UserSignatures_IsActive DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_DS_UserSignatures_UserId ON dbo.DS_UserSignatures (UserId);
END
ELSE IF COL_LENGTH(N'dbo.DS_UserSignatures', N'PinHash') IS NULL
BEGIN
    ALTER TABLE dbo.DS_UserSignatures ADD PinHash NVARCHAR(128) NULL, PinSalt NVARCHAR(64) NULL, PinFailedAttempts INT NOT NULL CONSTRAINT DF_DS_UserSignatures_PinFailedAttempts DEFAULT (0), PinLockedUntilUtc DATETIME2 NULL;
END;
IF COL_LENGTH(N'dbo.DS_UserSignatures', N'SignatureStyle') IS NULL
BEGIN
    ALTER TABLE dbo.DS_UserSignatures ADD SignatureStyle NVARCHAR(20) NOT NULL CONSTRAINT DF_DS_UserSignatures_SignatureStyle DEFAULT ('Canvas');
END;
IF OBJECT_ID(N'dbo.DS_DocumentSignatures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DS_DocumentSignatures (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DS_DocumentSignatures PRIMARY KEY,
        OrganisationId INT NOT NULL,
        DocumentType NVARCHAR(80) NOT NULL,
        DocumentId NVARCHAR(80) NOT NULL,
        SlotKey NVARCHAR(80) NOT NULL,
        SlotLabel NVARCHAR(150) NOT NULL,
        SignerUserId INT NOT NULL,
        SignerIndividualId INT NOT NULL,
        SignerName NVARCHAR(250) NOT NULL,
        SignerPosition NVARCHAR(250) NULL,
        SignatureImageDataUrl NVARCHAR(MAX) NOT NULL,
        SignatureStyle NVARCHAR(20) NOT NULL CONSTRAINT DF_DS_DocumentSignatures_SignatureStyle DEFAULT ('Canvas'),
        DocumentContentHash NVARCHAR(64) NOT NULL,
        VerificationCode NVARCHAR(40) NOT NULL,
        SignedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DS_DocumentSignatures_SignedAtUtc DEFAULT SYSUTCDATETIME(),
        IpAddress NVARCHAR(64) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_DS_DocumentSignatures_Status DEFAULT ('Signed'),
        RevokedAtUtc DATETIME2 NULL,
        RevokedByUserId INT NULL,
        RevokeReason NVARCHAR(500) NULL,
        OperationLogId INT NULL,
        CONSTRAINT FK_DS_DocumentSignatures_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
    );
    CREATE UNIQUE INDEX IX_DS_DocumentSignatures_VerificationCode ON dbo.DS_DocumentSignatures (VerificationCode);
    CREATE INDEX IX_DS_DocumentSignatures_DocSlot ON dbo.DS_DocumentSignatures (OrganisationId, DocumentType, DocumentId, SlotKey);
END;
IF COL_LENGTH(N'dbo.DS_DocumentSignatures', N'SignatureStyle') IS NULL
BEGIN
    ALTER TABLE dbo.DS_DocumentSignatures ADD SignatureStyle NVARCHAR(20) NOT NULL CONSTRAINT DF_DS_DocumentSignatures_SignatureStyle DEFAULT ('Canvas');
END;", ct);
            _tablesReady = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string NormalizeSignatureStyle(string? style) => (style?.Trim().ToLowerInvariant()) switch
    {
        "png" or "upload" or "image" => "Png",
        "qrcode" or "qr" => "QrCode",
        _ => "Canvas"
    };

    private static bool IsValidSignatureDataUrl(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return false;
        if (dataUrl.Length > 450_000) return false; // ~330KB decoded limit
        return dataUrl.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase)
            || dataUrl.StartsWith("data:image/webp;base64,", StringComparison.OrdinalIgnoreCase)
            || dataUrl.StartsWith("data:image/jpeg;base64,", StringComparison.OrdinalIgnoreCase);
    }

    private static string Sha256Hex(string input) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    private static bool IsValidPinFormat(string? pin) =>
        !string.IsNullOrWhiteSpace(pin) && pin.Length is >= 4 and <= 8 && pin.All(char.IsDigit);

    private static (string Hash, string Salt) HashPin(string pin)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pin), saltBytes, 100_000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    private static bool VerifyPinHash(string pin, string storedHash, string storedSalt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            var expectedBytes = Convert.FromBase64String(storedHash);
            var actualBytes = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pin), saltBytes, 100_000, HashAlgorithmName.SHA256, expectedBytes.Length);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch
        {
            return false;
        }
    }

    private static (bool Ok, string? Error) ValidateUserPin(UserSignature userSig, string? suppliedPin)
    {
        if (string.IsNullOrWhiteSpace(userSig.PinHash) || string.IsNullOrWhiteSpace(userSig.PinSalt))
            return (false, "Set up your 4–8 digit Signing PIN in My Profile before signing documents.");

        if (userSig.PinLockedUntilUtc.HasValue && userSig.PinLockedUntilUtc.Value > DateTime.UtcNow)
        {
            var remaining = Math.Max(1, (int)Math.Ceiling((userSig.PinLockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes));
            return (false, $"Signing PIN is temporarily locked due to too many failed attempts. Try again in {remaining} minute(s).");
        }

        if (!IsValidPinFormat(suppliedPin) || !VerifyPinHash(suppliedPin!, userSig.PinHash, userSig.PinSalt))
        {
            userSig.PinFailedAttempts++;
            if (userSig.PinFailedAttempts >= 5)
            {
                userSig.PinLockedUntilUtc = DateTime.UtcNow.AddMinutes(5);
                userSig.PinFailedAttempts = 0;
                return (false, "Too many incorrect PIN attempts. Signing is locked for 5 minutes.");
            }
            return (false, $"Incorrect Signing PIN ({5 - userSig.PinFailedAttempts} attempt(s) remaining).");
        }

        userSig.PinFailedAttempts = 0;
        userSig.PinLockedUntilUtc = null;
        return (true, null);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserSignatureDto?>> GetMySignature(CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var row = await db.UserSignatures.AsNoTracking()
            .Where(x => x.UserId == actor.UserId && x.IsActive)
            .Select(x => new UserSignatureDto(x.Id, x.UserId, x.SignatureDataUrl, x.UpdatedAtUtc, x.PinHash != null && x.PinHash != "", x.SignatureStyle))
            .SingleOrDefaultAsync(ct);

        if (row is null) return NoContent();
        return Ok(row);
    }

    [HttpPost("me/pin")]
    public async Task<ActionResult<ServiceResult>> SetMyPin([FromBody] SetSignaturePinInput input, CancellationToken ct)
    {
        var pin = input.Pin?.Trim() ?? string.Empty;
        if (!IsValidPinFormat(pin))
            return Ok(ServiceResult.Fail("Signing PIN must be 4 to 8 numeric digits."));

        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var userSig = await db.UserSignatures.SingleOrDefaultAsync(x => x.UserId == actor.UserId && x.IsActive, ct);
        if (userSig is null)
            return Ok(ServiceResult.Fail("Save your digital signature image first before setting a Signing PIN."));

        if (!string.IsNullOrWhiteSpace(userSig.PinHash) && !string.IsNullOrWhiteSpace(userSig.PinSalt))
        {
            var (ok, err) = ValidateUserPin(userSig, input.CurrentPin?.Trim());
            if (!ok)
            {
                await db.SaveChangesAsync(ct);
                return Ok(ServiceResult.Fail(err ?? "Current Signing PIN is incorrect."));
            }
        }

        var (hash, salt) = HashPin(pin);
        userSig.PinHash = hash;
        userSig.PinSalt = salt;
        userSig.PinFailedAttempts = 0;
        userSig.PinLockedUntilUtc = null;
        userSig.UpdatedAtUtc = DateTime.UtcNow;

        await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, "Updated digital signature PIN.");
        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok("Your Signing PIN has been saved."));
    }

    [HttpPost("me/verify-pin")]
    public async Task<ActionResult<ServiceResult>> VerifyMyPin([FromBody] VerifySignaturePinInput input, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var userSig = await db.UserSignatures.SingleOrDefaultAsync(x => x.UserId == actor.UserId && x.IsActive, ct);
        if (userSig is null)
            return Ok(ServiceResult.Fail("Save your digital signature and Signing PIN in My Profile first."));

        var (ok, err) = ValidateUserPin(userSig, input.Pin?.Trim());
        await db.SaveChangesAsync(ct);
        return ok ? Ok(ServiceResult.Ok("Signing PIN verified.")) : Ok(ServiceResult.Fail(err ?? "Incorrect Signing PIN."));
    }

    [HttpPost("me")]
    public async Task<ActionResult<ServiceResult>> SaveMySignature([FromBody] SaveUserSignatureInput input, CancellationToken ct)
    {
        var style = NormalizeSignatureStyle(input.SignatureStyle);
        var isQrOnly = style == "QrCode";
        if (!isQrOnly && !IsValidSignatureDataUrl(input.SignatureDataUrl))
            return Ok(ServiceResult.Fail("Provide a valid PNG, WebP, or JPEG signature image (max 300 KB)."));

        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var dataUrl = isQrOnly ? string.Empty : input.SignatureDataUrl.Trim();
        var now = DateTime.UtcNow;

        var existing = await db.UserSignatures.SingleOrDefaultAsync(x => x.UserId == actor.UserId, ct);
        if (existing is null)
        {
            var sha = Sha256Hex(string.IsNullOrEmpty(dataUrl) ? $"QR:{actor.UserId}:{now:O}" : dataUrl);
            db.UserSignatures.Add(new UserSignature
            {
                UserId = actor.UserId,
                IndividualId = actor.IndividualId,
                SignatureDataUrl = dataUrl,
                SignatureStyle = style,
                ContentSha256 = sha,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                IsActive = true
            });
        }
        else
        {
            existing.IndividualId = actor.IndividualId;
            existing.SignatureStyle = style;
            if (!isQrOnly)
            {
                existing.SignatureDataUrl = dataUrl;
                existing.ContentSha256 = Sha256Hex(dataUrl);
            }
            existing.UpdatedAtUtc = now;
            existing.IsActive = true;
        }

        await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Updated user digital signature (style: {style}).");
        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok(isQrOnly
            ? "Signing style switched to QR Code Seal."
            : $"Your digital signature ({(style == "Png" ? "PNG Upload" : "Canvas")}) has been saved."));
    }

    [HttpPost("me/style")]
    public async Task<ActionResult<ServiceResult>> SetMyStyle([FromBody] SetSignatureStyleInput input, CancellationToken ct)
    {
        var style = NormalizeSignatureStyle(input.SignatureStyle);
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var now = DateTime.UtcNow;
        var existing = await db.UserSignatures.SingleOrDefaultAsync(x => x.UserId == actor.UserId, ct);
        if (existing is null)
        {
            if (style != "QrCode")
                return Ok(ServiceResult.Fail($"Please {(style == "Png" ? "upload a PNG signature" : "draw your signature on the canvas")} first."));

            db.UserSignatures.Add(new UserSignature
            {
                UserId = actor.UserId,
                IndividualId = actor.IndividualId,
                SignatureDataUrl = string.Empty,
                SignatureStyle = "QrCode",
                ContentSha256 = Sha256Hex($"QR:{actor.UserId}:{now:O}"),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                IsActive = true
            });
        }
        else
        {
            if (style != "QrCode" && string.IsNullOrWhiteSpace(existing.SignatureDataUrl))
                return Ok(ServiceResult.Fail($"Please {(style == "Png" ? "upload a PNG signature" : "draw your signature on the canvas")} first."));

            existing.SignatureStyle = style;
            existing.UpdatedAtUtc = now;
            existing.IsActive = true;
        }

        await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Switched digital signing style to {style}.");
        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok($"Active signing style switched to {(style == "QrCode" ? "QR Code Seal" : style == "Png" ? "PNG Image" : "Canvas")}."));
    }

    [HttpDelete("me")]
    public async Task<ActionResult<ServiceResult>> DeleteMySignature(CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var existing = await db.UserSignatures.SingleOrDefaultAsync(x => x.UserId == actor.UserId, ct);
        if (existing is not null)
        {
            existing.IsActive = false;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return Ok(ServiceResult.Ok("Saved profile signature removed."));
    }

    [HttpGet("document")]
    public async Task<ActionResult<List<DocumentSignatureDto>>> ListForDocument([FromQuery] string documentType, [FromQuery] string documentId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(documentId))
            return Ok(new List<DocumentSignatureDto>());

        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        var isAdmin = actor.IsSuperAdministrator || await access.HasUserGroupAsync("Admin");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var type = documentType.Trim();
        var id = documentId.Trim();

        var rows = await db.DocumentSignatures.AsNoTracking()
            .Where(x => x.OrganisationId == org && x.DocumentType == type && x.DocumentId == id && x.Status == "Signed")
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
                isAdmin || x.SignerUserId == actor.UserId,
                x.SignatureStyle))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPost("sign")]
    public async Task<ActionResult<ServiceResult>> SignDocument([FromBody] SignDocumentInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.DocumentType)
            || string.IsNullOrWhiteSpace(input.DocumentId)
            || string.IsNullOrWhiteSpace(input.SlotKey))
            return Ok(ServiceResult.Fail("Document type, document ID, and signature slot are required."));

        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org))
            return Forbid();

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var docType = input.DocumentType.Trim();
        var docId = input.DocumentId.Trim();
        var slotKey = input.SlotKey.Trim();
        var slotLabel = string.IsNullOrWhiteSpace(input.SlotLabel) ? slotKey : input.SlotLabel.Trim();

        if (await db.DocumentSignatures.AnyAsync(x =>
            x.OrganisationId == org && x.DocumentType == docType && x.DocumentId == docId && x.SlotKey == slotKey && x.Status == "Signed", ct))
        {
            return Ok(ServiceResult.Fail($"This document is already signed for '{slotLabel}'. Revoke the existing signature first if re-signing is needed."));
        }

        var userSig = await db.UserSignatures.SingleOrDefaultAsync(x => x.UserId == actor.UserId && x.IsActive, ct);
        if (userSig is null)
            return Ok(ServiceResult.Fail("You have not saved a digital signature yet. Go to My Profile to draw or upload your signature and set a Signing PIN."));

        var (pinOk, pinErr) = ValidateUserPin(userSig, input.Pin?.Trim());
        if (!pinOk)
        {
            await db.SaveChangesAsync(ct);
            return Ok(ServiceResult.Fail(pinErr ?? "Invalid Signing PIN."));
        }

        string signatureImage;
        string effectiveStyle;
        if (!string.IsNullOrWhiteSpace(input.InlineSignatureDataUrl))
        {
            if (!IsValidSignatureDataUrl(input.InlineSignatureDataUrl))
                return Ok(ServiceResult.Fail("Invalid signature image format."));
            signatureImage = input.InlineSignatureDataUrl.Trim();
            effectiveStyle = "Canvas";
        }
        else if (string.Equals(userSig.SignatureStyle, "QrCode", StringComparison.OrdinalIgnoreCase))
        {
            signatureImage = string.Empty;
            effectiveStyle = "QrCode";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(userSig.SignatureDataUrl))
                return Ok(ServiceResult.Fail("No saved signature image found for your current signing style. Update your signature in Online Portal → My Signature."));
            signatureImage = userSig.SignatureDataUrl;
            effectiveStyle = NormalizeSignatureStyle(userSig.SignatureStyle);
        }

        // Resolve Signer Name & Active Position
        var signerName = string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName.Trim();
        var signerPosition = string.IsNullOrWhiteSpace(input.SignerPositionOverride) ? null : input.SignerPositionOverride.Trim();

        if (string.IsNullOrWhiteSpace(signerPosition) && actor.IndividualId > 0)
        {
            signerPosition = await db.Jobs.AsNoTracking()
                .Where(j => j.IndividualID == actor.IndividualId
                    && j.OrganisationID == org
                    && j.JobStateId == SharedConfig.JobStates.APPROVED
                    && (j.TerminatedDate == null || j.TerminatedDate > DateTime.UtcNow))
                .OrderByDescending(j => j.JoinedDate)
                .Select(j => j.JobPositions
                    .Where(jp => jp.ToDate == null || jp.ToDate > DateTime.UtcNow)
                    .OrderByDescending(jp => jp.FromDate)
                    .Select(jp => jp.Position.Name)
                    .FirstOrDefault())
                .FirstOrDefaultAsync(ct);
        }

        var canonicalContent = string.IsNullOrWhiteSpace(input.DocumentContentSnapshot)
            ? $"{org}|{docType}|{docId}|{slotKey}"
            : input.DocumentContentSnapshot;
        var docHash = Sha256Hex(canonicalContent);
        var verificationCode = $"SIG-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (ip?.Length > 64) ip = ip[..64];

        var opLog = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Digitally signed {docType} #{docId} ({slotLabel}) · {verificationCode}.");

        db.DocumentSignatures.Add(new DocumentSignature
        {
            OrganisationId = org,
            DocumentType = docType,
            DocumentId = docId,
            SlotKey = slotKey,
            SlotLabel = slotLabel,
            SignerUserId = actor.UserId,
            SignerIndividualId = actor.IndividualId,
            SignerName = signerName,
            SignerPosition = signerPosition,
            SignatureImageDataUrl = signatureImage,
            SignatureStyle = effectiveStyle,
            DocumentContentHash = docHash,
            VerificationCode = verificationCode,
            SignedAtUtc = DateTime.UtcNow,
            IpAddress = ip,
            Status = "Signed",
            OperationLogId = opLog.OperationLogId
        });

        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok($"Document signed ({slotLabel}) · Verification Code: {verificationCode}"));
    }

    [HttpPost("{id:int}/revoke")]
    public async Task<ActionResult<ServiceResult>> Revoke(int id, [FromBody] RevokeDocumentSignatureInput input, CancellationToken ct)
    {
        var reason = input.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            return Ok(ServiceResult.Fail("Provide a reason (up to 500 characters) for revoking this signature."));

        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        var isAdmin = actor.IsSuperAdministrator || await access.HasUserGroupAsync("Admin");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesAsync(db, ct);

        var sig = await db.DocumentSignatures.SingleOrDefaultAsync(x => x.Id == id && x.OrganisationId == org, ct);
        if (sig is null) return NotFound();
        if (sig.Status != "Signed") return Ok(ServiceResult.Fail("This signature is already revoked."));
        if (!isAdmin && sig.SignerUserId != actor.UserId)
            return Forbid();

        sig.Status = "Revoked";
        sig.RevokedAtUtc = DateTime.UtcNow;
        sig.RevokedByUserId = actor.UserId;
        sig.RevokeReason = reason;

        await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Revoked signature {sig.VerificationCode} on {sig.DocumentType} #{sig.DocumentId}: {reason}");
        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok($"Signature {sig.VerificationCode} revoked."));
    }
}
