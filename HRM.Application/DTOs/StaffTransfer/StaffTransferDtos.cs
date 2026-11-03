using HRM.Enums;

namespace HRM.DTOs.StaffTransfer;

/// <summary>
/// Dashboard summary counters and lists for the Staff Transfer Management overview.
/// </summary>
public sealed class StaffTransferDashboardData
{
    public int OrganisationId { get; set; }
    public string OrganisationName { get; set; } = "";
    public bool CanInitiate { get; set; }
    public bool CanApproveReceiving { get; set; }
    public bool CanActivate { get; set; }
    public bool CanProvisionSecurity { get; set; }

    public int DraftCount { get; set; }
    public int PendingConfirmationCount { get; set; }
    public int PendingApprovalCount { get; set; }
    public int ApprovedScheduledCount { get; set; }
    public int ActiveCount { get; set; }
    public int FailedActivationCount { get; set; }
    public int OverdueConfirmationCount { get; set; }
    public int PendingDoorProvisioningCount { get; set; }

    public List<StaffTransferRowDto> Items { get; set; } = [];
}

/// <summary>
/// Summary row for Transfer List, Transfer History, and Transfer Monitoring views.
/// </summary>
public sealed record StaffTransferRowDto(
    int StaffTransferId,
    int? RequestId,
    string? ReferenceNumber,
    int IndividualId,
    string EmployeeNumber,
    string FullName,
    int CurrentJobId,
    string CurrentPositionTitle,
    int FromOrganisationId,
    string FromOrganisationName,
    int FromOrganisationStructureId,
    string FromDepartmentName,
    int ToOrganisationId,
    string ToOrganisationName,
    int ToOrganisationStructureId,
    string ToDepartmentName,
    string? DestinationPositionTitle,
    int TransferTypeId,
    string TransferTypeName,
    string TransferAppointmentMode,
    DateTime RequestedDate,
    DateTime EffectiveDate,
    DateTime ReportingDate,
    string Status,
    int CurrentVersion,
    int? ConfirmedVersion,
    bool HasSupervisorComment,
    bool HasHodComment,
    string DoorProvisioningSummary,
    DateTime? FinalApprovedAt,
    DateTime? ActivatedAt);

/// <summary>
/// Tab 1 — Staff information snapshot retrieved from HRM database.
/// </summary>
public sealed class StaffTransferStaffTabDto
{
    public int IndividualId { get; set; }
    public string FullName { get; set; } = "";
    public string EmployeeNumber { get; set; } = "";
    public string? IdCardNumber { get; set; }
    public bool HasPhoto { get; set; }
    public string? PhotoContentType { get; set; }
    public int CurrentOrganisationId { get; set; }
    public string CurrentOrganisationName { get; set; } = "";
    public int CurrentOrganisationStructureId { get; set; }
    public string CurrentDepartmentName { get; set; } = "";
    public string CurrentEmploymentStatus { get; set; } = "Active";
    public DateTime DateJoined { get; set; }
    public string? OfficialEmail { get; set; }
    public string? OfficialPhone { get; set; }
    public int? CurrentSupervisorIndividualId { get; set; }
    public string? CurrentSupervisorName { get; set; }
    public int? CurrentHodIndividualId { get; set; }
    public string? CurrentHodName { get; set; }
    public string EmploymentCategory { get; set; } = "";
    public int CurrentJobId { get; set; }
    public int? CurrentJobPositionId { get; set; }
    public int? CurrentPositionId { get; set; }
    public string CurrentPositionTitle { get; set; } = "";
}

/// <summary>
/// Tab 2 — Job, position, PF_PositionMatrix, grade/step, and appointment mode details.
/// </summary>
public sealed class StaffTransferJobTabDto
{
    public int ExistingJobId { get; set; }
    public int? ExistingJobPositionId { get; set; }
    public int? ExistingPositionId { get; set; }
    public string ExistingPositionTitle { get; set; } = "";
    public int? ExistingPositionFrameworkId { get; set; }
    public string? ExistingPositionFrameworkName { get; set; }
    public int? ExistingPositionMatrixId { get; set; }
    public string? ExistingPositionMatrixLabel { get; set; }
    public int? ExistingJobMatrixId { get; set; }
    public int? ExistingJobMatrixStepId { get; set; }
    public string? ExistingGradeLevel { get; set; }
    public decimal? ExistingBasicSalary { get; set; }
    public decimal? ExistingAllowance { get; set; }

    public int DestinationOrganisationId { get; set; }
    public int DestinationOrganisationStructureId { get; set; }
    public int? DestinationJobTypeId { get; set; }
    public int? DestinationPositionId { get; set; }
    public string? DestinationPositionTitle { get; set; }
    public int? DestinationPositionFrameworkId { get; set; }
    public int? DestinationPositionMatrixId { get; set; }
    public int? DestinationJobMatrixId { get; set; }
    public int? DestinationJobMatrixStepId { get; set; }
    public string? DestinationGradeLevel { get; set; }

    public int TransferTypeId { get; set; } = (int)StaffTransferType.InternalDepartmental;
    public string TransferAppointmentMode { get; set; } = TransferAppointmentModes.OrganisationalReassignment;
    public bool HasSalaryBenefitChange { get; set; }
    public decimal? ProposedBasicSalary { get; set; }
    public decimal? ProposedAllowance { get; set; }
    public string? SalaryBenefitChangeNotes { get; set; }
    public int? NewReportingSupervisorIndividualId { get; set; }
    public string? NewReportingSupervisorName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string TransferReason { get; set; } = "";
}

/// <summary>
/// Tab 3 — Work schedule, attendance planning provider, template, location, and duty arrangements.
/// </summary>
public sealed class StaffTransferWorkTabDto
{
    public int? CurrentLocationId { get; set; }
    public string? CurrentLocationName { get; set; }
    public int? ProposedLocationId { get; set; }
    public string ProposedLocationName { get; set; } = "";
    public int? CurrentPlanningProviderId { get; set; }
    public string? CurrentPlanningProviderName { get; set; }
    public int? ProposedPlanningProviderId { get; set; }
    public string? ProposedPlanningProviderName { get; set; }
    public int? CurrentWorkTemplateId { get; set; }
    public string? CurrentWorkTemplateName { get; set; }
    public int? ProposedWorkTemplateId { get; set; }
    public string? ProposedWorkTemplateName { get; set; }
    public string? CurrentScheduleMode { get; set; }
    public string ProposedScheduleMode { get; set; } = "Standard";
    public int WorkingDaysMask { get; set; } = 31;
    public TimeOnly? StandardStartTime { get; set; }
    public TimeOnly? StandardEndTime { get; set; }
    public decimal WeeklyWorkingHours { get; set; } = 35m;
    public string RequiredAttendanceMethod { get; set; } = "Biometric";
    public int? AttendanceDeviceId { get; set; }
    public string? AttendanceDeviceName { get; set; }
    public string? AttendanceDeviceLocationNotes { get; set; }
    public bool IsOvertimeEligible { get; set; }
    public string WorkArrangementMode { get; set; } = "OnSite";
    public int? AttendanceVerificationSupervisorIndividualId { get; set; }
    public string? AttendanceVerificationSupervisorName { get; set; }
    public DateTime ScheduleEffectiveDate { get; set; }
    public bool ReconcileFutureWorkPlans { get; set; } = true;
    public string? WorkNotes { get; set; }
}

/// <summary>
/// Tab 4 — Application/organizational permission comparison item.
/// </summary>
public sealed class StaffTransferAccessChangeItemDto
{
    public int StaffTransferAccessChangeId { get; set; }
    public string AccessAreaCode { get; set; } = "";
    public string AccessAreaName { get; set; } = "";
    public string? CurrentScopeSummary { get; set; }
    public string? ProposedScopeSummary { get; set; }
    public string TransitionAction { get; set; } = StaffTransferAccessActions.ReviewAndAssign;
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? UserGroupId { get; set; }
    public string? UserGroupName { get; set; }
    public bool RemainsActiveInOldDepartmentWarning { get; set; }
    public bool RequiresSeparateSecurityAuth { get; set; }
    public string ProvisioningStatus { get; set; } = "Pending";
    public DateTime? AppliedAtUtc { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// Tab 5 — Physical door-access and security provisioning item.
/// </summary>
public sealed class StaffTransferDoorAccessItemDto
{
    public int StaffTransferDoorAccessId { get; set; }
    public string BuildingName { get; set; } = "";
    public string? FloorName { get; set; }
    public string? DepartmentOfficeArea { get; set; }
    public string? RestrictedRoomName { get; set; }
    public bool IncludesServerRoom { get; set; }
    public string? StaffEntranceName { get; set; }
    public int? AttendanceDeviceId { get; set; }
    public string? AttendanceDeviceName { get; set; }
    public string? AccessCardReference { get; set; }
    public string? ExistingAccessGroups { get; set; }
    public string? ProposedAccessGroups { get; set; }
    public string TransitionAction { get; set; } = StaffTransferDoorAccessStates.ActionReplace;
    public DateTime AccessStartDate { get; set; }
    public DateTime? PreviousAccessExpiryDate { get; set; }
    public int? SecurityOfficerUserId { get; set; }
    public string? SecurityOfficerName { get; set; }
    public string? ConnectorReference { get; set; }
    public string ProvisioningState { get; set; } = StaffTransferDoorAccessStates.StatePending;
    public DateTime? ProvisionedAtUtc { get; set; }
    public string? ProvisioningNotes { get; set; }
}

/// <summary>
/// Tab 6 — Reporting and handover arrangements.
/// </summary>
public sealed class StaffTransferReportingTabDto
{
    public DateTime ApprovedTransferEffectiveDate { get; set; }
    public DateTime ReportingDate { get; set; }
    public TimeOnly? ReportingTime { get; set; }
    public string ReportingLocation { get; set; } = "";
    public int? ReportingToIndividualId { get; set; }
    public string? ReportingToName { get; set; }
    public bool HandoverRequired { get; set; }
    public DateTime? HandoverDeadline { get; set; }
    public string? OutstandingAssignmentsSummary { get; set; }
    public string? AssetsToReturnSummary { get; set; }
    public string? AssetsToReceiveSummary { get; set; }
    public string OutstandingApprovalsRoutingRule { get; set; } = StaffTransferApprovalRoutingRules.RetainHistoricalReassignOpen;
    public string? OutstandingApprovalsSummary { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<StaffTransferResourceLinkDto> WorkHandoverLinks { get; set; } = [];
    public List<StaffTransferResourceLinkDto> AssetReturnRequestLinks { get; set; } = [];
    public List<StaffTransferResourceLinkDto> AssetReceiveRequestLinks { get; set; } = [];
}

public sealed record StaffTransferResourceLinkDto(
    int RequestId,
    string Reference,
    string Title,
    string Status,
    string Url);

/// <summary>
/// Advisory comment, clarification, or decision note.
/// </summary>
public sealed record StaffTransferCommentDto(
    int StaffTransferCommentId,
    int AuthorUserId,
    string AuthorName,
    string ParticipantRole,
    string CommentKind,
    int RequestVersion,
    string CommentText,
    bool IsVisibleToEmployee,
    DateTime CreatedAtUtc);

/// <summary>
/// Tab 7 — Employee confirmation history entry.
/// </summary>
public sealed record StaffTransferConfirmationDto(
    int StaffTransferConfirmationId,
    int ConfirmedVersion,
    string ResponseKind,
    int ConfirmedByUserId,
    string ConfirmedByName,
    string ConfirmationStatement,
    string? EmployeeRemarks,
    bool IsInvalidatedByMaterialEdit,
    DateTime? InvalidatedAtUtc,
    DateTime ConfirmedAtUtc);

/// <summary>
/// Complete multi-tab transfer model used for creating, editing, confirming, and approving a transfer request.
/// </summary>
public sealed class StaffTransferDetailDto
{
    public int StaffTransferId { get; set; }
    public int? RequestId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Status { get; set; } = StaffTransferStatuses.Draft;
    public bool IsOpen { get; set; } = true;
    public int CurrentVersion { get; set; } = 1;
    public int? ConfirmedVersion { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = "";
    public DateTime RequestedDate { get; set; }
    public int? FinalApprovedByUserId { get; set; }
    public string? FinalApprovedByName { get; set; }
    public DateTime? FinalApprovedAt { get; set; }
    public string? FinalDecisionRemarks { get; set; }
    public DateTime? ActivatedAt { get; set; }

    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanCommentAsSupervisorOrHod { get; set; }
    public bool CanConfirmAsEmployee { get; set; }
    public bool CanApproveAsReceiving { get; set; }
    public bool CanActivate { get; set; }
    public bool CanExportRestrictedPdf { get; set; }
    public string PortalAccessContext { get; set; } = "Online";

    public StaffTransferStaffTabDto Staff { get; set; } = new();
    public StaffTransferJobTabDto Job { get; set; } = new();
    public StaffTransferWorkTabDto Work { get; set; } = new();
    public List<StaffTransferAccessChangeItemDto> AccessChanges { get; set; } = [];
    public List<StaffTransferDoorAccessItemDto> DoorAccessChanges { get; set; } = [];
    public StaffTransferReportingTabDto Reporting { get; set; } = new();
    public List<StaffTransferCommentDto> Comments { get; set; } = [];
    public List<StaffTransferConfirmationDto> Confirmations { get; set; } = [];
    public List<StaffTransferActivationLogDto> ActivationLogs { get; set; } = [];
    public List<StaffTransferDocumentSummaryDto> Documents { get; set; } = [];
}

public sealed record StaffTransferActivationLogDto(
    long StaffTransferActivationLogId,
    int ExecutionAttempt,
    string StepCode,
    string StepStatus,
    string IdempotencyKey,
    string? DetailsJson,
    string? ErrorMessage,
    DateTime ExecutedAtUtc);

public sealed record StaffTransferDocumentSummaryDto(
    long StaffTransferDocumentId,
    string DocumentNumber,
    string DocumentVariant,
    int RequestVersion,
    int? ConfirmedVersion,
    string Sha256Hash,
    DateTime GeneratedAtUtc);

/// <summary>
/// Input payload for saving a draft or submitting a transfer request across Tabs 1–6.
/// </summary>
public sealed class StaffTransferSaveInput
{
    public int StaffTransferId { get; set; }
    public int IndividualId { get; set; }
    public int CurrentJobId { get; set; }
    public bool SubmitImmediately { get; set; }
    public int ExpectedVersion { get; set; } = 1;
    public string PortalAccessContext { get; set; } = "Online";

    public StaffTransferJobTabDto Job { get; set; } = new();
    public StaffTransferWorkTabDto Work { get; set; } = new();
    public List<StaffTransferAccessChangeItemDto> AccessChanges { get; set; } = [];
    public List<StaffTransferDoorAccessItemDto> DoorAccessChanges { get; set; } = [];
    public StaffTransferReportingTabDto Reporting { get; set; } = new();
}

/// <summary>
/// Employee confirmation or correction request payload (Tab 7).
/// </summary>
public sealed class StaffTransferEmployeeConfirmationInput
{
    public int StaffTransferId { get; set; }
    public int ReviewedVersion { get; set; }
    public string ResponseKind { get; set; } = StaffTransferConfirmationResponses.Confirmed;
    public bool AcknowledgedStatement { get; set; }
    public string? EmployeeRemarks { get; set; }
}

/// <summary>
/// Receiving department final decision payload (Approve, Reject, ReturnForCorrection).
/// </summary>
public sealed class StaffTransferDecisionInput
{
    public int StaffTransferId { get; set; }
    public int ExpectedVersion { get; set; }
    public string Decision { get; set; } = "Approve";
    public string? Remarks { get; set; }
}

/// <summary>
/// Result returned by IStaffTransferActivationService.ActivateAsync.
/// </summary>
public sealed class TransferActivationResult
{
    public int StaffTransferId { get; set; }
    public bool Succeeded { get; set; }
    public bool WasAlreadyActive { get; set; }
    public string ResultingStatus { get; set; } = "";
    public int? ResultingJobId { get; set; }
    public int? ResultingJobPositionId { get; set; }
    public int ReconciledWorkPlanCount { get; set; }
    public int DispatchedDoorAccessTaskCount { get; set; }
    public string Message { get; set; } = "";
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Lookup options for the New/Edit Staff Transfer multi-tab form.
/// </summary>
public sealed class StaffTransferFormLookupDto
{
    public int CurrentOrganisationId { get; set; }
    public string CurrentOrganisationName { get; set; } = "";
    public List<StaffTransferLookupOption> Employees { get; set; } = [];
    public List<StaffTransferEmployeeOption> EmployeeItems { get; set; } = [];
    public List<StaffTransferLookupOption> StructureTypes { get; set; } = [];
    public List<StaffTransferLookupOption> Organisations { get; set; } = [];
    public List<StaffTransferDepartmentOption> Departments { get; set; } = [];
    public List<StaffTransferSupervisorOption> DepartmentSupervisors { get; set; } = [];
    public List<StaffTransferStructureAccessOption> StructureAccessScopes { get; set; } = [];
    public List<StaffTransferLookupOption> DoorAccessGroups { get; set; } = [];
    public List<StaffTransferLookupOption> JobTypes { get; set; } = [];
    public List<StaffTransferPositionOption> Positions { get; set; } = [];
    public List<StaffTransferLookupOption> PositionFrameworks { get; set; } = [];
    public List<StaffTransferMatrixOption> PositionMatrices { get; set; } = [];
    public List<StaffTransferStepOption> MatrixSteps { get; set; } = [];
    public List<StaffTransferLookupOption> Locations { get; set; } = [];
    public List<StaffTransferLookupOption> PlanningProviders { get; set; } = [];
    public List<StaffTransferLookupOption> WorkTemplates { get; set; } = [];
    public List<StaffTransferLookupOption> AttendanceDevices { get; set; } = [];
    public List<StaffTransferLookupOption> Supervisors { get; set; } = [];
    public List<StaffTransferLookupOption> Roles { get; set; } = [];
    public List<StaffTransferLookupOption> UserGroups { get; set; } = [];
    public List<StaffTransferLookupOption> SecurityOfficers { get; set; } = [];
}

public sealed record StaffTransferLookupOption(int Id, string Name, string? Subtitle = null);

public sealed record StaffTransferEmployeeOption(
    int IndividualId,
    int JobId,
    int OrganisationId,
    int OrganisationStructureId,
    int StructureTypeId,
    string Name,
    string? Subtitle = null);

public sealed record StaffTransferSupervisorOption(
    int IndividualId,
    int OrganisationId,
    int OrganisationStructureId,
    string Name,
    string? RoleLabel = null);

public sealed record StaffTransferStructureAccessOption(
    int OrganisationStructureId,
    string StructureName,
    string ScopeLabel);

public sealed record StaffTransferDepartmentOption(int Id, int OrganisationId, string Name, int StructureTypeId);

public sealed record StaffTransferPositionOption(
    int PositionId,
    int? OrganisationId,
    string Name,
    int? FrameworkId,
    int? PositionMatrixId,
    bool IsVacant,
    decimal? BasicSalary,
    decimal? Allowance);

public sealed record StaffTransferMatrixOption(
    int PositionMatrixId,
    int? FrameworkId,
    int JobMatrixId,
    string Label,
    string? GradeCode,
    int ApprovedSlots,
    int AvailableSlots);

public sealed record StaffTransferStepOption(
    int JobMatrixStepId,
    int JobMatrixId,
    int StepNumber,
    decimal BasicSalary,
    decimal JobAllowance);

/// <summary>
/// Section 7 — Formal printable A4 transfer document and PDF export model.
/// Reconstructed strictly from the approved/confirmed versioned snapshot.
/// </summary>
public sealed class StaffTransferPrintableDocumentDto
{
    public long StaffTransferDocumentId { get; set; }
    public int StaffTransferId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentVariant { get; set; } = StaffTransferDocumentVariants.EmployeeNotice;
    public string? RequestReferenceNumber { get; set; }
    public DateTime IssueDateUtc { get; set; }
    public int RequestVersion { get; set; }
    public int? ConfirmedVersion { get; set; }
    public string Sha256Hash { get; set; } = "";
    public string Status { get; set; } = "";

    public string OrganisationBrandingName { get; set; } = "";
    public StaffTransferStaffTabDto Staff { get; set; } = new();
    public StaffTransferJobTabDto Job { get; set; } = new();
    public StaffTransferWorkTabDto Work { get; set; } = new();
    public List<StaffTransferAccessChangeItemDto> AccessChanges { get; set; } = [];
    public List<StaffTransferDoorAccessItemDto> DoorAccessChanges { get; set; } = [];
    public StaffTransferReportingTabDto Reporting { get; set; } = new();

    public string? ConfirmationStatement { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public string? EmployeeRemarks { get; set; }

    public List<StaffTransferCommentDto> SupervisorComments { get; set; } = [];
    public List<StaffTransferCommentDto> HodComments { get; set; } = [];

    public string? FinalApprovedByName { get; set; }
    public DateTime? FinalApprovedAtUtc { get; set; }
    public string? FinalDecisionRemarks { get; set; }
}
