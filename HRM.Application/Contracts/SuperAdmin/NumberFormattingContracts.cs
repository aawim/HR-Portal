namespace HRM.Application.Contracts.SuperAdmin;

public sealed record NumberFormatOptionDto(int Id, string Name, string? Code = null);

public sealed class GlobalSequenceNumberDto
{
    public int Id { get; set; }
    public string TypeName { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string FormatString { get; set; } = "";
    public bool IsResetAnnually { get; set; }
    public int StartingNumber { get; set; } = 1;
    public int NextNumber { get; set; } = 1;
    public List<string> LinkedRequestTypes { get; set; } = [];
}

public sealed class OrganisationNumberFormatDto
{
    public int NumberFormatId { get; set; }
    public int OrganisationId { get; set; }
    public string Category { get; set; } = "Request";
    public int? SequenceNumberTypeId { get; set; }
    public string? SequenceNumberTypeName { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string FormatString { get; set; } = "{PREFIX}/{YEAR}/{NUMBER}";
    public int PaddingLength { get; set; } = 4;
    public int StartingNumber { get; set; } = 1;
    public int NextNumber { get; set; } = 1;
    public bool IsResetAnnually { get; set; } = true;
    public int? LastResetYear { get; set; }
    public bool IsActive { get; set; } = true;
    public string Preview { get; set; } = "";
}

public sealed class NumberFormattingWorkspaceDto
{
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public string OrganisationCode { get; set; } = "";
    public List<NumberFormatOptionDto> Organisations { get; set; } = [];
    public List<OrganisationNumberFormatDto> OrganisationFormats { get; set; } = [];
    public List<GlobalSequenceNumberDto> GlobalSequences { get; set; } = [];
}

public interface INumberFormattingApiClient
{
    Task<NumberFormattingWorkspaceDto> LoadAsync(int? organisationId = null, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> SaveOrganisationFormatAsync(OrganisationNumberFormatDto model, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> DeleteOrganisationFormatAsync(int organisationId, int numberFormatId, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> SaveGlobalSequenceAsync(GlobalSequenceNumberDto model, CancellationToken ct = default);
}
