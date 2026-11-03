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

public static class AssetServiceRequestTypes
{
    public const string FaultCorrective = "FaultCorrective";
    public const string Preventive = "Preventive";
    public const string HardwareUpgrade = "HardwareUpgrade";
    public const string ComponentReplacement = "ComponentReplacement";
    public const string AccessoryReplacement = "AccessoryReplacement";
    public const string DeviceReplacement = "DeviceReplacement";
    public const string LoanerRequest = "LoanerRequest";
    public const string TransferClearance = "TransferClearance";
    public const string OffboardingClearance = "OffboardingClearance";

    public static readonly IReadOnlyList<string> All =
    [
        FaultCorrective,
        Preventive,
        HardwareUpgrade,
        ComponentReplacement,
        AccessoryReplacement,
        DeviceReplacement,
        LoanerRequest,
        TransferClearance,
        OffboardingClearance
    ];
}

public static class AssetServiceRequestStatuses
{
    public const string Submitted = "Submitted";
    public const string Triage = "Triage";
    public const string Approved = "Approved";
    public const string InDiagnosis = "InDiagnosis";
    public const string InRepair = "InRepair";
    public const string Testing = "Testing";
    public const string ReadyForHandover = "ReadyForHandover";
    public const string PendingStaffAcknowledgement = "PendingStaffAcknowledgement";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Submitted,
        Triage,
        Approved,
        InDiagnosis,
        InRepair,
        Testing,
        ReadyForHandover,
        PendingStaffAcknowledgement,
        Completed,
        Rejected,
        Cancelled
    ];
}

public static class AssetMaintenanceJobTypes
{
    public const string Corrective = "Corrective";
    public const string Preventive = "Preventive";
    public const string Upgrade = "Upgrade";
    public const string ReplacementInspection = "ReplacementInspection";
    public const string DataSanitization = "DataSanitization";

    public static readonly IReadOnlyList<string> All =
    [
        Corrective,
        Preventive,
        Upgrade,
        ReplacementInspection,
        DataSanitization
    ];
}

public static class AssetMaintenanceJobStatuses
{
    public const string Assigned = "Assigned";
    public const string InDiagnosis = "InDiagnosis";
    public const string AwaitingParts = "AwaitingParts";
    public const string InRepair = "InRepair";
    public const string Testing = "Testing";
    public const string PassedTesting = "PassedTesting";
    public const string FailedTesting = "FailedTesting";
    public const string ReadyForHandover = "ReadyForHandover";
    public const string Completed = "Completed";
    public const string Aborted = "Aborted";

    public static readonly IReadOnlyList<string> All =
    [
        Assigned,
        InDiagnosis,
        AwaitingParts,
        InRepair,
        Testing,
        PassedTesting,
        FailedTesting,
        ReadyForHandover,
        Completed,
        Aborted
    ];
}

public static class AssetComponentTypes
{
    public const string RAM = "RAM";
    public const string StorageDrive = "StorageDrive";
    public const string Battery = "Battery";
    public const string DisplayPanel = "DisplayPanel";
    public const string Keyboard = "Keyboard";
    public const string Motherboard = "Motherboard";
    public const string WiFiAdapter = "WiFiAdapter";
    public const string PowerSupply = "PowerSupply";
    public const string CoolingModule = "CoolingModule";
    public const string Accessory = "Accessory";

    public static readonly IReadOnlyList<string> All =
    [
        RAM,
        StorageDrive,
        Battery,
        DisplayPanel,
        Keyboard,
        Motherboard,
        WiFiAdapter,
        PowerSupply,
        CoolingModule,
        Accessory
    ];
}

public static class AssetComponentStatuses
{
    public const string Installed = "Installed";
    public const string StockSpare = "StockSpare";
    public const string ReservedForJob = "ReservedForJob";
    public const string RemovedFunctional = "RemovedFunctional";
    public const string RemovedDefective = "RemovedDefective";
    public const string SanitizedForDisposal = "SanitizedForDisposal";
    public const string EWASTE_Scrapped = "EWASTE_Scrapped";

    public static readonly IReadOnlyList<string> All =
    [
        Installed,
        StockSpare,
        ReservedForJob,
        RemovedFunctional,
        RemovedDefective,
        SanitizedForDisposal,
        EWASTE_Scrapped
    ];
}

public static class AssetComponentTransactionTypes
{
    public const string InitialAssembly = "InitialAssembly";
    public const string InstalledAsUpgrade = "InstalledAsUpgrade";
    public const string InstalledAsRepair = "InstalledAsRepair";
    public const string RemovedForUpgrade = "RemovedForUpgrade";
    public const string RemovedDefective = "RemovedDefective";
    public const string StockTransferred = "StockTransferred";
    public const string DataSanitized = "DataSanitized";
    public const string DisposedEwaste = "DisposedEwaste";

    public static readonly IReadOnlyList<string> All =
    [
        InitialAssembly,
        InstalledAsUpgrade,
        InstalledAsRepair,
        RemovedForUpgrade,
        RemovedDefective,
        StockTransferred,
        DataSanitized,
        DisposedEwaste
    ];
}

public static class AssetTemporaryHandoverStatuses
{
    public const string ActiveLoaner = "ActiveLoaner";
    public const string ReturnedInGoodOrder = "ReturnedInGoodOrder";
    public const string ReturnedDamaged = "ReturnedDamaged";
    public const string Overdue = "Overdue";

    public static readonly IReadOnlyList<string> All =
    [
        ActiveLoaner,
        ReturnedInGoodOrder,
        ReturnedDamaged,
        Overdue
    ];
}

public static class AssetInspectionStages
{
    public const string PreServiceTriage = "PreServiceTriage";
    public const string PostRepairTesting = "PostRepairTesting";
    public const string ReturnInspection = "ReturnInspection";
    public const string TransferClearance = "TransferClearance";
    public const string OffboardingClearance = "OffboardingClearance";

    public static readonly IReadOnlyList<string> All =
    [
        PreServiceTriage,
        PostRepairTesting,
        ReturnInspection,
        TransferClearance,
        OffboardingClearance
    ];
}

public static class AssetInspectorDecisions
{
    public const string Passed = "Passed";
    public const string RequiresService = "RequiresService";
    public const string DamagedMaintenance = "DamagedMaintenance";
    public const string BeyondEconomicalRepair = "BeyondEconomicalRepair";
    public const string SanitizedClean = "SanitizedClean";

    public static readonly IReadOnlyList<string> All =
    [
        Passed,
        RequiresService,
        DamagedMaintenance,
        BeyondEconomicalRepair,
        SanitizedClean
    ];
}

public static class AssetReplacementTypes
{
    public const string CompleteDeviceReplacement = "CompleteDeviceReplacement";
    public const string ComponentUpgradeReplacement = "ComponentUpgradeReplacement";
    public const string AccessoryReplacement = "AccessoryReplacement";

    public static readonly IReadOnlyList<string> All =
    [
        CompleteDeviceReplacement,
        ComponentUpgradeReplacement,
        AccessoryReplacement
    ];
}

public static class AssetReplacementReasons
{
    public const string BeyondEconomicalRepair = "BeyondEconomicalRepair";
    public const string TotalLossOrTheft = "TotalLossOrTheft";
    public const string EndOfLifeUpgrade = "EndOfLifeUpgrade";
    public const string ComponentDefect = "ComponentDefect";

    public static readonly IReadOnlyList<string> All =
    [
        BeyondEconomicalRepair,
        TotalLossOrTheft,
        EndOfLifeUpgrade,
        ComponentDefect
    ];
}

public static class AssetServiceDocumentTypes
{
    public const string ServiceReport = "ServiceReport";
    public const string UpgradeCertificate = "UpgradeCertificate";
    public const string ReplacementCertificate = "ReplacementCertificate";
    public const string LoanerAgreement = "LoanerAgreement";
    public const string TransferClearanceReport = "TransferClearanceReport";
    public const string OffboardingClearanceReport = "OffboardingClearanceReport";

    public static readonly IReadOnlyList<string> All =
    [
        ServiceReport,
        UpgradeCertificate,
        ReplacementCertificate,
        LoanerAgreement,
        TransferClearanceReport,
        OffboardingClearanceReport
    ];
}

public static class AssetConfigurationTriggerEvents
{
    public const string InitialRegistration = "InitialRegistration";
    public const string PostUpgrade = "PostUpgrade";
    public const string PostRepair = "PostRepair";
    public const string ComponentReplaced = "ComponentReplaced";
    public const string TransferVerification = "TransferVerification";

    public static readonly IReadOnlyList<string> All =
    [
        InitialRegistration,
        PostUpgrade,
        PostRepair,
        ComponentReplaced,
        TransferVerification
    ];
}
