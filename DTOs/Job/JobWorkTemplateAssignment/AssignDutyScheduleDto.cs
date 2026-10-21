namespace HRM.DTOs.Job.JobWorkTemplateAssignment;

public sealed class AssignDutyScheduleDto
{
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public int OrganisationBusinessEntityId { get; set; }
    public string Name { get; set; } = "";
    public TimeOnly StartTime { get; set; } = new(10, 0);
    public TimeOnly EndTime { get; set; } = new(12, 0);
    public DateTime FromDate { get; set; } = DateTime.UtcNow.AddHours(5).Date;
    public DateTime? ToDate { get; set; }
    public string Period { get; set; } = "OneDay";
    public bool RequireCheckIn { get; set; } = true;
    public bool RequireCheckOut { get; set; } = true;
}
