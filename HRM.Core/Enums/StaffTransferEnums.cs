namespace HRM.Enums;

/// <summary>
/// Formal workflow and operational lifecycle states for an internal staff transfer request.
/// Distinguishes business approval from effective-date activation.
/// </summary>
public static class StaffTransferStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string PendingStaffConfirmation = "PendingStaffConfirmation";
    public const string CorrectionRequested = "CorrectionRequested";
    public const string PendingReceivingApproval = "PendingReceivingApproval";
    public const string Rejected = "Rejected";
    public const string Approved = "Approved";
    public const string Scheduled = "Scheduled";
    public const string ActivationInProgress = "ActivationInProgress";
    public const string Active = "Active";
    public const string ActivationFailed = "ActivationFailed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Submitted,
        PendingStaffConfirmation,
        CorrectionRequested,
        PendingReceivingApproval,
        Rejected,
        Approved,
        Scheduled,
        ActivationInProgress,
        Active,
        ActivationFailed,
        Cancelled
    ];

    public static readonly IReadOnlySet<string> OpenStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
        Submitted,
        PendingStaffConfirmation,
        CorrectionRequested,
        PendingReceivingApproval,
        Approved,
        Scheduled,
        ActivationInProgress,
        ActivationFailed
    };

    public static bool IsOpenStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && OpenStatuses.Contains(status);
}

/// <summary>
/// Business classification of the transfer request.
/// Supports both internal departmental movements and external cross-employer/secondment transfers.
/// </summary>
public enum StaffTransferType
{
    InternalDepartmental = 1,
    CrossStructureReassignment = 2,
    SameJobPositionTransfer = 3,
    NewAppointmentTransfer = 4,
    CrossOrganisationTransfer = 5,
    ExternalOrganizationTransfer = 6,
    TemporaryAssignmentOrSecondment = 7
}

/// <summary>
/// Treatment of accumulated years of service / pension continuity when transferring.
/// </summary>
public static class ServiceContinuityTreatments
{
    public const string RecognizedContinuous = "RecognizedContinuous";
    public const string SeverancePaidReset = "SeverancePaidReset";
    public const string FrozenRetained = "FrozenRetained";

    public static readonly IReadOnlyList<string> All =
    [
        RecognizedContinuous,
        SeverancePaidReset,
        FrozenRetained
    ];
}

/// <summary>
/// Treatment of accrued annual, casual, and other leave balances upon transfer.
/// </summary>
public static class LeaveBalanceTreatments
{
    public const string TransferFull = "TransferFull";
    public const string EncashAtSource = "EncashAtSource";
    public const string ForfeitLapse = "ForfeitLapse";

    public static readonly IReadOnlyList<string> All =
    [
        TransferFull,
        EncashAtSource,
        ForfeitLapse
    ];
}

/// <summary>
/// Clearance domains for multi-stage external & internal transfer verification.
/// </summary>
public static class StaffTransferClearanceDomains
{
    public const string WorkHandover = "WorkHandover";
    public const string AssetCustody = "AssetCustody";
    public const string FinancialSettlement = "FinancialSettlement";
    public const string ItAccess = "ItAccess";
    public const string PhysicalSecurity = "PhysicalSecurity";
    public const string StatutoryFiling = "StatutoryFiling";

    public static readonly IReadOnlyList<string> All =
    [
        WorkHandover,
        AssetCustody,
        FinancialSettlement,
        ItAccess,
        PhysicalSecurity,
        StatutoryFiling
    ];
}

/// <summary>
/// Asset decisions for staff items in custody when transferring between departments/organizations.
/// </summary>
public static class StaffTransferAssetDecisions
{
    public const string ReturnToSource = "ReturnToSource";
    public const string RetainWithEmployee = "RetainWithEmployee";
    public const string TransferOwnershipToDestination = "TransferOwnershipToDestination";
    public const string SendForMaintenance = "SendForMaintenance";
    public const string ReplaceAsset = "ReplaceAsset";

    public static readonly IReadOnlyList<string> All =
    [
        ReturnToSource,
        RetainWithEmployee,
        TransferOwnershipToDestination,
        SendForMaintenance,
        ReplaceAsset
    ];
}

/// <summary>
/// Defines how the transfer affects the employee's Job and JobPosition records upon activation.
/// </summary>
public static class TransferAppointmentModes
{
    /// <summary>Employee remains in the same JobID; versioned history of department/structure reassignment is recorded.</summary>
    public const string OrganisationalReassignment = "OrganisationalReassignment";

    /// <summary>Keep JobID where permitted by existing job rules, and update effective-dated JobPositions / PF_JobStepHistory.</summary>
    public const string SameJobPositionChange = "SameJobPositionChange";

    /// <summary>Create a new Job record when the transfer is genuinely a new employment appointment under HR rules.</summary>
    public const string NewEmploymentAppointment = "NewEmploymentAppointment";

    public static readonly IReadOnlyList<string> All =
    [
        OrganisationalReassignment,
        SameJobPositionChange,
        NewEmploymentAppointment
    ];
}

/// <summary>
/// Employee actions available on the Tab 7 Staff Confirmation step.
/// </summary>
public static class StaffTransferConfirmationResponses
{
    public const string Confirmed = "Confirmed";
    public const string CorrectionRequested = "CorrectionRequested";
    public const string ClarificationSubmitted = "ClarificationSubmitted";

    public static readonly IReadOnlyList<string> All =
    [
        Confirmed,
        CorrectionRequested,
        ClarificationSubmitted
    ];
}

/// <summary>
/// Participant roles for transfer commentary, confirmation, approval, and provisioning.
/// </summary>
public static class StaffTransferParticipantRoles
{
    public const string SendingOfficer = "SendingOfficer";
    public const string CurrentSupervisor = "CurrentSupervisor";
    public const string CurrentHOD = "CurrentHOD";
    public const string Employee = "Employee";
    public const string ReceivingApprover = "ReceivingApprover";
    public const string HR = "HR";
    public const string SecurityAdmin = "SecurityAdmin";

    public static readonly IReadOnlyList<string> All =
    [
        SendingOfficer,
        CurrentSupervisor,
        CurrentHOD,
        Employee,
        ReceivingApprover,
        HR,
        SecurityAdmin
    ];
}

/// <summary>
/// Comment classifications recorded in ST_StaffTransferComments.
/// </summary>
public static class StaffTransferCommentKinds
{
    public const string AdvisoryComment = "AdvisoryComment";
    public const string Clarification = "Clarification";
    public const string CorrectionRequest = "CorrectionRequest";
    public const string ApprovalRemark = "ApprovalRemark";
    public const string SystemAudit = "SystemAudit";

    public static readonly IReadOnlyList<string> All =
    [
        AdvisoryComment,
        Clarification,
        CorrectionRequest,
        ApprovalRemark,
        SystemAudit
    ];
}

/// <summary>
/// Application and organizational access areas reviewed in Tab 4.
/// </summary>
public static class StaffTransferAccessAreas
{
    public const string OrganisationDataAccess = "OrganisationDataAccess";
    public const string DepartmentInformation = "DepartmentInformation";
    public const string EmployeeRecordVisibility = "EmployeeRecordVisibility";
    public const string HrApprovalPermissions = "HrApprovalPermissions";
    public const string AttendanceAdministration = "AttendanceAdministration";
    public const string LeaveManagement = "LeaveManagement";
    public const string ReportsAndAnalytics = "ReportsAndAnalytics";
    public const string RequestProcessing = "RequestProcessing";
    public const string SupervisorHodPrivileges = "SupervisorHodPrivileges";
    public const string SensitiveStaffInformation = "SensitiveStaffInformation";
}

/// <summary>
/// Permission transition actions for organizational/application access changes.
/// </summary>
public static class StaffTransferAccessActions
{
    public const string Retain = "Retain";
    public const string Revoke = "Revoke";
    public const string Grant = "Grant";
    public const string RevokeOldGrantNew = "RevokeOldGrantNew";
    public const string ReviewAndAssign = "ReviewAndAssign";
    public const string NoChange = "NoChange";

    public static readonly IReadOnlyList<string> All =
    [
        Retain,
        Revoke,
        Grant,
        RevokeOldGrantNew,
        ReviewAndAssign,
        NoChange
    ];
}

/// <summary>
/// Physical door-access transition actions and provisioning states (Tab 5).
/// </summary>
public static class StaffTransferDoorAccessStates
{
    public const string ActionRetainCommon = "RetainCommon";
    public const string ActionRevokeOld = "RevokeOld";
    public const string ActionGrantNew = "GrantNew";
    public const string ActionReplace = "Replace";

    public const string StatePending = "Pending";
    public const string StateTaskGenerated = "TaskGenerated";
    public const string StateInProgress = "InProgress";
    public const string StateProvisioned = "Provisioned";
    public const string StateRevoked = "Revoked";
    public const string StateVerified = "Verified";
    public const string StateFailed = "Failed";
}

/// <summary>
/// Routing policies for pending requests and supervisory approvals when a staff member transfers.
/// </summary>
public static class StaffTransferApprovalRoutingRules
{
    public const string RetainHistoricalReassignOpen = "RetainHistoricalReassignOpen";
    public const string ReassignAllToSuccessor = "ReassignAllToSuccessor";
    public const string EscalateToSendingHod = "EscalateToSendingHod";
    public const string ManualReview = "ManualReview";

    public static readonly IReadOnlyList<string> All =
    [
        RetainHistoricalReassignOpen,
        ReassignAllToSuccessor,
        EscalateToSendingHod,
        ManualReview
    ];
}

/// <summary>
/// Idempotent execution step codes recorded by StaffTransferActivationService.
/// </summary>
public static class StaffTransferActivationSteps
{
    public const string Validation = "Validation";
    public const string JobAndStructureReassignment = "JobAndStructureReassignment";
    public const string PositionAndStepHistory = "PositionAndStepHistory";
    public const string WorkPlanningReconciliation = "WorkPlanningReconciliation";
    public const string PendingApprovalsRouting = "PendingApprovalsRouting";
    public const string OrganisationalAccessProvisioning = "OrganisationalAccessProvisioning";
    public const string DoorAccessTaskDispatch = "DoorAccessTaskDispatch";
    public const string Completed = "Completed";
}

/// <summary>
/// Printable A4 PDF document variants for approved transfers.
/// </summary>
public static class StaffTransferDocumentVariants
{
    public const string EmployeeNotice = "EmployeeNotice";
    public const string RestrictedAdminRecord = "RestrictedAdminRecord";
}
