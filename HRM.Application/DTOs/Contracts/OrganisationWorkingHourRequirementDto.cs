namespace HRM.DTOs.Contracts;

public sealed class OrganisationWorkingHourRequirementDto
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
