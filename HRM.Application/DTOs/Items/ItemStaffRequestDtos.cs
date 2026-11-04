namespace HRM.DTOs.Items;

public sealed record RequestableItemDto(int ItemId, int OrganisationId, string Name, string Kind, string UnitOfMeasure, int Available);

public sealed class ItemStaffRequestInput
{
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public string Purpose { get; set; } = "Common";
    public string Kind { get; set; } = "Requisition";
    public string Reason { get; set; } = "";
    /// <summary>For Replacement/Upgrade: the in-use job-related record being replaced.</summary>
    public int? ReplacesRequestId { get; set; }
    public List<ItemStaffRequestLineInput> Lines { get; set; } = [];
}

public sealed class ItemStaffRequestLineInput
{
    public int? ItemId { get; set; }
    public int? ItemUnitId { get; set; }
    public string ItemName { get; set; } = "";
    public string? InventoryNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Condition { get; set; } = "Good";
    public DateOnly? ExpectedReturnDate { get; set; }
    public string? Remarks { get; set; }
    public int Quantity { get; set; } = 1;
}

public sealed record ItemStaffRequestLineDto(
    int ItemStaffRequestLineId,
    int? ItemId,
    int? ItemUnitId,
    string ItemName,
    string? InventoryNumber,
    string? SerialNumber,
    string? Condition,
    DateOnly? ExpectedReturnDate,
    string? Remarks,
    int Quantity,
    DateTime? IssuedAtUtc
);

public sealed record ItemStaffRequestEventDto(
    string Action,
    string? FromStatus,
    string ToStatus,
    string ActorName,
    string? Condition,
    string? Comment,
    DateTime CreatedAtUtc
);

/// <summary>
/// Actions is the list of action codes the current user may perform:
/// Approve, Reject, Cancel, Accept, Decline, RequestReturn, ConfirmReturn, RequestReplacement, RequestUpgrade.
/// ConfirmReturn is sent as "ConfirmReturn:{Condition}" with damage notes in the comment.
/// </summary>
public sealed record ItemStaffRequestDto(
    int RequestId,
    int StateId,
    string Reference,
    int IndividualId,
    string StaffName,
    string Purpose,
    string Kind,
    string Reason,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? IssuedAtUtc,
    DateTime? ReturnedAtUtc,
    bool CanApprove,
    bool CanCancel,
    List<ItemStaffRequestLineDto> Lines
)
{
    public int? ReplacesRequestId { get; init; }
    public string? ReplacesReference { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public DateTime? ReturnRequestedAtUtc { get; init; }
    public string? ReturnCondition { get; init; }
    public string? ReturnNotes { get; init; }
    public List<string> Actions { get; init; } = [];
    public List<ItemStaffRequestEventDto> History { get; init; } = [];
}
