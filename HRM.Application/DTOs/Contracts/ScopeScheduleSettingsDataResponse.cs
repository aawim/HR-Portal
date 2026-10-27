namespace HRM.DTOs.Contracts;

public sealed class ScopeScheduleSettingsDataResponse
{
    public List<global::HRM.Services.ScopeScheduleStructureOption> Structures { get; set; } = [];
    public List<global::HRM.Services.ScopeScheduleOption> Groups { get; set; } = [];
    public List<global::HRM.Services.ScopeScheduleOption> Templates { get; set; } = [];
    public List<global::HRM.DTOs.Contracts.ScopeScheduleAssignmentDto> Assignments { get; set; } = [];
}
