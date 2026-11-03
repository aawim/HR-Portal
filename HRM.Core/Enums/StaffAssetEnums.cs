namespace HRM.Enums;

public static class AssetIssuanceStatuses
{
    public const string Draft = "Draft";
    public const string AwaitingHandover = "AwaitingHandover";
    public const string PendingStaffAcknowledgement = "PendingStaffAcknowledgement";
    public const string PendingHRApproval = "PendingHRApproval";
    public const string ReturnedForCorrection = "ReturnedForCorrection";
    public const string Completed = "Completed";
    public const string PartiallyCompleted = "PartiallyCompleted";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        AwaitingHandover,
        PendingStaffAcknowledgement,
        PendingHRApproval,
        ReturnedForCorrection,
        Completed,
        PartiallyCompleted,
        Cancelled
    ];

    public static bool CanCancel(string status) =>
        status is Draft or AwaitingHandover or ReturnedForCorrection;
}

public static class AssetIssuanceLineStatuses
{
    public const string Proposed = "Proposed";
    public const string Reserved = "Reserved";
    public const string HandedOver = "HandedOver";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
    public const string HRApproved = "HRApproved";
    public const string ReturnedForCorrection = "ReturnedForCorrection";
    public const string Closed = "Closed";

    public static readonly IReadOnlyList<string> All =
    [
        Proposed,
        Reserved,
        HandedOver,
        Accepted,
        Rejected,
        HRApproved,
        ReturnedForCorrection,
        Closed
    ];

    public static bool HoldsInventoryBlock(string status) =>
        status is Reserved or HandedOver or Accepted or ReturnedForCorrection;
}

public static class AssetInventoryStatuses
{
    public const string Available = "Available";
    public const string Reserved = "Reserved";
    public const string PendingHandover = "PendingHandover";
    public const string Assigned = "Assigned";
    public const string ReturnPending = "ReturnPending";
    public const string Maintenance = "Maintenance";
    public const string Lost = "Lost";
    public const string Disposed = "Disposed";

    public static readonly IReadOnlyList<string> All =
    [
        Available,
        Reserved,
        PendingHandover,
        Assigned,
        ReturnPending,
        Maintenance,
        Lost,
        Disposed
    ];
}

public static class AssetCustodyStatuses
{
    public const string Active = "Active";
    public const string ReturnPending = "ReturnPending";
    public const string Returned = "Returned";
    public const string Lost = "Lost";
    public const string Transferred = "Transferred";
    public const string Closed = "Closed";

    public static readonly IReadOnlyList<string> All =
    [
        Active,
        ReturnPending,
        Returned,
        Lost,
        Transferred,
        Closed
    ];
}

public static class AssetStaffDecisions
{
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All = [Accepted, Rejected];
}

public static class AssetRejectionReasons
{
    public const string NotPhysicallyReceived = "NotPhysicallyReceived";
    public const string DamagedOrDefective = "DamagedOrDefective";
    public const string WrongSerialOrModel = "WrongSerialOrModel";
    public const string MissingAccessories = "MissingAccessories";
    public const string NotRequiredForRole = "NotRequiredForRole";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
    [
        NotPhysicallyReceived,
        DamagedOrDefective,
        WrongSerialOrModel,
        MissingAccessories,
        NotRequiredForRole,
        Other
    ];

    public static string Label(string? code) => code switch
    {
        NotPhysicallyReceived => "Not physically received",
        DamagedOrDefective => "Damaged or defective item",
        WrongSerialOrModel => "Wrong serial number or model",
        MissingAccessories => "Missing listed accessories",
        NotRequiredForRole => "Not required for assigned role",
        Other => "Other reason",
        _ => code ?? ""
    };
}

public static class AssetHrDecisionKinds
{
    public const string HRApproved = "HRApproved";
    public const string ReturnedForCorrection = "ReturnedForCorrection";
    public const string Closed = "Closed";

    public static readonly IReadOnlyList<string> All =
    [
        HRApproved,
        ReturnedForCorrection,
        Closed
    ];
}

public static class AssetReturnTypes
{
    public const string StandardReturn = "StandardReturn";
    public const string DamageReport = "DamageReport";
    public const string LossReport = "LossReport";
    public const string TransferReturn = "TransferReturn";
    public const string TerminationClearance = "TerminationClearance";

    public static readonly IReadOnlyList<string> All =
    [
        StandardReturn,
        DamageReport,
        LossReport,
        TransferReturn,
        TerminationClearance
    ];
}

public static class AssetReturnStatuses
{
    public const string ReturnPending = "ReturnPending";
    public const string Inspected = "Inspected";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        ReturnPending,
        Inspected,
        Closed,
        Cancelled
    ];
}

public static class AssetInspectionResults
{
    public const string Pending = "Pending";
    public const string GoodAvailable = "GoodAvailable";
    public const string DamagedMaintenance = "DamagedMaintenance";
    public const string LostConfirmed = "LostConfirmed";
    public const string Disposed = "Disposed";
    public const string ReplacementRequired = "ReplacementRequired";

    public static readonly IReadOnlyList<string> All =
    [
        Pending,
        GoodAvailable,
        DamagedMaintenance,
        LostConfirmed,
        Disposed,
        ReplacementRequired
    ];
}

public static class AssetTransferDecisions
{
    public const string Retain = "Retain";
    public const string Return = "Return";
    public const string ChangeOwningDepartment = "ChangeOwningDepartment";
    public const string Replace = "Replace";

    public static readonly IReadOnlyList<string> All =
    [
        Retain,
        Return,
        ChangeOwningDepartment,
        Replace
    ];
}

public static class AssetDocumentVariants
{
    public const string StandardHandoverRecord = "StandardHandoverRecord";
    public const string RestrictedSecurityRecord = "RestrictedSecurityRecord";

    public static readonly IReadOnlyList<string> All =
    [
        StandardHandoverRecord,
        RestrictedSecurityRecord
    ];
}

public static class AssetAttachmentScopes
{
    public const string EmployeeAndHr = "EmployeeAndHr";
    public const string HrOnly = "HrOnly";
    public const string RestrictedSecurity = "RestrictedSecurity";

    public static readonly IReadOnlyList<string> All =
    [
        EmployeeAndHr,
        HrOnly,
        RestrictedSecurity
    ];
}
