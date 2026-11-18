namespace HRM.Services.Contracts;

public abstract class SloganServiceContracts
{
    public static class DocumentTypes
    {
        public const string LeaveChit = "LeaveChit";
        public const string SalarySlip = "SalarySlip";
        public const string Certificate = "Certificate";
        public const string DmsDocument = "DmsDocument";
        public const string StaffTransfer = "StaffTransfer";
        public const string StaffSeparation = "StaffSeparation";
        public const string AssetClearance = "AssetClearance";

        public static readonly IReadOnlyList<(string Code, string Label)> All =
        [
            (LeaveChit, "Leave Chit"),
            (SalarySlip, "Salary Slip / Payslip"),
            (Certificate, "Certificates"),
            (DmsDocument, "DMS Official Documents"),
            (StaffTransfer, "Staff Transfer Form"),
            (StaffSeparation, "Staff Separation Form"),
            (AssetClearance, "Asset Clearance Form")
        ];
    }

    public sealed class SloganDto
    {
        public int SloganId { get; set; }
        public int OrganisationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SloganType { get; set; } = "Text";
        public string? TextContent { get; set; }
        public string? TextColor { get; set; } = "#1e3a8a";
        public string? BackgroundColor { get; set; } = "#eff6ff";
        public string? BorderColor { get; set; } = "#93c5fd";
        public string BoxStyle { get; set; } = "RoundedRectangle";
        public string? ImageBase64 { get; set; }
        public string? ImageContentType { get; set; }
        public string Placement { get; set; } = "Footer";
        public string Alignment { get; set; } = "Center";
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class DocumentSloganAssignmentDto
    {
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentLabel { get; set; } = string.Empty;
        public int? SloganId { get; set; }
        public bool HasExplicitAssignment { get; set; }
        public string? EffectiveSloganName { get; set; }
    }

    public sealed class SloganWorkspaceDto
    {
        public string OrganisationName { get; set; } = string.Empty;
        public List<SloganDto> Slogans { get; set; } = [];
        public List<DocumentSloganAssignmentDto> Assignments { get; set; } = [];
    }

    public sealed class SloganSaveRequest
    {
        public int SloganId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SloganType { get; set; } = "Text";
        public string? TextContent { get; set; }
        public string? TextColor { get; set; } = "#1e3a8a";
        public string? BackgroundColor { get; set; } = "#eff6ff";
        public string? BorderColor { get; set; } = "#93c5fd";
        public string BoxStyle { get; set; } = "RoundedRectangle";
        public string? ImageBase64 { get; set; }
        public string? ImageContentType { get; set; }
        public string Placement { get; set; } = "Footer";
        public string Alignment { get; set; } = "Center";
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public interface IOperations
    {
        Task<SloganWorkspaceDto> LoadAsync();
        Task<SloganDto> SaveSloganAsync(SloganSaveRequest request);
        Task DeleteSloganAsync(int sloganId);
        Task SetDefaultSloganAsync(int? sloganId);
        Task SaveDocumentAssignmentAsync(string documentType, int? sloganId);
        Task<SloganDto?> ResolveForDocumentAsync(string documentType);
    }
}
