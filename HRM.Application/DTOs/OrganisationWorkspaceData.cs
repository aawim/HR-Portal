namespace HRM.DTOs;

public class OrganisationWorkspaceData
{
    public string Name { get; set; } = "";
    public string? DhivehiName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string Type { get; set; } = "";
    public string? Parent { get; set; }
    public int StaffCount { get; set; }
    public List<OrganisationWorkspaceGroup> Departments { get; set; } = [];
    public List<OrganisationWorkspaceGroup> Teams { get; set; } = [];
}
public record OrganisationWorkspaceGroup(int Id, string Name, int Members);
public record OrganisationWorkspaceStaff(int Id, string Name, string Department, string? EmployeeNumber, string? SapNumber, DateTime Joined);
public record OrganisationWorkspaceDocument(int Id, string Name, string Type, DateOnly? Date, int? Size);
