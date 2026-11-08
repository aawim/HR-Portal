namespace HRM.DTOs.Organisation;

public class OrganisationSettingsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? DhivehiName { get; set; }
    public string RegistrationNumber { get; set; } = "";
    public DateTime? RegistrationDate { get; set; }
    public int TypeId { get; set; }
    public int CountryId { get; set; }
    public int? ParentId { get; set; }
}
public record OrganisationSettingsOption(int Id, string Name);
public record OrganisationAreaOption(int Id, int ParentId, string Name);
public class OrganisationContactDto
{
    public int Id { get; set; }
    public int TypeId { get; set; }
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
}
public class OrganisationAddressDto
{
    public int LinkId { get; set; }
    public int? ExistingLocationId { get; set; }
    public int UsageId { get; set; } = 3;
    public int BaseTypeId { get; set; }
    public int CountryId { get; set; }
    public int? IslandId { get; set; }
    public int? WardId { get; set; }
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string? Street { get; set; }
    public string? Floor { get; set; }
    public string? PostCode { get; set; }
}
public class OrganisationSettingsData
{
    public List<OrganisationSettingsDto> Organisations { get; set; } = new();
    public List<OrganisationSettingsOption> Types { get; set; } = new();
    public List<OrganisationSettingsOption> Countries { get; set; } = new();
    public List<OrganisationSettingsOption> AddressTypes { get; set; } = new();
    public List<OrganisationSettingsOption> AddressUsages { get; set; } = new();
    public List<OrganisationSettingsOption> ContactTypes { get; set; } = new();
    public List<OrganisationAreaOption> Islands { get; set; } = new();
    public List<OrganisationAreaOption> Wards { get; set; } = new();
}
public class OrganisationLinksDto
{
    public List<OrganisationAddressOption> Addresses { get; set; } = new();
    public List<OrganisationContactDto> Contacts { get; set; } = new();
}
public record OrganisationAddressOption(int Id, string Name, int LinkId);
public record OrganisationStaffRow(int JobId, string Name, string? EmployeeNumber, string Department, string JobType, string Status)
{
    public int IndividualId { get; init; }
}
