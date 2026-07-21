namespace HRM.DTOs
{
    public class OrgNodeDto
    {

        public int StructureId { get; set; }
        public int? ParentId { get; set; }
        public int TypeId { get; set; }
        public string Name { get; set; } = string.Empty;

        // This is where the magic happens: A list of children nodes
        public List<OrgNodeDto> Children { get; set; } = new();


    }
}
