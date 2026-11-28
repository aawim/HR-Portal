namespace HRM.Models;

public sealed class Slogan
{
    public int SloganId { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SloganType { get; set; } = "Text"; // "Text" | "Image"
    public string? TextContent { get; set; }
    public string? TextColor { get; set; } = "#1e3a8a";
    public string? BackgroundColor { get; set; } = "#eff6ff";
    public string? BorderColor { get; set; } = "#93c5fd";
    public string BoxStyle { get; set; } = "RoundedRectangle"; // "None" | "RoundedRectangle"
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public string Placement { get; set; } = "Footer"; // "Header" | "Footer"
    public string Alignment { get; set; } = "Center"; // "Left" | "Center" | "Right"
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int OperationLogId { get; set; }
}

public sealed class DocumentSloganAssignment
{
    public int DocumentSloganAssignmentId { get; set; }
    public int OrganisationId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public int? SloganId { get; set; } // null = explicitly None for this document type
    public int UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int OperationLogId { get; set; }

    public Slogan? Slogan { get; set; }
}
