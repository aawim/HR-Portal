using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRM.Services.Interfaces;

public sealed class DocumentStorageWriteRequest
{
    public int OrganisationId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string? DeclaredContentType { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string PermittedExtensionsCsv { get; set; } = ".pdf,.png,.jpg,.jpeg,.docx,.xlsx";
    public int MaxFileSizeMb { get; set; } = 15;
    public int ActorUserId { get; set; }
}

public sealed class DocumentStorageWriteResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long StoredObjectId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string VerifiedContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string MalwareScanStatus { get; set; } = "Clean";
    public string? MalwareScanDetails { get; set; }
}

public sealed class DocumentStorageReadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long StoredObjectId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public bool IntegrityVerified { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Central private binary storage abstraction for DHR-DMS.
/// Supports multi-instance API deployments, malware scanning, magic-byte validation,
/// immutable version objects, and SHA-256 content integrity verification.
/// Never exposes physical storage paths to the browser.
/// </summary>
public interface IDocumentStorageProvider
{
    Task<DocumentStorageWriteResult> StoreAsync(
        DocumentStorageWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<DocumentStorageReadResult> RetrieveAsync(
        long storedObjectId,
        int expectedOrganisationId,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyIntegrityAsync(
        long storedObjectId,
        int expectedOrganisationId,
        CancellationToken cancellationToken = default);
}
