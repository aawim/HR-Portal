namespace HRM.DTOs.Items;

public sealed record RequestableItemDto(int ItemId, int OrganisationId, string Name, string Kind, string UnitOfMeasure, int Available);

public sealed class ItemStaffRequestInput
{
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public string Purpose { get; set; } = "Common";
    public string Kind { get; set; } = "Requisition";
    public string Reason { get; set; } = "";
    public List<ItemStaffRequestLineInput> Lines { get; set; } = [];
}

public sealed class ItemStaffRequestLineInput
{
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; } = 1;
}

public sealed record ItemStaffRequestLineDto(int ItemStaffRequestLineId, string ItemName, int Quantity, DateTime? IssuedAtUtc);

public sealed record ItemStaffRequestDto(int RequestId, int StateId, string Reference, int IndividualId,
    string StaffName, string Purpose, string Kind,
    string Reason, string Status, DateTime CreatedAtUtc, DateTime? IssuedAtUtc,
    bool CanApprove, bool CanCancel, List<ItemStaffRequestLineDto> Lines);
