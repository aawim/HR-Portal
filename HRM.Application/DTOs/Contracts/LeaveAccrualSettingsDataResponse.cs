namespace HRM.DTOs.Contracts;

public sealed class LeaveAccrualSettingsDataResponse
{
    public int OrganisationId { get; set; }
    public string Organisation { get; set; } = "";
    public List<global::HRM.Services.Leave.AccrualOption> JobTypes { get; set; } = [];
    public List<global::HRM.Services.Leave.AccrualOption> LeaveTypes { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.JobTypeLeaveAccrualRuleDto> Rules { get; set; } = [];
}
