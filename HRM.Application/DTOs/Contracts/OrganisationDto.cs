namespace HRM.DTOs.Contracts;

public sealed class OrganisationDto
{
    public int BusinessEntityID { get; set; }
    public int OrganisationTypeId { get; set; }
    public string? RegistrationNumber { get; set; }
    public string OrganisationName { get; set; } = "";
    public string? OrganisationNameDhivehi { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public int CountryId { get; set; }
    public int? ParentOrganisationBusinessEntityId { get; set; }
    public int? CscofficePimaryKey { get; set; }
}
