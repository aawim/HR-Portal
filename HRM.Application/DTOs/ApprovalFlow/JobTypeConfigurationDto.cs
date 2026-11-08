namespace HRM.DTOs.ApprovalFlow;

public class JobTypeConfigurationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? DhivehiName { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SequenceNumberTypeId { get; set; }
    public string? ViewUrl { get; set; }
}

public class JobTypeWorkflowDto
{
    public int JobTypeId { get; set; }
    public int RequestTypeId { get; set; }
    public int Version { get; set; }
    public List<JobTypeStageDto> Stages { get; set; } = new();
}

public class JobTypeStageDto
{
    public string Name { get; set; } = "";
    public int StateId { get; set; }
    public string Mode { get; set; } = "Any";
    public int? MinimumApprovals { get; set; }
    public List<int> StaffUserIds { get; set; } = new();
}

public record ConfigurationOption(int Id, string Name);
public class JobTypeConfigurationData
{
    public bool CanManageCatalogues { get; set; }
    public List<JobTypeConfigurationDto> JobTypes { get; set; } = new();
    public List<ConfigurationOption> Staff { get; set; } = new();
    public List<ConfigurationOption> RequestTypes { get; set; } = new();
    public List<ConfigurationOption> States { get; set; } = new();
    public List<ConfigurationOption> NumberingTypes { get; set; } = new();
}
