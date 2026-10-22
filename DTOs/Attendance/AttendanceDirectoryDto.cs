namespace HRM.DTOs.Attendance;

public sealed class AttendanceDirectoryDto
{
    public string Organisation { get; set; } = "";
    public List<AttendanceDirectoryOption> Departments { get; set; } = [];
    public List<AttendanceDirectoryOption> DepartmentTypes { get; set; } = [];
    public List<AttendanceDirectoryOption> Teams { get; set; } = [];
    public List<AttendanceDirectoryStaff> Staff { get; set; } = [];
}
public sealed record AttendanceDirectoryOption(int Id, string Name, int? TypeId = null);
public sealed record AttendanceDirectoryStaff(int Id, string Name, List<int> DepartmentIds, List<int> TeamIds);
