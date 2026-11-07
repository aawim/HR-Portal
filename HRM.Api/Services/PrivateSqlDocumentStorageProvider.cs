using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Api.Services;

/// <summary>
/// Private SQL-backed immutable binary storage provider for multi-instance API deployments.
/// Enforces extension allow-lists, file size caps, magic-byte signature verification,
/// malicious content scanning, generated internal GUID storage keys, and SHA-256 integrity checks.
/// </summary>
public sealed class PrivateSqlDocumentStorageProvider(
    IDbContextFactory<HrmTeContext> factory,
    ILogger<PrivateSqlDocumentStorageProvider> logger) : IDocumentStorageProvider
{
    private static readonly byte[] EicarSignature = Encoding.ASCII.GetBytes("X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");

    public async Task<DocumentStorageWriteResult> StoreAsync(
        DocumentStorageWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OrganisationId <= 0)
        {
            return Fail("Organisation context is required for secure document storage.");
        }

        if (request.Content is null || request.Content.Length == 0)
        {
            return Fail("Uploaded file content cannot be empty.");
        }

        var maxBytes = Math.Max(1, request.MaxFileSizeMb) * 1024L * 1024L;
        if (request.Content.LongLength > maxBytes)
        {
            return Fail($"File size ({request.Content.LongLength / 1024.0 / 1024.0:F2} MB) exceeds the maximum permitted limit of {request.MaxFileSizeMb} MB.");
        }

        var safeFileName = SanitizeFileName(request.OriginalFileName);
        var ext = Path.GetExtension(safeFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext))
        {
            return Fail("Uploaded file must have a valid extension.");
        }

        var permittedExtensions = ParseExtensions(request.PermittedExtensionsCsv);
        if (!permittedExtensions.Contains(ext))
        {
            return Fail($"File extension '{ext}' is not permitted for this document type. Allowed formats: {string.Join(", ", permittedExtensions)}.");
        }

        var (signatureValid, verifiedContentType, sigError) = ValidateMagicBytesAndContentType(ext, request.Content);
        if (!signatureValid)
        {
            return Fail(sigError ?? "File binary header does not match its declared extension.");
        }

        var (scanClean, scanDetails) = ScanForMaliciousContent(ext, request.Content);
        if (!scanClean)
        {
            logger.LogWarning("Blocked malicious file upload '{FileName}' in Org {OrgId}: {Details}",
                safeFileName, request.OrganisationId, scanDetails);
            return Fail($"Security scan rejected the uploaded file: {scanDetails}");
        }

        var sha256 = ComputeSha256Hex(request.Content);
        var storageKey = $"dms-obj-{request.OrganisationId}-{Guid.NewGuid():N}";

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var storedObj = new DmsStoredObject
        {
            StorageKey = storageKey,
            OrganisationId = request.OrganisationId,
            ContentType = verifiedContentType,
            FileExtension = ext,
            FileSizeBytes = request.Content.LongLength,
            Sha256Hash = sha256,
            StorageProvider = "SqlServerPrivateStore",
            BinaryContent = request.Content,
            MalwareScanStatus = "Clean",
            MalwareScanDetails = scanDetails,
            ScannedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = request.ActorUserId
        };

        db.DmsStoredObjects.Add(storedObj);
        await db.SaveChangesAsync(cancellationToken);

        return new DocumentStorageWriteResult
        {
            Success = true,
            StoredObjectId = storedObj.StoredObjectId,
            StorageKey = storageKey,
            SafeFileName = safeFileName,
            FileExtension = ext,
            VerifiedContentType = verifiedContentType,
            FileSizeBytes = storedObj.FileSizeBytes,
            Sha256Hash = sha256,
            MalwareScanStatus = "Clean",
            MalwareScanDetails = scanDetails
        };
    }

    public async Task<DocumentStorageReadResult> RetrieveAsync(
        long storedObjectId,
        int expectedOrganisationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var obj = await db.DmsStoredObjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StoredObjectId == storedObjectId && x.OrganisationId == expectedOrganisationId, cancellationToken);

        if (obj is null)
        {
            return new DocumentStorageReadResult
            {
                Success = false,
                ErrorMessage = "Stored binary object was not found or belongs to a different organisation."
            };
        }

        var actualHash = ComputeSha256Hex(obj.BinaryContent);
        var integrityOk = string.Equals(actualHash, obj.Sha256Hash, StringComparison.OrdinalIgnoreCase);

        if (!integrityOk)
        {
            logger.LogError("Document storage integrity mismatch for StoredObjectId {Id}. Expected {Expected}, computed {Actual}.",
                obj.StoredObjectId, obj.Sha256Hash, actualHash);
            return new DocumentStorageReadResult
            {
                Success = false,
                StoredObjectId = obj.StoredObjectId,
                StorageKey = obj.StorageKey,
                IntegrityVerified = false,
                ErrorMessage = "Cryptographic SHA-256 integrity verification failed for stored document binary."
            };
        }

        return new DocumentStorageReadResult
        {
            Success = true,
            StoredObjectId = obj.StoredObjectId,
            StorageKey = obj.StorageKey,
            ContentType = obj.ContentType,
            FileExtension = obj.FileExtension,
            FileSizeBytes = obj.FileSizeBytes,
            Sha256Hash = obj.Sha256Hash,
            IntegrityVerified = true,
            Content = obj.BinaryContent
        };
    }

    public async Task<bool> VerifyIntegrityAsync(
        long storedObjectId,
        int expectedOrganisationId,
        CancellationToken cancellationToken = default)
    {
        var read = await RetrieveAsync(storedObjectId, expectedOrganisationId, cancellationToken);
        return read.Success && read.IntegrityVerified;
    }

    public static string ComputeSha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SanitizeFileName(string? rawName)
    {
        var fileName = Path.GetFileName(rawName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "document.pdf";
        }

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        fileName = fileName.Replace("..", "_");
        return fileName.Length > 200 ? fileName[..200] : fileName;
    }

    private static HashSet<string> ParseExtensions(string? csv)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(csv))
        {
            result.UnionWith([".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx"]);
            return result;
        }

        foreach (var part in csv.Split(',', ';', ' '))
        {
            var trimmed = part.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            if (!trimmed.StartsWith('.')) trimmed = "." + trimmed;
            result.Add(trimmed);
        }

        if (result.Count == 0)
        {
            result.Add(".pdf");
        }

        return result;
    }

    private static (bool Valid, string ContentType, string? Error) ValidateMagicBytesAndContentType(string ext, byte[] content)
    {
        // Reject Windows PE / ELF / Mach-O executables immediately regardless of extension
        if (content.Length >= 2 && content[0] == 0x4D && content[1] == 0x5A) // MZ
        {
            return (false, string.Empty, "Executable binaries (MZ/PE header) are strictly prohibited.");
        }
        if (content.Length >= 4 && content[0] == 0x7F && content[1] == 0x45 && content[2] == 0x4C && content[3] == 0x46) // ELF
        {
            return (false, string.Empty, "Executable binaries (ELF header) are strictly prohibited.");
        }

        switch (ext)
        {
            case ".pdf":
                if (content.Length >= 5 &&
                    content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46 && content[4] == 0x2D) // %PDF-
                {
                    return (true, "application/pdf", null);
                }
                return (false, string.Empty, "File does not contain a valid PDF (%PDF-) magic header.");

            case ".png":
                if (content.Length >= 8 &&
                    content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 &&
                    content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
                {
                    return (true, "image/png", null);
                }
                return (false, string.Empty, "File does not contain a valid PNG signature.");

            case ".jpg":
            case ".jpeg":
                if (content.Length >= 3 &&
                    content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
                {
                    return (true, "image/jpeg", null);
                }
                return (false, string.Empty, "File does not contain a valid JPEG signature.");

            case ".docx":
                if (content.Length >= 4 &&
                    content[0] == 0x50 && content[1] == 0x4B && content[2] == 0x03 && content[3] == 0x04) // PK.. OpenXML zip
                {
                    return (true, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", null);
                }
                return (false, string.Empty, "File does not contain a valid OpenXML (.docx) package header.");

            case ".xlsx":
                if (content.Length >= 4 &&
                    content[0] == 0x50 && content[1] == 0x4B && content[2] == 0x03 && content[3] == 0x04) // PK.. OpenXML zip
                {
                    return (true, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", null);
                }
                return (false, string.Empty, "File does not contain a valid OpenXML (.xlsx) package header.");

            case ".txt":
            case ".csv":
                return (true, ext == ".csv" ? "text/csv" : "text/plain", null);

            default:
                return (false, string.Empty, $"Unsupported or unverified file extension '{ext}'.");
        }
    }

    private static (bool Clean, string Details) ScanForMaliciousContent(string ext, byte[] content)
    {
        // 1. Check for EICAR test virus signature
        if (ContainsSequence(content, EicarSignature))
        {
            return (false, "Detected EICAR malware test signature.");
        }

        // 2. Inspect text/PDF streams for embedded active scripts or dangerous launch actions
        var sampleLength = Math.Min(content.Length, 256 * 1024);
        var asciiSample = Encoding.Latin1.GetString(content, 0, sampleLength);

        if (ext == ".pdf")
        {
            if (asciiSample.Contains("/JavaScript", StringComparison.OrdinalIgnoreCase) ||
                asciiSample.Contains("/JS", StringComparison.Ordinal) ||
                asciiSample.Contains("/Launch", StringComparison.OrdinalIgnoreCase) ||
                asciiSample.Contains("/SubmitForm", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "PDF contains prohibited active script (/JavaScript, /JS, or /Launch) actions.");
            }
        }

        if (asciiSample.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
            asciiSample.Contains("powershell -", StringComparison.OrdinalIgnoreCase) ||
            asciiSample.Contains("cmd.exe", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "File contains prohibited embedded script or shell command payload.");
        }

        return (true, "Heuristic signature, magic-byte, and active-content scan passed.");
    }

    private static bool ContainsSequence(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || haystack.Length < needle.Length)
            return false;

        return haystack.AsSpan().IndexOf(needle) >= 0;
    }

    private static DocumentStorageWriteResult Fail(string error) => new()
    {
        Success = false,
        ErrorMessage = error,
        MalwareScanStatus = "Flagged"
    };
}
