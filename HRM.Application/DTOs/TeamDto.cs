namespace HRM.DTOs
{
    public sealed class MyOrganisationStructureDto
    {
        public int StructureId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
    }

    public class TeamDto
    {
        public int TeamId { get; set; }
        public int? ParentTeamId { get; set; }
        public int? OrganisationStructureId { get; set; }
        public int? OrganisationStructureTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameDhivehi { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IsValid { get; set; }
        public bool IsSuperVisor { get; set; }
        public int SubTeamCount { get; set; }

        public List<TeamStaffDto> Staffs { get; set; } = new();
    }
}
