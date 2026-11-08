namespace HRM.DTOs.Organisation;

public class OrganisationTeamSettingsData
{
    public List<OrganisationTeamSettingsRow> Rows { get; set; } = [];
    public List<OrganisationSettingsOption> Staff { get; set; } = [];
    public List<OrganisationSettingsOption> StructureTypes { get; set; } = [];
    public Dictionary<int, string> SupervisorIssues { get; set; } = [];
    public bool CanAssignDepartments { get; set; }
    public bool CanRemoveDepartments { get; set; }
    public bool CanEditTeams { get; set; }
}
public class OrganisationTeamSettingsRow
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public int? StructureTypeId { get; set; }
    public List<OrganisationSettingsOption> Supervisors { get; set; } = [];
    public List<OrganisationSettingsOption> Members { get; set; } = [];
}
