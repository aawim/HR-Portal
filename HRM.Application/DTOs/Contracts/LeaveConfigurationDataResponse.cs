namespace HRM.DTOs.Contracts;

public sealed class LeaveConfigurationDataResponse
{
    public List<global::HRM.DTOs.Contracts.OrganisationDto> Organisations { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.LeaveConfigurableValueTypeDto> Types { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.JobTypeDto> JobTypes { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.LeaveConfigurableValueDto> Values { get; set; } = [];
}
