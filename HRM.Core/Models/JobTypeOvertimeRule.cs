
namespace HRM.Models;

public sealed class JobTypeOvertimeRule
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int JobTypeId { get; set; }
    public bool IsAllowed { get; set; }
    public decimal MaximumWeeklyHours { get; set; }
    public decimal? MaximumDailyHours { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}
