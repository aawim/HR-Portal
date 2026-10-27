using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Models.Archives;
using HRM.DTOs;
namespace HRM.Services;

public sealed record OrganisationDocumentTypesData(int OrganisationId, string OrganisationName, bool CanEdit, List<OrganisationDocumentType> Types);

public sealed class OrganisationDocumentType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? DhivehiName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsImage { get; set; }
    public bool ShouldBeColor { get; set; }
    public int? ImageMinimumResolution { get; set; }
    public bool IsShared { get; set; }
    public bool IsSystem { get; set; }
    public string? Version { get; set; }
}
