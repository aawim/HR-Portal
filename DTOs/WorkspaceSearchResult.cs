using HRM.Enum;
using HRM.Models;

namespace HRM.DTOs
{
    public class WorkspaceSearchResult
    {

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string IDCard { get; set; } = "";
        public string NameDv { get; set; } = "";
        public string Description { get; set; } = "";
        public WorkspaceEntityType EntityType { get; set; }
        public string Icon { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int BusinessEntityId { get; set; }
        public int IndividualId { get; set; }
        public int? JobId { get; set; }
        public virtual Job Job { get; set; } = null!;

    }
}
