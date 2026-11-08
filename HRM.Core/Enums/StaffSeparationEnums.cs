namespace HRM.Enums;

/// <summary>
/// Statuses for employee separation lifecycle.
/// </summary>
public static class StaffSeparationStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string HandoverInProgress = "HandoverInProgress";
    public const string ClearanceInProgress = "ClearanceInProgress";
    public const string FinalReviewPending = "FinalReviewPending";
    public const string Approved = "Approved";
    public const string Terminated = "Terminated";
    public const string Cancelled = "Cancelled";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Submitted,
        UnderReview,
        HandoverInProgress,
        ClearanceInProgress,
        FinalReviewPending,
        Approved,
        Terminated,
        Cancelled,
        Rejected
    ];

    public static readonly IReadOnlySet<string> OpenStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
        Submitted,
        UnderReview,
        HandoverInProgress,
        ClearanceInProgress,
        FinalReviewPending,
        Approved
    };

    public static bool IsOpenStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && OpenStatuses.Contains(status);
}

/// <summary>
/// High level separation categories.
/// </summary>
public static class StaffSeparationCategories
{
    public const string Resignation = "Resignation";
    public const string Retirement = "Retirement";
    public const string ContractExpiry = "ContractExpiry";
    public const string EmployerInitiated = "EmployerInitiated";
    public const string MutualSeparation = "MutualSeparation";
    public const string MedicalDischarge = "MedicalDischarge";
    public const string ProbationTermination = "ProbationTermination";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
    [
        Resignation,
        Retirement,
        ContractExpiry,
        EmployerInitiated,
        MutualSeparation,
        MedicalDischarge,
        ProbationTermination,
        Other
    ];
}

/// <summary>
/// Initiation sources.
/// </summary>
public static class StaffSeparationInitiationSources
{
    public const string EmployeeSelf = "EmployeeSelf";
    public const string Supervisor = "Supervisor";
    public const string HRAdmin = "HRAdmin";
    public const string SystemRule = "SystemRule";
}

/// <summary>
/// Domains of clearance.
/// </summary>
public static class StaffSeparationClearanceDomains
{
    public const string SupervisorHandover = "SupervisorHandover";
    public const string AssetReturn = "AssetReturn";
    public const string FinanceSettlement = "FinanceSettlement";
    public const string ItRevocation = "ItRevocation";
    public const string SecurityDoorAccess = "SecurityDoorAccess";
    public const string DepartmentalClearance = "DepartmentalClearance";

    public static readonly IReadOnlyList<string> All =
    [
        SupervisorHandover,
        AssetReturn,
        FinanceSettlement,
        ItRevocation,
        SecurityDoorAccess,
        DepartmentalClearance
    ];
}

/// <summary>
/// Task statuses.
/// </summary>
public static class StaffSeparationClearanceTaskStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Cleared = "Cleared";
    public const string Waived = "Waived";
    public const string Blocked = "Blocked";
}

/// <summary>
/// Item types for operational handover.
/// </summary>
public static class StaffSeparationWorkHandoverItemTypes
{
    public const string ProjectAssignment = "ProjectAssignment";
    public const string OperationalDuty = "OperationalDuty";
    public const string FileOrDocument = "FileOrDocument";
    public const string OpenWorkflowTask = "OpenWorkflowTask";
    public const string SystemAdminRole = "SystemAdminRole";
    public const string Other = "Other";
}

/// <summary>
/// Handover item states.
/// </summary>
public static class StaffSeparationWorkHandoverItemStatuses
{
    public const string Pending = "Pending";
    public const string HandedOver = "HandedOver";
    public const string Verified = "Verified";
    public const string Waived = "Waived";
}

/// <summary>
/// Asset return actions.
/// </summary>
public static class StaffSeparationAssetDisposalActions
{
    public const string PendingReturn = "PendingReturn";
    public const string ReturnedInGoodOrder = "ReturnedInGoodOrder";
    public const string ReturnedDamaged = "ReturnedDamaged";
    public const string ReportedLost = "ReportedLost";
    public const string KeptBuyout = "KeptBuyout";
    public const string Waived = "Waived";
}

/// <summary>
/// Financial breakdown lines for final settlement.
/// </summary>
public static class StaffSeparationFinanceCategories
{
    public const string SalaryArrears = "SalaryArrears";
    public const string UnusedLeaveEncashment = "UnusedLeaveEncashment";
    public const string Gratuity = "Gratuity";
    public const string Severance = "Severance";
    public const string LoanDeduction = "LoanDeduction";
    public const string AdvanceRecovery = "AdvanceRecovery";
    public const string AssetDamageDeduction = "AssetDamageDeduction";
    public const string NoticePenaltyDeduction = "NoticePenaltyDeduction";
    public const string OtherAdjustment = "OtherAdjustment";
}

/// <summary>
/// System types for IT and security account revocation.
/// </summary>
public static class StaffSeparationAccessSystemTypes
{
    public const string HrmPortal = "HrmPortal";
    public const string AdminPortal = "AdminPortal";
    public const string ActiveDirectory = "ActiveDirectory";
    public const string EmailExchange = "EmailExchange";
    public const string EFaasOrgLink = "EFaasOrgLink";
    public const string DoorAccessCard = "DoorAccessCard";
    public const string PhysicalKey = "PhysicalKey";
    public const string VpnAccess = "VpnAccess";
    public const string ThirdPartyApp = "ThirdPartyApp";
}

/// <summary>
/// Actions taken during access revocation.
/// </summary>
public static class StaffSeparationAccessRevocationActions
{
    public const string RevokeAccess = "RevokeAccess";
    public const string DisableAccount = "DisableAccount";
    public const string DeleteAccount = "DeleteAccount";
    public const string ReassignMailbox = "ReassignMailbox";
    public const string PhysicalCardCollected = "PhysicalCardCollected";
}

/// <summary>
/// Status of revocation task.
/// </summary>
public static class StaffSeparationAccessRevocationStatuses
{
    public const string Pending = "Pending";
    public const string Scheduled = "Scheduled";
    public const string Revoked = "Revoked";
    public const string CardCollected = "CardCollected";
    public const string Failed = "Failed";
    public const string Waived = "Waived";
}

/// <summary>
/// Standard document variants.
/// </summary>
public static class StaffSeparationDocumentVariants
{
    public const string ExitClearanceCertificate = "ExitClearanceCertificate";
    public const string ResignationAcceptanceLetter = "ResignationAcceptanceLetter";
    public const string ServiceCertificate = "ServiceCertificate";
    public const string HandoverCertificate = "HandoverCertificate";
    public const string FinalSettlementStatement = "FinalSettlementStatement";
}

/// <summary>
/// Types of separation events.
/// </summary>
public static class StaffSeparationEventTypes
{
    public const string Initiated = "Initiated";
    public const string Submitted = "Submitted";
    public const string HandoverUpdated = "HandoverUpdated";
    public const string AssetCleared = "AssetCleared";
    public const string FinanceUpdated = "FinanceUpdated";
    public const string TaskCleared = "TaskCleared";
    public const string TaskWaived = "TaskWaived";
    public const string AccessRevoked = "AccessRevoked";
    public const string FinalApproved = "FinalApproved";
    public const string Terminated = "Terminated";
    public const string PostSeparationUpdated = "PostSeparationUpdated";
}
