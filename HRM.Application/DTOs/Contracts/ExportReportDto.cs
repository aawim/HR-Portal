namespace HRM.DTOs.Contracts;

public enum ExportCellKind { Empty, Text, Number, Boolean, DateTime }
public sealed record ExportCellDto(ExportCellKind Kind, string? Text = null, decimal? Number = null,
    bool? Boolean = null, DateTime? DateTime = null);
public sealed record ExportTableDto(string Name, string[] Headers, List<ExportCellDto[]> Rows);
public sealed record ExportReportDto(string Title, string Description, List<ExportTableDto> Tables);
