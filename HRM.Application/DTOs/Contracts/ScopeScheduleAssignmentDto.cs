namespace HRM.DTOs.Contracts;

public sealed class ScopeScheduleAssignmentDto
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int? OrganisationStructureId { get; set; }
    public int? GroupId { get; set; }
    public int WorkTemplateId { get; set; }
    public string SchedulePurpose { get; set; } = "Official";
    public string ScheduleMode { get; set; } = "Standard";
    public int WorkingDaysMask { get; set; } = 31;
    public DateTime? RotationAnchorDate { get; set; }
    public string? RotationTemplateIds { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}
