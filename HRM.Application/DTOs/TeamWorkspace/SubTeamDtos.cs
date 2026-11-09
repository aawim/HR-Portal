namespace HRM.DTOs;

public sealed class SubTeamWorkspaceDto
{
    public int ParentTeamId { get; set; }
    public string ParentTeamName { get; set; } = "";
    public bool CanManage { get; set; }
    public List<SubTeamDto> SubTeams { get; set; } = [];
    public List<SubTeamOptionDto> Structures { get; set; } = [];
    public List<SubTeamOptionDto> Staff { get; set; } = [];
}

public sealed record SubTeamOptionDto(int Id, string Name);

public sealed class SubTeamDto
{
    public int TeamId { get; set; }
    public int ParentTeamId { get; set; }
    public string Name { get; set; } = "";
    public string NameDhivehi { get; set; } = "";
    public int OrganisationId { get; set; }
    public int? OrganisationStructureId { get; set; }
    public string OrganisationStructureName { get; set; } = "";
    public bool OvertimeEnabled { get; set; }
    public decimal? MaximumDailyOvertimeHours { get; set; }
    public decimal? MaximumWeeklyOvertimeHours { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SubTeamMemberDto> Members { get; set; } = [];
    public List<TeamScheduleAdjustmentDto> Adjustments { get; set; } = [];
}

public sealed class SubTeamMemberDto
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = "";
    public string Role { get; set; } = "Member";
    public DateTime StartDate { get; set; } = DateTime.Today;
}

public sealed class SaveSubTeamDto
{
    public int TeamId { get; set; }
    public int ParentTeamId { get; set; }
    public string Name { get; set; } = "";
    public string? NameDhivehi { get; set; }
    public int? OrganisationStructureId { get; set; }
    public bool OvertimeEnabled { get; set; }
    public decimal? MaximumDailyOvertimeHours { get; set; }
    public decimal? MaximumWeeklyOvertimeHours { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaveSubTeamMemberDto
{
    public int TeamId { get; set; }
    public int StaffId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTime StartDate { get; set; } = DateTime.Today;
}

public sealed class TeamScheduleAdjustmentDto
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public DateTime WorkDate { get; set; } = DateTime.Today;
    public TimeSpan? RevisedCheckIn { get; set; }
    public TimeSpan? RevisedCheckOut { get; set; }
    public string Reason { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}
