namespace HRM.DTOs.Contracts;

public sealed class LeaveConfigurableValueTypeDto
{
    public int LeaveConfigurableValueTypeId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Comment { get; set; }
    public bool IsJobTypeDependant { get; set; }
    public string? Unit { get; set; }
}
