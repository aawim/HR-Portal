namespace HRM.DTOs.Contracts;

public sealed class LeaveConfigurableValueDto
{
    public int LeaveConfigurableValueId { get; set; }
    public int LeaveConfigurableValueTypeId { get; set; }
    public int OrganisationId { get; set; }
    public string Value { get; set; } = "";
    public bool IsValid { get; set; }
    public int? JobTypeId { get; set; }
}
