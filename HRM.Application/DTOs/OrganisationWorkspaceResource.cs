namespace HRM.DTOs;
public class OrganisationWorkspaceResource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<string> Details { get; set; } = [];
}
