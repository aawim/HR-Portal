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
    public List<OrganisationSettingsOption> DepartmentTypes { get; set; } = new();
    public List<OrganisationSettingsOption> JobTypes { get; set; } = new();
}
public class OrganisationLinksDto
{
    public int ActiveStaffCount { get; set; }
    public List<OrganisationAddressOption> Addresses { get; set; } = new();
    public List<OrganisationContactDto> Contacts { get; set; } = new();
    public List<OrganisationDepartmentSummaryDto> Departments { get; set; } = new();
    public List<OrganisationDepartmentStaffDto> DepartmentStaff { get; set; } = new();
    public List<OrganisationTeamSummaryDto> Teams { get; set; } = new();
    public List<OrganisationMemberStaffDto> TeamStaff { get; set; } = new();
    public List<OrganisationGroupSummaryDto> Groups { get; set; } = new();
    public List<OrganisationMemberStaffDto> GroupStaff { get; set; } = new();
}
public record OrganisationAddressOption(int Id, string Name, int LinkId);
public record OrganisationDepartmentSummaryDto(int Id, string Name, int TypeId, string TypeName, int StateId, bool IsActive, int StaffCount = 0);
public record OrganisationDepartmentStaffDto(int IndividualId, int DepartmentId, string Name, string? EmployeeNumber, string JobType);
public record OrganisationMemberStaffDto(int ParentId, string Source, int IndividualId, string Name, string? EmployeeNumber, string? Detail = null);
public record OrganisationTeamSummaryDto(int Id, string Name, int? DepartmentId, string? DepartmentName, int StaffCount, bool IsActive);
public record OrganisationGroupSummaryDto(int Id, string Name, string? Code, string? Description, int MemberCount, bool IsDissolved, int? DepartmentId = null, string? DepartmentName = null, string Source = "StaffGroup");
public record OrganisationStaffRow(int JobId, string Name, string? EmployeeNumber, string Department, string JobType, string Status)
{
    public int IndividualId { get; init; }
}
public record UnassignedIndividualOption(int IndividualId, string Name, string? IdCardNumber);
