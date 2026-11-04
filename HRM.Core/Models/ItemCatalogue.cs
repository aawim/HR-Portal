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

/// <summary>
/// Staff item custody record. Kind: Issue (HR assigns job-related items), Requisition (staff requests),
/// Replacement/Upgrade (staff asks to swap an in-use job-related item). Status follows
/// Submitted → PendingAcceptance → InUse (job related) / Received (personal) → ReturnRequested → Returned,
/// with Rejected, Cancelled and Declined as terminal outcomes. Every transition is kept in <see cref="Events"/>.
/// </summary>
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
    public string Status { get; set; } = "Submitted";
    public int? ReplacesRequestId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? ReturnRequestedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    // Condition recorded by HR when the item is checked back in: Good, Fair, Damaged, Unusable, Lost.
    public string? ReturnCondition { get; set; }
    public string? ReturnNotes { get; set; }
    public Request Request { get; set; } = null!;
    public Item? Item { get; set; }
    public ItemUnit? ItemUnit { get; set; }
    public ItemStaffRequest? ReplacesRequest { get; set; }
    public ICollection<ItemStaffRequestLine> Lines { get; set; } = new List<ItemStaffRequestLine>();
    public ICollection<ItemStaffRequestEvent> Events { get; set; } = new List<ItemStaffRequestEvent>();
}

/// <summary>Immutable history entry for a staff item record transition.</summary>
public sealed class ItemStaffRequestEvent
{
    public int ItemStaffRequestEventId { get; set; }
    public int RequestId { get; set; }
    public string Action { get; set; } = "";
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public int ActorUserId { get; set; }
    public int ActorIndividualId { get; set; }
    public string? Condition { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ItemStaffRequest Request { get; set; } = null!;
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
