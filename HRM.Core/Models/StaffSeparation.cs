using System;
using System.Collections.Generic;
using HRM.Enums;

namespace HRM.Models;

/// <summary>
/// Configurable separation categories and default notice policies.
/// Mapped to dbo.SEP_SeparationTypes.
/// </summary>
public sealed class SeparationType
{
    public int SeparationTypeId { get; set; }
    public int? OrganisationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = StaffSeparationCategories.Resignation;
    public int DefaultNoticeDays { get; set; } = 30;
    public bool RequiresClearance { get; set; } = true;
    public bool RequiresExitInterview { get; set; } = true;
    public bool AllowsNoticeWaiver { get; set; } = true;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organisation? Organisation { get; set; }
    public ICollection<ClearanceTemplate> ClearanceTemplates { get; set; } = new List<ClearanceTemplate>();
    public ICollection<StaffSeparation> StaffSeparations { get; set; } = new List<StaffSeparation>();
}

/// <summary>
/// Configurable clearance checklist templates scoped to organisation, separation type, and job type.
/// Mapped to dbo.SEP_ClearanceTemplates.
/// </summary>
public sealed class ClearanceTemplate
{
    public int ClearanceTemplateId { get; set; }
    public int OrganisationId { get; set; }
    public int? SeparationTypeId { get; set; }
    public int? JobTypeId { get; set; }
    public string TemplateName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Organisation Organisation { get; set; } = null!;
    public SeparationType? SeparationType { get; set; }
    public JobType? JobType { get; set; }
    public ICollection<ClearanceTemplateTask> Tasks { get; set; } = new List<ClearanceTemplateTask>();
}

/// <summary>
/// Predefined checklist tasks inside a clearance template.
/// Mapped to dbo.SEP_ClearanceTemplateTasks.
/// </summary>
public sealed class ClearanceTemplateTask
{
    public int ClearanceTemplateTaskId { get; set; }
    public int ClearanceTemplateId { get; set; }
    public string Domain { get; set; } = StaffSeparationClearanceDomains.DepartmentalClearance;
    public string TaskName { get; set; } = "";
    public string? TaskDescription { get; set; }
    public int? AssignedOrganisationStructureId { get; set; }
    public int? AssignedRoleId { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int SequenceOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ClearanceTemplate ClearanceTemplate { get; set; } = null!;
    public OrganisationStructure? AssignedOrganisationStructure { get; set; }
    public Role? AssignedRole { get; set; }
}

/// <summary>
/// Main separation header, employee snapshot, notice timeline, and execution status.
/// Mapped to dbo.SEP_StaffSeparations.
/// </summary>
public sealed class StaffSeparation
{
    public int StaffSeparationId { get; set; }
    public int? RequestId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int OrganisationStructureId { get; set; }
    public int SeparationTypeId { get; set; }
    public string ReasonCategory { get; set; } = StaffSeparationCategories.Resignation;
    public string ReasonDetails { get; set; } = "";
    public string InitiationSource { get; set; } = StaffSeparationInitiationSources.EmployeeSelf;

    // Timeline and Notice
    public DateTime RequestedNoticeDate { get; set; }
    public int ContractualNoticeDays { get; set; }
    public bool NoticeWaiverRequested { get; set; }
    public bool NoticeWaiverApproved { get; set; }
    public int NoticeWaiverDays { get; set; }
    public int NoticeShortfallDays { get; set; }
    public decimal NoticeShortfallPenaltyAmount { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public DateTime EffectiveSeparationDate { get; set; }
    public string Status { get; set; } = StaffSeparationStatuses.Draft;

    // Employee Exit & Acknowledgement
    public string EmployeeAcknowledgementStatus { get; set; } = "Pending";
    public DateTime? EmployeeAcknowledgedAtUtc { get; set; }
    public string? EmployeeExitComments { get; set; }
    public bool ExitInterviewCompleted { get; set; }
    public DateTime? ExitInterviewDate { get; set; }
    public string? ExitInterviewNotes { get; set; }

    // Historical Staff & Job Snapshots
    public string FullNameSnapshot { get; set; } = "";
    public string EmployeeNumberSnapshot { get; set; } = "";
    public string? ProtectedIdCardSnapshot { get; set; }
    public string PositionTitleSnapshot { get; set; } = "";
    public string OrganisationNameSnapshot { get; set; } = "";
    public string DepartmentNameSnapshot { get; set; } = "";
    public DateTime DateJoinedSnapshot { get; set; }
    public decimal? BasicSalarySnapshot { get; set; }
    public int? SupervisorIndividualId { get; set; }
    public string? SupervisorNameSnapshot { get; set; }
    public string? OfficialEmailSnapshot { get; set; }
    public string? PersonalContactNumber { get; set; }
    public string? PersonalEmail { get; set; }
    public string? ForwardingAddress { get; set; }

    // Final Review & Execution
    public int? HrAdminReviewerUserId { get; set; }
    public DateTime? HrAdminReviewedAtUtc { get; set; }
    public string? HrReviewRemarks { get; set; }
    public DateTime? TerminationExecutedAtUtc { get; set; }
    public int? ExecutedByUserId { get; set; }
    public bool HasUnresolvedObligations { get; set; }
    public string? UnresolvedObligationsSummary { get; set; }

    // Audit and Concurrency
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // Navigations
    public Request? Request { get; set; }
    public Individual Individual { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public Organisation Organisation { get; set; } = null!;
    public OrganisationStructure OrganisationStructure { get; set; } = null!;
    public SeparationType SeparationType { get; set; } = null!;
    public Individual? SupervisorIndividual { get; set; }
    public User? HrAdminReviewerUser { get; set; }
    public User? ExecutedByUser { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public SeparationWorkHandover? WorkHandover { get; set; }
    public ICollection<ClearanceTask> ClearanceTasks { get; set; } = new List<ClearanceTask>();
    public ICollection<SeparationWorkHandoverItem> WorkHandoverItems { get; set; } = new List<SeparationWorkHandoverItem>();
    public ICollection<SeparationAssetClearance> AssetClearances { get; set; } = new List<SeparationAssetClearance>();
    public ICollection<SeparationFinanceItem> FinanceItems { get; set; } = new List<SeparationFinanceItem>();
    public ICollection<SeparationAccessRevocation> AccessRevocations { get; set; } = new List<SeparationAccessRevocation>();
    public ICollection<SeparationDocument> Documents { get; set; } = new List<SeparationDocument>();
    public ICollection<SeparationEvent> Events { get; set; } = new List<SeparationEvent>();
}

/// <summary>
/// Clearance task instance per domain and checklist item.
/// Mapped to dbo.SEP_ClearanceTasks.
/// </summary>
public sealed class ClearanceTask
{
    public int ClearanceTaskId { get; set; }
    public int StaffSeparationId { get; set; }
    public string Domain { get; set; } = StaffSeparationClearanceDomains.DepartmentalClearance;
    public string TaskName { get; set; } = "";
    public string? TaskDescription { get; set; }
    public int? AssignedOrganisationStructureId { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? AssignedToRoleId { get; set; }
    public string Status { get; set; } = StaffSeparationClearanceTaskStatuses.Pending;
    public bool IsMandatory { get; set; } = true;
    public int SequenceOrder { get; set; }
    public int? ClearedByUserId { get; set; }
    public DateTime? ClearedAtUtc { get; set; }
    public string? Remarks { get; set; }
    public string? WaiverReason { get; set; }
    public bool OutstandingObligation { get; set; }
    public decimal? OutstandingObligationAmount { get; set; }
    public string? OutstandingObligationDetails { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public StaffSeparation StaffSeparation { get; set; } = null!;
    public OrganisationStructure? AssignedOrganisationStructure { get; set; }
    public User? AssignedToUser { get; set; }
    public Role? AssignedToRole { get; set; }
    public User? ClearedByUser { get; set; }
}

/// <summary>
/// Operational work handover review and sign-off header.
/// Mapped to dbo.SEP_WorkHandovers.
/// </summary>
public sealed class SeparationWorkHandover
{
    public int WorkHandoverId { get; set; }
    public int StaffSeparationId { get; set; }
    public int SupervisorIndividualId { get; set; }
    public int? HodIndividualId { get; set; }
    public string SupervisorStatus { get; set; } = "Pending";
    public DateTime? SupervisorReviewedAtUtc { get; set; }
    public string? SupervisorComments { get; set; }
    public string HodStatus { get; set; } = "Pending";
    public DateTime? HodReviewedAtUtc { get; set; }
    public string? HodComments { get; set; }
    public bool AllItemsCompleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public StaffSeparation StaffSeparation { get; set; } = null!;
    public Individual SupervisorIndividual { get; set; } = null!;
    public Individual? HodIndividual { get; set; }
    public ICollection<SeparationWorkHandoverItem> Items { get; set; } = new List<SeparationWorkHandoverItem>();
}

/// <summary>
/// Granular duty, project assignment, file handover, or open workflow task handover item.
/// Mapped to dbo.SEP_WorkHandoverItems.
/// </summary>
public sealed class SeparationWorkHandoverItem
{
    public int WorkHandoverItemId { get; set; }
    public int WorkHandoverId { get; set; }
    public int StaffSeparationId { get; set; }
    public string ItemType { get; set; } = StaffSeparationWorkHandoverItemTypes.ProjectAssignment;
    public string ItemTitle { get; set; } = "";
    public string? Details { get; set; }
    public int RecipientIndividualId { get; set; }
    public DateTime TargetCompletionDate { get; set; }
    public string Status { get; set; } = StaffSeparationWorkHandoverItemStatuses.Pending;
    public int? VerifiedByIndividualId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? HandoverNotes { get; set; }

    public SeparationWorkHandover WorkHandover { get; set; } = null!;
    public StaffSeparation StaffSeparation { get; set; } = null!;
    public Individual RecipientIndividual { get; set; } = null!;
    public Individual? VerifiedByIndividual { get; set; }
}

/// <summary>
/// Links employee assets to AM_AssetCustodies and AM_AssetItems for return/damage inspection.
/// Mapped to dbo.SEP_AssetClearances.
/// </summary>
public sealed class SeparationAssetClearance
{
    public int SeparationAssetClearanceId { get; set; }
    public int StaffSeparationId { get; set; }
    public int? AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public int? AssetReturnId { get; set; }
    public int? AssetReturnItemId { get; set; }
    public int? ConditionOnReturnId { get; set; }
    public string DisposalAction { get; set; } = StaffSeparationAssetDisposalActions.PendingReturn;
    public decimal? AssessedDamageCost { get; set; }
    public bool DeductionFromFinalSettlement { get; set; }
    public string Status { get; set; } = "PendingReturn";
    public int? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? Remarks { get; set; }

    public StaffSeparation StaffSeparation { get; set; } = null!;
    public AssetCustody? AssetCustody { get; set; }
    public AssetItem AssetItem { get; set; } = null!;
    public AssetReturn? AssetReturn { get; set; }
    public AssetCondition? ConditionOnReturn { get; set; }
    public User? VerifiedByUser { get; set; }
}

/// <summary>
/// Financial breakdown lines for final settlement (salary arrears, leave encashment, gratuity, debts).
/// Mapped to dbo.SEP_FinanceItems.
/// </summary>
public sealed class SeparationFinanceItem
{
    public int SeparationFinanceItemId { get; set; }
    public int StaffSeparationId { get; set; }
    public string Category { get; set; } = StaffSeparationFinanceCategories.SalaryArrears;
    public string Description { get; set; } = "";
    public bool IsCredit { get; set; } // true = payable to staff, false = debit/deduction
    public decimal Amount { get; set; }
    public bool IsCleared { get; set; }
    public string? PayrollBatchReference { get; set; }
    public string? Remarks { get; set; }
    public int? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }

    public StaffSeparation StaffSeparation { get; set; } = null!;
    public User? VerifiedByUser { get; set; }
}

/// <summary>
/// IT systems, corporate accounts, portal roles, building door cards and keys revocation.
/// Mapped to dbo.SEP_AccessRevocations.
/// </summary>
public sealed class SeparationAccessRevocation
{
    public int SeparationAccessRevocationId { get; set; }
    public int StaffSeparationId { get; set; }
    public string SystemType { get; set; } = StaffSeparationAccessSystemTypes.HrmPortal;
    public string SystemName { get; set; } = "";
    public string AccountIdentifier { get; set; } = "";
    public string? DoorBuildingName { get; set; }
    public string? DoorCardNumber { get; set; }
    public string RevocationAction { get; set; } = StaffSeparationAccessRevocationActions.RevokeAccess;
    public DateTime ScheduledRevocationDate { get; set; }
    public string Status { get; set; } = StaffSeparationAccessRevocationStatuses.Pending;
    public DateTime? RevokedAtUtc { get; set; }
    public int? RevokedByUserId { get; set; }
    public string? ExecutionNotes { get; set; }

    public StaffSeparation StaffSeparation { get; set; } = null!;
    public User? RevokedByUser { get; set; }
}

/// <summary>
/// Official documents generated for separation (clearance certificate, service certificate, settlement statement).
/// Mapped to dbo.SEP_SeparationDocuments.
/// </summary>
public sealed class SeparationDocument
{
    public long SeparationDocumentId { get; set; }
    public int StaffSeparationId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentVariant { get; set; } = StaffSeparationDocumentVariants.ExitClearanceCertificate;
    public string Title { get; set; } = "";
    public byte[]? PdfBytes { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public string Sha256Hash { get; set; } = "";
    public int GeneratedByUserId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    public StaffSeparation StaffSeparation { get; set; } = null!;
    public User GeneratedByUser { get; set; } = null!;
}

/// <summary>
/// Immutable audit log of separation milestones and approvals.
/// Mapped to dbo.SEP_SeparationEvents.
/// </summary>
public sealed class SeparationEvent
{
    public long SeparationEventId { get; set; }
    public int StaffSeparationId { get; set; }
    public string EventType { get; set; } = StaffSeparationEventTypes.Initiated;
    public string EventTitle { get; set; } = "";
    public string? DetailsJson { get; set; }
    public int ActorUserId { get; set; }
    public string ActorName { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }

    public StaffSeparation StaffSeparation { get; set; } = null!;
    public User ActorUser { get; set; } = null!;
}
