using HRM.DTOs.Organisation;

namespace HRM.DTOs.Items;

public sealed class ItemCatalogueData
{
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public bool IsSuperAdmin { get; set; }
    public bool CanEdit { get; set; }
    public List<OrganisationSettingsOption> Organisations { get; set; } = [];
    public List<ItemCategoryDto> Categories { get; set; } = [];
    public List<ItemDto> Items { get; set; } = [];
}

public sealed record ItemCategoryDto(int ItemCategoryId, string Name, string Description, bool IsActive, int ItemCount, int Version);

public sealed record ItemDto(int ItemId, int ItemCategoryId, string CategoryName, string Name, string Description,
    string Kind, string UnitOfMeasure, string? Brand, string? Model, int QuantityOnHand, int ReorderLevel,
    int UnitCount, int AvailableUnitCount, bool IsActive, int Version);

public sealed record ItemUnitDto(int ItemUnitId, int ItemId, string InventoryNumber, string? SerialNumber,
    string Status, string Condition, int? IssuedToIndividualId, string? IssuedToName,
    DateOnly? IssuedDate, DateOnly? CollectedDate, string? Notes, int Version);

public sealed class ItemCategoryInput
{
    public int ItemCategoryId { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}

public sealed class ItemInput
{
    public int ItemId { get; set; }
    public int OrganisationId { get; set; }
    public int ItemCategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Kind { get; set; } = "Consumable";
    public string UnitOfMeasure { get; set; } = "Each";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}

public sealed class ItemUnitInput
{
    public int ItemUnitId { get; set; }
    public int OrganisationId { get; set; }
    public int ItemId { get; set; }
    public string InventoryNumber { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string Status { get; set; } = "Available";
    public string Condition { get; set; } = "Good";
    public int? IssuedToIndividualId { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? CollectedDate { get; set; }
    public string? Notes { get; set; }
    public int Version { get; set; }
}
