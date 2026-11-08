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
    public List<OrganisationOverviewStaff> Workforce { get; set; } = [];
    public List<OrganisationOverviewLeave> StaffOnLeave { get; set; } = [];
    public bool CanViewLeaveDetails { get; set; }
    public bool HasWorkFromHomeRequestType { get; set; }
    public List<int> WorkingFromHomeStaffIds { get; set; } = [];
    public List<int> OnLeaveStaffIds { get; set; } = [];
}
public record OrganisationOverviewStaff(int IndividualId, int DepartmentId, int JobTypeId, string JobType, int GenderTypeId);
public record OrganisationOverviewLeave(int LeaveId, int IndividualId, int DepartmentId, int JobTypeId, string Name, string LeaveType, DateTime FromDate, DateTime ToDate, int Duration);
public record OrganisationWorkspaceGroup(int Id, string Name, int Members);
public record OrganisationWorkspaceStaff(int Id, string Name, string Department, string? EmployeeNumber, string? SapNumber, DateTime Joined);
public record OrganisationWorkspaceDocument(int Id, string Name, string Type, DateOnly? Date, int? Size);
