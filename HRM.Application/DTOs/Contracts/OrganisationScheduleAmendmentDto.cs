namespace HRM.DTOs.Contracts;

public sealed class OrganisationScheduleAmendmentDto
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
