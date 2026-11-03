namespace HRM.Models;

/// <summary>Groups catalogue items for an organisation, such as job equipment or common consumables.</summary>
public sealed class ItemCategory
{
    public int ItemCategoryId { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int? OperationLogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

/// <summary>A catalogue entry. Assets are tracked by inventory units; consumables use stock on hand.</summary>
public sealed class Item
{
    public int ItemId { get; set; }
    public int OrganisationId { get; set; }
    public int ItemCategoryId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    // Asset or Consumable.
    public string Kind { get; set; } = "Consumable";
    public string UnitOfMeasure { get; set; } = "Each";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int? OperationLogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ItemCategory Category { get; set; } = null!;
    public ICollection<ItemUnit> Units { get; set; } = new List<ItemUnit>();
}

/// <summary>An individually tracked asset unit with its inventory number and issue/collection dates.</summary>
public sealed class ItemUnit
{
    public int ItemUnitId { get; set; }
    public int OrganisationId { get; set; }
    public int ItemId { get; set; }
    public string InventoryNumber { get; set; } = "";
    public string? SerialNumber { get; set; }
    // Available, Issued, Returned, Damaged, Lost, Retired.
    public string Status { get; set; } = "Available";
    // Good, Fair, Damaged, Unusable.
    public string Condition { get; set; } = "Good";
    public int? IssuedToIndividualId { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? CollectedDate { get; set; }
    public string? Notes { get; set; }
    public int Version { get; set; } = 1;
    public int? OperationLogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Item Item { get; set; } = null!;
}

/// <summary>An approved request issues stock or one asset unit to a staff member.</summary>
public sealed class ItemStaffRequest
{
    public int RequestId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int? ItemId { get; set; }
    public int? ItemUnitId { get; set; }
    public int Quantity { get; set; }
    public string Purpose { get; set; } = "Common";
    public string Kind { get; set; } = "Requisition";
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public Request Request { get; set; } = null!;
    public Item? Item { get; set; }
    public ItemUnit? ItemUnit { get; set; }
    public ICollection<ItemStaffRequestLine> Lines { get; set; } = new List<ItemStaffRequestLine>();
}

public sealed class ItemStaffRequestLine
{
    public int ItemStaffRequestLineId { get; set; }
    public int RequestId { get; set; }
    public int? ItemId { get; set; }
    public int? ItemUnitId { get; set; }
    public string ItemName { get; set; } = "";
    public string? InventoryNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Condition { get; set; }
    public DateOnly? ExpectedReturnDate { get; set; }
    public string? Remarks { get; set; }
    public int Quantity { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public ItemStaffRequest Request { get; set; } = null!;
    public Item? Item { get; set; }
    public ItemUnit? ItemUnit { get; set; }
}
