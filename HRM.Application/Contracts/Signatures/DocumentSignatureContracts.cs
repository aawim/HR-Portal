using System;
using System.Collections.Generic;

namespace HRM.Application.Contracts.Signatures;

public sealed record UserSignatureDto(
    int Id,
    int UserId,
    string SignatureDataUrl,
    DateTime UpdatedAtUtc,
    bool HasPin = false,
    string SignatureStyle = "Canvas");

public sealed record SaveUserSignatureInput(string SignatureDataUrl, string SignatureStyle = "Canvas");
public sealed record SetSignatureStyleInput(string SignatureStyle);
public sealed record SetSignaturePinInput(string Pin, string? CurrentPin = null);
public sealed record VerifySignaturePinInput(string Pin);

public sealed record DocumentSignatureDto(
    int Id,
    string DocumentType,
    string DocumentId,
    string SlotKey,
    string SlotLabel,
    int SignerUserId,
    string SignerName,
    string? SignerPosition,
    string SignatureImageDataUrl,
    string DocumentContentHash,
    string VerificationCode,
    DateTime SignedAtUtc,
    string Status,
    bool CanRevoke = false,
    string SignatureStyle = "Canvas");

public sealed record SignDocumentInput(
    string DocumentType,
    string DocumentId,
    string SlotKey,
    string SlotLabel,
    string DocumentContentSnapshot,
    string? SignerPositionOverride = null,
    string? InlineSignatureDataUrl = null,
    string? Pin = null);

public sealed record RevokeDocumentSignatureInput(string Reason);

public sealed record VerifySignatureDto(
    bool IsValid,
    string VerificationCode,
    string DocumentType,
    string DocumentId,
    string SlotLabel,
    string SignerName,
    string? SignerPosition,
    string? OrganisationName,
    DateTime SignedAtUtc,
    string DocumentContentHash,
    string Status,
    string? RevokeReason = null);
