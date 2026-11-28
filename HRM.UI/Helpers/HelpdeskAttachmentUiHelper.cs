using HRM.DTOs.Helpdesk;
using Microsoft.AspNetCore.Components.Forms;

namespace HRM.UI.Helpers;

public sealed class StagedHelpdeskAttachment
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public bool IsImage { get; set; }
    public string Base64Data { get; set; } = "";
    public string? PreviewDataUrl { get; set; }

    public HelpdeskAttachmentUploadDto ToUploadDto() => new()
    {
        FileName = FileName,
        ContentType = ContentType,
        Base64Data = Base64Data
    };
}

public static class HelpdeskAttachmentUiHelper
{
    public const int MaxFiles = 4;
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    public const string AcceptFilter = ".png,.jpg,.jpeg,.webp,.gif,.pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.zip";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".zip"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif"
    };

    public static async Task<string?> AddFilesAsync(InputFileChangeEventArgs e, List<StagedHelpdeskAttachment> target)
    {
        var files = e.GetMultipleFiles(MaxFiles);
        foreach (var file in files)
        {
            if (target.Count >= MaxFiles)
                return $"You can attach up to {MaxFiles} files per message.";

            var ext = Path.GetExtension(file.Name);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
                return $"File type '{ext}' is not supported. Allowed: images, PDF, Office docs, CSV, TXT, and ZIP.";

            if (file.Size <= 0)
                return $"'{file.Name}' is empty.";

            if (file.Size > MaxFileSizeBytes)
                return $"'{file.Name}' exceeds the 5 MB file size limit.";

            using var ms = new MemoryStream();
            await file.OpenReadStream(MaxFileSizeBytes).CopyToAsync(ms);
            var bytes = ms.ToArray();
            var b64 = Convert.ToBase64String(bytes);
            var isImage = ImageExtensions.Contains(ext);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? (isImage ? $"image/{ext.TrimStart('.').ToLowerInvariant().Replace("jpg", "jpeg")}" : "application/octet-stream")
                : file.ContentType;

            target.Add(new StagedHelpdeskAttachment
            {
                FileName = file.Name,
                ContentType = contentType,
                SizeBytes = bytes.LongLength,
                IsImage = isImage,
                Base64Data = b64,
                PreviewDataUrl = isImage ? $"data:{contentType};base64,{b64}" : null
            });
        }

        return null;
    }

    public static string? AddClipboardImage(string fileName, string contentType, string dataUrlOrBase64, List<StagedHelpdeskAttachment> target)
    {
        if (target.Count >= MaxFiles)
            return $"You can attach up to {MaxFiles} files per message.";

        if (string.IsNullOrWhiteSpace(dataUrlOrBase64))
            return "Clipboard image was empty.";

        var b64 = dataUrlOrBase64;
        var comma = b64.IndexOf(',');
        if (b64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
            b64 = b64[(comma + 1)..];

        var approxBytes = (long)(b64.Length * 0.75);
        if (approxBytes > MaxFileSizeBytes)
            return "Pasted screenshot exceeds the 5 MB file size limit.";

        var mime = string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType.Trim();
        target.Add(new StagedHelpdeskAttachment
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? "screenshot.png" : fileName.Trim(),
            ContentType = mime,
            SizeBytes = approxBytes,
            IsImage = true,
            Base64Data = b64,
            PreviewDataUrl = $"data:{mime};base64,{b64}"
        });

        return null;
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.##} MB";
    }

    public static string FileIcon(string fileName, bool isImage)
    {
        if (isImage) return "bi-image";
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "bi-file-earmark-pdf",
            ".doc" or ".docx" => "bi-file-earmark-word",
            ".xls" or ".xlsx" or ".csv" => "bi-file-earmark-spreadsheet",
            ".zip" => "bi-file-earmark-zip",
            ".txt" => "bi-file-earmark-text",
            _ => "bi-paperclip"
        };
    }
}
