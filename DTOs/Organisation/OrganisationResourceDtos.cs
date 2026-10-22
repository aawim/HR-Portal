namespace HRM.DTOs.Organisation;

public enum OrganisationResourceKind { StructureTypes, Calendar, Locations, LocationTypes, OrganisationTypes, Requests }
public sealed record OrganisationResourceRow(int Id, string Name, string Detail = "", string Status = "", string? Url = null);
public sealed class OrganisationResourceData
{
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public bool IsSuperAdmin { get; set; }
    public bool CanEdit { get; set; }
    public List<OrganisationSettingsOption> Organisations { get; set; } = [];
    public List<OrganisationResourceRow> Rows { get; set; } = [];
    public List<OrganisationSettingsOption> Calendars { get; set; } = [];
}
