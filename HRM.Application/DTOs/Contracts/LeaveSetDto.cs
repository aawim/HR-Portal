namespace HRM.DTOs.Contracts;

public sealed class LeaveSetDto
{
    public int LeaveTypeCount { get; set; }
    public int LeaveSetId { get; set; }
    public string Name { get; set; } = "";
    public int OrganisationId { get; set; }
    public int OperationLogId { get; set; }
    public bool IsPublic { get; set; }
    public bool IsGlobal { get; set; }
}
