
namespace HRM.Models;

public sealed class JobTypeWorkingHourRule
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int JobTypeId { get; set; }
    // Retained for existing records. Runtime requirements come from official templates.
    public int? RequiredWeeklyMinutes { get; set; }
    public int WorkingDaysMask { get; set; } = 31; // Sunday through Thursday.
    public int WeekStartsOn { get; set; }
    public bool CountDutyHours { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}


public sealed class OrganisationScheduleAmendment
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public DateTime WorkDate { get; set; }
    public TimeSpan OriginalStart { get; set; }
    public TimeSpan RevisedStart { get; set; }
    public string Reason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}


public sealed class OrganisationWorkingHourRequirement
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int? JobTypeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int RequiredDailyMinutes { get; set; }
    public string Reason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}


public sealed class WeeklyHourExceptionRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public DateTime WeekStart { get; set; }
    public int ExcusedMinutes { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
}
