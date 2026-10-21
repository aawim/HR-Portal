namespace HRM.DTOs.Leave;

public sealed class LeaveFlowConfigurationDto
{
    public int JobTypeId { get; set; }
    public int RequestTypeId { get; set; }
    public int HrUserGroupId { get; set; }
    public int SupervisorUserGroupId { get; set; }
    public bool SupervisorFirst { get; set; } = true;
    public List<LeaveFlowStageDto>? Stages { get; set; }
    public int Version { get; set; }
    public bool ImportedFromLegacy { get; set; }
}

public sealed class LeaveFlowSettingsData
{
    public string Organisation { get; set; } = "";
    public List<LeaveFlowOption> JobTypes { get; set; } = [];
    public List<LeaveFlowOption> RequestTypes { get; set; } = [];
    public List<LeaveFlowOption> UserGroups { get; set; } = [];
    public List<LeaveFlowOption> States { get; set; } = [];
}
public sealed class LeaveFlowStageDto
{
    public string Name { get; set; } = "";
    public int UserGroupId { get; set; }
    public bool ApplicantSupervisorOnly { get; set; }
    public int StateId { get; set; }
}
public sealed record LeaveFlowOption(int Id, string Name);
