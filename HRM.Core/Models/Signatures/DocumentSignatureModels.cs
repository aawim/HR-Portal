using System;

namespace HRM.Models.Signatures;

public sealed class UserSignature
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int IndividualId { get; set; }
    public string SignatureDataUrl { get; set; } = string.Empty;
    public string ContentSha256 { get; set; } = string.Empty;
    public string? PinHash { get; set; }
    public string? PinSalt { get; set; }
    public int PinFailedAttempts { get; set; }
    public DateTime? PinLockedUntilUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public sealed class DocumentSignature
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string SlotKey { get; set; } = string.Empty;
    public string SlotLabel { get; set; } = string.Empty;
    public int SignerUserId { get; set; }
    public int SignerIndividualId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string? SignerPosition { get; set; }
    public string SignatureImageDataUrl { get; set; } = string.Empty;
    public string DocumentContentHash { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public DateTime SignedAtUtc { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string Status { get; set; } = "Signed";
    public DateTime? RevokedAtUtc { get; set; }
    public int? RevokedByUserId { get; set; }
    public string? RevokeReason { get; set; }
    public int? OperationLogId { get; set; }
}
