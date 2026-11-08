namespace HRM.DTOs.Contracts;

public sealed class WorkingHoursSettingsDataResponse
{
    public string Organisation { get; set; } = "";
    public List<global::HRM.Services.WorkingHourOption> JobTypes { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.JobTypeWorkingHourRuleDto> Rules { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.OrganisationScheduleAmendmentDto> Amendments { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.OrganisationWorkingHourRequirementDto> Requirements { get; set; } = [];
    public bool CanConfigure { get; set; }
    public bool CanAmend { get; set; }
}
