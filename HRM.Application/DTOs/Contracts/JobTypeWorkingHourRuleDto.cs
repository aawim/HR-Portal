namespace HRM.DTOs.Contracts;

public sealed class JobTypeWorkingHourRuleDto
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int JobTypeId { get; set; }
    public int? RequiredWeeklyMinutes { get; set; }
    public int WorkingDaysMask { get; set; } = 31;
    public int WeekStartsOn { get; set; }
    public bool CountDutyHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}
