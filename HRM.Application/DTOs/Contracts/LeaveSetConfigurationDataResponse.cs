namespace HRM.DTOs.Contracts;

public sealed class LeaveSetConfigurationDataResponse
{
    public List<global::HRM.DTOs.Contracts.OrganisationDto> Organisations { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.LeaveSetDto> Sets { get; set; } = [];
    public bool IsSuperAdmin { get; set; }
}
