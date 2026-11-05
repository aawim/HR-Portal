using HRM.Enums;
using HRM.Models.Archives;
using HRM.Models.WorkPlanning;

namespace HRM.Models;

/// <summary>
/// Transfer header, employee snapshot, source/destination placement, reason, dates, version tracking, and lifecycle status.
/// Mapped to dbo.ST_StaffTransfers.
/// </summary>
public sealed class StaffTransfer
{
    public int StaffTransferId { get; set; }
    public int? RequestId { get; set; }
    public int IndividualId { get; set; }
    public int CurrentJobId { get; set; }
    public int FromOrganisationId { get; set; }
    public int FromOrganisationStructureId { get; set; }
    public int ToOrganisationId { get; set; }
    public int ToOrganisationStructureId { get; set; }
    public int TransferTypeId { get; set; } = (int)StaffTransferType.InternalDepartmental;
    public bool IsExternalTransfer { get; set; }
    public bool IsLegalEmployerChanged { get; set; }
    public int? SourceEmployerId { get; set; }
    public int? DestinationEmployerId { get; set; }
    public DateTime? SecondmentEndDate { get; set; }
    public string ServiceContinuityTreatment { get; set; } = ServiceContinuityTreatments.RecognizedContinuous;
    public string LeaveBalanceTreatment { get; set; } = LeaveBalanceTreatments.TransferFull;
    public int? SourceHrReleaseUserId { get; set; }
    public DateTime? SourceHrReleasedAt { get; set; }
    public string? AppointingAuthorityRef { get; set; }

    public string TransferReason { get; set; } = "";
    public int RequestedByUserId { get; set; }
    public DateTime RequestedDate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReportingDate { get; set; }
    public string Status { get; set; } = StaffTransferStatuses.Draft;
    public bool IsOpen { get; set; } = true;
    public int CurrentVersion { get; set; } = 1;
    public int? ConfirmedVersion { get; set; }
    public string? MaterialStateFingerprint { get; set; }

    // Tab 1 — Immutable staff profile snapshot captured at submission/version update
    public string FullNameSnapshot { get; set; } = "";
    public string EmployeeNumberSnapshot { get; set; } = "";
    public string? ProtectedIdCardSnapshot { get; set; }
    public string CurrentEmploymentStatusSnapshot { get; set; } = "";
    public string EmploymentCategorySnapshot { get; set; } = "";
    public DateTime DateJoinedSnapshot { get; set; }
    public string? OfficialEmailSnapshot { get; set; }
    public string? OfficialPhoneSnapshot { get; set; }
    public int? CurrentSupervisorIndividualId { get; set; }
    public int? CurrentHodIndividualId { get; set; }
    public string? StaffSnapshotJson { get; set; }

    // Decision and activation metadata
    public int? FinalApprovedByUserId { get; set; }
    public DateTime? FinalApprovedAt { get; set; }
    public string? FinalDecisionRemarks { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public int? OperationLogId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public Request? Request { get; set; }
    public Individual Individual { get; set; } = null!;
    public Job CurrentJob { get; set; } = null!;
    public Organisation FromOrganisation { get; set; } = null!;
    public OrganisationStructure FromOrganisationStructure { get; set; } = null!;
    public Organisation ToOrganisation { get; set; } = null!;
    public OrganisationStructure ToOrganisationStructure { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? FinalApprovedByUser { get; set; }
    public Individual? CurrentSupervisorIndividual { get; set; }
    public Individual? CurrentHodIndividual { get; set; }
    public OperationLog? OperationLog { get; set; }

    public Organisation? SourceEmployer { get; set; }
    public Organisation? DestinationEmployer { get; set; }
    public User? SourceHrReleaseUser { get; set; }

    public StaffTransferJobDetail? JobDetail { get; set; }
    public StaffTransferWorkDetail? WorkDetail { get; set; }
    public StaffTransferReportingDetail? ReportingDetail { get; set; }
    public StaffTransferFinanceSettlement? FinanceSettlement { get; set; }
    public ICollection<StaffTransferClearanceTask> ClearanceTasks { get; set; } = new List<StaffTransferClearanceTask>();
    public ICollection<StaffTransferAssetDecision> AssetDecisions { get; set; } = new List<StaffTransferAssetDecision>();
    public ICollection<StaffTransferEmploymentExecution> EmploymentExecutions { get; set; } = new List<StaffTransferEmploymentExecution>();
    public ICollection<StaffTransferAccessChange> AccessChanges { get; set; } = new List<StaffTransferAccessChange>();
    public ICollection<StaffTransferDoorAccess> DoorAccessChanges { get; set; } = new List<StaffTransferDoorAccess>();
    public ICollection<StaffTransferComment> Comments { get; set; } = new List<StaffTransferComment>();
    public ICollection<StaffTransferConfirmation> Confirmations { get; set; } = new List<StaffTransferConfirmation>();
    public ICollection<StaffTransferActivationLog> ActivationLogs { get; set; } = new List<StaffTransferActivationLog>();
    public ICollection<StaffTransferDocument> Documents { get; set; } = new List<StaffTransferDocument>();
}

/// <summary>
/// Tab 2 — Current and proposed job, position, PF_PositionMatrix, grade, level, step, and appointment mode details.
/// Mapped to dbo.ST_StaffTransferJobDetails.
/// </summary>
public sealed class StaffTransferJobDetail
{
    public int StaffTransferJobDetailId { get; set; }
    public int StaffTransferId { get; set; }
    public int ExistingJobId { get; set; }
    public int? ExistingJobPositionId { get; set; }
    public int? ExistingPositionId { get; set; }
    public string ExistingPositionTitleSnapshot { get; set; } = "";
    public int? ExistingPositionFrameworkId { get; set; }
    public int? ExistingPositionMatrixId { get; set; }
    public int? ExistingJobMatrixId { get; set; }
    public int? ExistingJobMatrixStepId { get; set; }
    public string? ExistingGradeLevelSnapshot { get; set; }
    public decimal? ExistingBasicSalarySnapshot { get; set; }
    public decimal? ExistingAllowanceSnapshot { get; set; }

    public int DestinationOrganisationId { get; set; }
    public int DestinationOrganisationStructureId { get; set; }
    public int? DestinationJobTypeId { get; set; }
    public int? DestinationPositionId { get; set; }
    public string? DestinationPositionTitleSnapshot { get; set; }
    public int? DestinationPositionFrameworkId { get; set; }
    public int? DestinationPositionMatrixId { get; set; }
    public int? DestinationJobMatrixId { get; set; }
    public int? DestinationJobMatrixStepId { get; set; }
    public string? DestinationGradeLevelSnapshot { get; set; }

    public string TransferAppointmentMode { get; set; } = TransferAppointmentModes.OrganisationalReassignment;
    public bool HasSalaryBenefitChange { get; set; }
    public decimal? ProposedBasicSalary { get; set; }
    public decimal? ProposedAllowance { get; set; }
    public string? SalaryBenefitChangeNotes { get; set; }
    public int? NewReportingSupervisorIndividualId { get; set; }

    public int? ResultingJobId { get; set; }
    public int? ResultingJobPositionId { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public Job ExistingJob { get; set; } = null!;
    public JobPosition? ExistingJobPosition { get; set; }
    public Position? ExistingPosition { get; set; }
    public Organisation DestinationOrganisation { get; set; } = null!;
    public OrganisationStructure DestinationOrganisationStructure { get; set; } = null!;
    public JobType? DestinationJobType { get; set; }
    public Position? DestinationPosition { get; set; }
    public Individual? NewReportingSupervisorIndividual { get; set; }
    public Job? ResultingJob { get; set; }
    public JobPosition? ResultingJobPosition { get; set; }
}

/// <summary>
/// Tab 3 — Current and proposed work location, schedule, work template, attendance planning provider, and duty arrangements.
/// Mapped to dbo.ST_StaffTransferWorkDetails.
/// </summary>
public sealed class StaffTransferWorkDetail
{
    public int StaffTransferWorkDetailId { get; set; }
    public int StaffTransferId { get; set; }
    public int? CurrentLocationId { get; set; }
    public string? CurrentLocationNameSnapshot { get; set; }
    public int? ProposedLocationId { get; set; }
    public string ProposedLocationName { get; set; } = "";
    public int? CurrentPlanningProviderId { get; set; }
    public int? ProposedPlanningProviderId { get; set; }
    public int? CurrentWorkTemplateId { get; set; }
    public int? ProposedWorkTemplateId { get; set; }
    public string? CurrentScheduleMode { get; set; }
    public string ProposedScheduleMode { get; set; } = "Standard";
    public int WorkingDaysMask { get; set; } = 31;
    public TimeOnly? StandardStartTime { get; set; }
    public TimeOnly? StandardEndTime { get; set; }
    public decimal WeeklyWorkingHours { get; set; } = 35m;
    public string RequiredAttendanceMethod { get; set; } = "Biometric";
    public int? AttendanceDeviceId { get; set; }
    public string? AttendanceDeviceLocationNotes { get; set; }
    public bool IsOvertimeEligible { get; set; }
    public string WorkArrangementMode { get; set; } = "OnSite";
    public int? AttendanceVerificationSupervisorIndividualId { get; set; }
    public DateTime ScheduleEffectiveDate { get; set; }
    public bool ReconcileFutureWorkPlans { get; set; } = true;
    public string? WorkNotes { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public Location? CurrentLocation { get; set; }
    public Location? ProposedLocation { get; set; }
    public PlanningProvider? CurrentPlanningProvider { get; set; }
    public PlanningProvider? ProposedPlanningProvider { get; set; }
    public WorkTemplate? CurrentWorkTemplate { get; set; }
    public WorkTemplate? ProposedWorkTemplate { get; set; }
    public AttendanceDevice? AttendanceDevice { get; set; }
    public Individual? AttendanceVerificationSupervisorIndividual { get; set; }
}

/// <summary>
/// Tab 4 — Application and organizational permission differences (Current vs Proposed Access comparison).
/// Mapped to dbo.ST_StaffTransferAccessChanges.
/// </summary>
public sealed class StaffTransferAccessChange
{
    public int StaffTransferAccessChangeId { get; set; }
    public int StaffTransferId { get; set; }
    public string AccessAreaCode { get; set; } = "";
    public string AccessAreaName { get; set; } = "";
    public string? CurrentScopeSummary { get; set; }
    public string? ProposedScopeSummary { get; set; }
    public string TransitionAction { get; set; } = StaffTransferAccessActions.ReviewAndAssign;
    public int? RoleId { get; set; }
    public int? UserGroupId { get; set; }
    public bool RemainsActiveInOldDepartmentWarning { get; set; }
    public bool RequiresSeparateSecurityAuth { get; set; }
    public string ProvisioningStatus { get; set; } = "Pending";
    public DateTime? AppliedAtUtc { get; set; }
    public int? AppliedByUserId { get; set; }
    public string? Remarks { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public Role? Role { get; set; }
    public UserGroup? UserGroup { get; set; }
    public User? AppliedByUser { get; set; }
}

/// <summary>
/// Tab 5 — Physical door access changes, equipment references, and independent security provisioning outcomes.
/// Mapped to dbo.ST_StaffTransferDoorAccess.
/// </summary>
public sealed class StaffTransferDoorAccess
{
    public int StaffTransferDoorAccessId { get; set; }
    public int StaffTransferId { get; set; }
    public string BuildingName { get; set; } = "";
    public string? FloorName { get; set; }
    public string? DepartmentOfficeArea { get; set; }
    public string? RestrictedRoomName { get; set; }
    public bool IncludesServerRoom { get; set; }
    public string? StaffEntranceName { get; set; }
    public int? AttendanceDeviceId { get; set; }
    public string? AccessCardReference { get; set; }
    public string? ExistingAccessGroups { get; set; }
    public string? ProposedAccessGroups { get; set; }
    public string TransitionAction { get; set; } = StaffTransferDoorAccessStates.ActionReplace;
    public DateTime AccessStartDate { get; set; }
    public DateTime? PreviousAccessExpiryDate { get; set; }
    public int? SecurityOfficerUserId { get; set; }
    public string? ConnectorReference { get; set; }
    public string ProvisioningState { get; set; } = StaffTransferDoorAccessStates.StatePending;
    public DateTime? ProvisionedAtUtc { get; set; }
    public string? ProvisioningNotes { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public AttendanceDevice? AttendanceDevice { get; set; }
    public User? SecurityOfficerUser { get; set; }
}

/// <summary>
/// Tab 6 — Operational reporting instructions, handover obligations, assets, and pending approval routing.
/// Mapped to dbo.ST_StaffTransferReportingDetails.
/// </summary>
public sealed class StaffTransferReportingDetail
{
    public int StaffTransferReportingDetailId { get; set; }
    public int StaffTransferId { get; set; }
    public DateTime ApprovedTransferEffectiveDate { get; set; }
    public DateTime ReportingDate { get; set; }
    public TimeOnly? ReportingTime { get; set; }
    public string ReportingLocation { get; set; } = "";
    public int? ReportingToIndividualId { get; set; }
    public string? ReportingToNameSnapshot { get; set; }
    public bool HandoverRequired { get; set; }
    public DateTime? HandoverDeadline { get; set; }
    public string? OutstandingAssignmentsSummary { get; set; }
    public string? AssetsToReturnSummary { get; set; }
    public string? AssetsToReceiveSummary { get; set; }
    public string OutstandingApprovalsRoutingRule { get; set; } = StaffTransferApprovalRoutingRules.RetainHistoricalReassignOpen;
    public string? OutstandingApprovalsSummary { get; set; }
    public string? SpecialInstructions { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public Individual? ReportingToIndividual { get; set; }
}

/// <summary>
/// Optional non-blocking Supervisor/HOD commentary, employee clarifications, and participant audit notes.
/// Mapped to dbo.ST_StaffTransferComments.
/// </summary>
public sealed class StaffTransferComment
{
    public int StaffTransferCommentId { get; set; }
    public int StaffTransferId { get; set; }
    public int AuthorUserId { get; set; }
    public int? AuthorIndividualId { get; set; }
    public string ParticipantRole { get; set; } = StaffTransferParticipantRoles.SendingOfficer;
    public string CommentKind { get; set; } = StaffTransferCommentKinds.AdvisoryComment;
    public int RequestVersion { get; set; } = 1;
    public string CommentText { get; set; } = "";
    public bool IsVisibleToEmployee { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public User AuthorUser { get; set; } = null!;
    public Individual? AuthorIndividual { get; set; }
}

/// <summary>
/// Tab 7 — Formal, auditable employee confirmation history and confirmed snapshot version.
/// Mapped to dbo.ST_StaffTransferConfirmations.
/// </summary>
public sealed class StaffTransferConfirmation
{
    public int StaffTransferConfirmationId { get; set; }
    public int StaffTransferId { get; set; }
    public int ConfirmedVersion { get; set; }
    public string ResponseKind { get; set; } = StaffTransferConfirmationResponses.Confirmed;
    public int ConfirmedByUserId { get; set; }
    public int ConfirmedByIndividualId { get; set; }
    public string ConfirmationStatement { get; set; } = "";
    public string? EmployeeRemarks { get; set; }
    public string ReviewedSnapshotJson { get; set; } = "";
    public bool IsInvalidatedByMaterialEdit { get; set; }
    public DateTime? InvalidatedAtUtc { get; set; }
    public DateTime ConfirmedAtUtc { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public User ConfirmedByUser { get; set; } = null!;
    public Individual ConfirmedByIndividual { get; set; } = null!;
}

/// <summary>
/// Section 5 — Idempotent transfer activation execution steps, errors, retries, and reconciliation log.
/// Mapped to dbo.ST_StaffTransferActivationLogs.
/// </summary>
public sealed class StaffTransferActivationLog
{
    public long StaffTransferActivationLogId { get; set; }
    public int StaffTransferId { get; set; }
    public int ExecutionAttempt { get; set; } = 1;
    public string StepCode { get; set; } = StaffTransferActivationSteps.Validation;
    public string StepStatus { get; set; } = "Pending";
    public string IdempotencyKey { get; set; } = "";
    public string? DetailsJson { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ExecutedByUserId { get; set; }
    public DateTime ExecutedAtUtc { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public User? ExecutedByUser { get; set; }
}

/// <summary>
/// Section 7 — Printable A4 PDF snapshot, version, SHA-256 integrity hash, and access metadata.
/// Mapped to dbo.ST_StaffTransferDocuments.
/// </summary>
public sealed class StaffTransferDocument
{
    public long StaffTransferDocumentId { get; set; }
    public int StaffTransferId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentVariant { get; set; } = StaffTransferDocumentVariants.EmployeeNotice;
    public int RequestVersion { get; set; }
    public int? ConfirmedVersion { get; set; }
    public string SnapshotJson { get; set; } = "";
    public string Sha256Hash { get; set; } = "";
    public byte[]? PdfBytes { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public int GeneratedByUserId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public User GeneratedByUser { get; set; } = null!;
}

/// <summary>
/// Effective-dated organizational structure assignment history for a Job.
/// Preserves historical department/structure placement when Jobs.OrganisationStructureID changes on transfer activation.
/// Mapped to dbo.ST_JobOrganisationHistories.
/// </summary>
public sealed class JobOrganisationHistory
{
    public long JobOrganisationHistoryId { get; set; }
    public int JobId { get; set; }
    public int IndividualId { get; set; }
    public int OrganisationId { get; set; }
    public int OrganisationStructureId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int? StaffTransferId { get; set; }
    public string ChangeReason { get; set; } = "";
    public int? OperationLogId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Job Job { get; set; } = null!;
    public Individual Individual { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public OrganisationStructure OrganisationStructure { get; set; } = null!;
    public StaffTransfer? StaffTransfer { get; set; }
    public OperationLog? OperationLog { get; set; }
}

/// <summary>
/// Tracks multi-domain clearance tasks (work handover, asset custody, finance, IT access, door access)
/// required for external and internal transfers. Mapped to dbo.ST_StaffTransferClearanceTasks.
/// </summary>
public sealed class StaffTransferClearanceTask
{
    public int ClearanceTaskId { get; set; }
    public int StaffTransferId { get; set; }
    public string ClearanceDomain { get; set; } = StaffTransferClearanceDomains.WorkHandover;
    public string TaskTitle { get; set; } = "";
    public string? TaskDescription { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? AssignedToOrganisationStructureId { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsMandatoryForFinalApproval { get; set; }
    public bool IsMandatoryForActivation { get; set; } = true;
    public int? ClearedByUserId { get; set; }
    public DateTime? ClearedAtUtc { get; set; }
    public string? WaiverReason { get; set; }
    public string? Remarks { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public User? AssignedToUser { get; set; }
    public OrganisationStructure? AssignedToOrganisationStructure { get; set; }
    public User? ClearedByUser { get; set; }
}

/// <summary>
/// Tracks item-level asset disposition (return, retain, transfer ownership, maintenance, replace)
/// integrated with AM_AssetCustodies. Mapped to dbo.ST_StaffTransferAssetDecisions.
/// </summary>
public sealed class StaffTransferAssetDecision
{
    public int TransferAssetDecisionId { get; set; }
    public int StaffTransferId { get; set; }
    public int AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetDecision { get; set; } = StaffTransferAssetDecisions.ReturnToSource;
    public bool DestinationAccepted { get; set; }
    public int? HandoverConditionId { get; set; }
    public int? ReturnServiceRequestId { get; set; }
    public int? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
}

/// <summary>
/// Financial settlement between source and destination employers (advances, leave encashment, gratuity).
/// Mapped to dbo.ST_StaffTransferFinanceSettlements.
/// </summary>
public sealed class StaffTransferFinanceSettlement
{
    public int FinanceSettlementId { get; set; }
    public int StaffTransferId { get; set; }
    public DateTime LastWorkingDateAtSource { get; set; }
    public DateTime LastPayrollCutoffDate { get; set; }
    public decimal OutstandingAdvanceDeduction { get; set; }
    public decimal UnencashedLeaveDays { get; set; }
    public decimal LeaveEncashmentAmount { get; set; }
    public decimal GratuitySeveranceAmount { get; set; }
    public string SettlementMode { get; set; } = "InterCompanyTransfer";
    public int? FinanceOfficerUserId { get; set; }
    public bool IsCleared { get; set; }
    public DateTime? ClearedAtUtc { get; set; }
    public string? Remarks { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public User? FinanceOfficerUser { get; set; }
}

/// <summary>
/// Audit trail for destination Job and JobPosition creation on effective-date activation.
/// Mapped to dbo.ST_StaffTransferEmploymentExecutions.
/// </summary>
public sealed class StaffTransferEmploymentExecution
{
    public long ExecutionId { get; set; }
    public int StaffTransferId { get; set; }
    public int SourceJobId { get; set; }
    public int? DestinationJobId { get; set; }
    public int? DestinationJobPositionId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsNewContractCreated { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public DateTime ExecutedAtUtc { get; set; }
    public string? ExecutionDetailsJson { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public StaffTransfer StaffTransfer { get; set; } = null!;
    public Job SourceJob { get; set; } = null!;
    public Job? DestinationJob { get; set; }
    public JobPosition? DestinationJobPosition { get; set; }
}
