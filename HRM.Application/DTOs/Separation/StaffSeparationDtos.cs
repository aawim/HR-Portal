using System;
using System.Collections.Generic;

namespace HRM.DTOs.Separation;

public sealed class StaffSeparationDashboardData
{
    public int TotalCount { get; set; }
    public int PendingHandoverCount { get; set; }
    public int PendingClearanceCount { get; set; }
    public int PendingFinalReviewCount { get; set; }
    public int TerminatedCount { get; set; }
    public int UnresolvedObligationsCount { get; set; }

    public List<StaffSeparationSummaryDto> Separations { get; set; } = new();
    public List<SeparationTypeDto> SeparationTypes { get; set; } = new();
}

public sealed class StaffSeparationSummaryDto
{
    public int StaffSeparationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public string FullName { get; set; } = "";
    public string EmployeeNumber { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string PositionTitle { get; set; } = "";
    public string SeparationTypeName { get; set; } = "";
    public string ReasonCategory { get; set; } = "";
    public DateTime RequestedNoticeDate { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public DateTime EffectiveSeparationDate { get; set; }
    public string Status { get; set; } = "";
    public bool HasUnresolvedObligations { get; set; }
    public string EmployeeAcknowledgementStatus { get; set; } = "Pending";
    public int TotalTasksCount { get; set; }
    public int ClearedTasksCount { get; set; }
}

public sealed class SeparationTypeDto
{
    public int SeparationTypeId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int DefaultNoticeDays { get; set; }
    public bool RequiresClearance { get; set; }
    public bool RequiresExitInterview { get; set; }
    public bool AllowsNoticeWaiver { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StaffSeparationDetailDto
{
    public int StaffSeparationId { get; set; }
    public int? RequestId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int OrganisationId { get; set; }
    public int OrganisationStructureId { get; set; }
    public int SeparationTypeId { get; set; }
    public string SeparationTypeName { get; set; } = "";
    public string ReasonCategory { get; set; } = "";
    public string ReasonDetails { get; set; } = "";
    public string InitiationSource { get; set; } = "";

    // Notice & Dates
    public DateTime RequestedNoticeDate { get; set; }
    public int ContractualNoticeDays { get; set; }
    public bool NoticeWaiverRequested { get; set; }
    public bool NoticeWaiverApproved { get; set; }
    public int NoticeWaiverDays { get; set; }
    public int NoticeShortfallDays { get; set; }
    public decimal NoticeShortfallPenaltyAmount { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public DateTime EffectiveSeparationDate { get; set; }
    public string Status { get; set; } = "";

    // Employee Exit Feedback
    public string EmployeeAcknowledgementStatus { get; set; } = "Pending";
    public DateTime? EmployeeAcknowledgedAtUtc { get; set; }
    public string? EmployeeExitComments { get; set; }
    public bool ExitInterviewCompleted { get; set; }
    public DateTime? ExitInterviewDate { get; set; }
    public string? ExitInterviewNotes { get; set; }

    // Snapshots
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

    // Final Execution
    public int? HrAdminReviewerUserId { get; set; }
    public DateTime? HrAdminReviewedAtUtc { get; set; }
    public string? HrReviewRemarks { get; set; }
    public DateTime? TerminationExecutedAtUtc { get; set; }
    public bool HasUnresolvedObligations { get; set; }
    public string? UnresolvedObligationsSummary { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    // Collections
    public WorkHandoverDetailDto? WorkHandover { get; set; }
    public List<ClearanceTaskDto> ClearanceTasks { get; set; } = new();
    public List<AssetClearanceDto> AssetClearances { get; set; } = new();
    public List<FinanceItemDto> FinanceItems { get; set; } = new();
    public List<AccessRevocationDto> AccessRevocations { get; set; } = new();
    public List<SeparationDocumentDto> Documents { get; set; } = new();
    public List<SeparationEventDto> Events { get; set; } = new();

    // Financial balance summary
    public decimal TotalPayableAmount { get; set; }
    public decimal TotalDeductionAmount { get; set; }
    public decimal NetSettlementAmount => TotalPayableAmount - TotalDeductionAmount;
}

public sealed class WorkHandoverDetailDto
{
    public int WorkHandoverId { get; set; }
    public int StaffSeparationId { get; set; }
    public int SupervisorIndividualId { get; set; }
    public string SupervisorName { get; set; } = "";
    public int? HodIndividualId { get; set; }
    public string? HodName { get; set; }
    public string SupervisorStatus { get; set; } = "Pending";
    public DateTime? SupervisorReviewedAtUtc { get; set; }
    public string? SupervisorComments { get; set; }
    public string HodStatus { get; set; } = "Pending";
    public DateTime? HodReviewedAtUtc { get; set; }
    public string? HodComments { get; set; }
    public bool AllItemsCompleted { get; set; }
    public List<WorkHandoverItemDto> Items { get; set; } = new();
}

public sealed class WorkHandoverItemDto
{
    public int WorkHandoverItemId { get; set; }
    public int WorkHandoverId { get; set; }
    public int StaffSeparationId { get; set; }
    public string ItemType { get; set; } = "";
    public string ItemTitle { get; set; } = "";
    public string? Details { get; set; }
    public int RecipientIndividualId { get; set; }
    public string RecipientName { get; set; } = "";
    public DateTime TargetCompletionDate { get; set; }
    public string Status { get; set; } = "Pending";
    public string? HandoverNotes { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public sealed class ClearanceTaskDto
{
    public int ClearanceTaskId { get; set; }
    public int StaffSeparationId { get; set; }
    public string Domain { get; set; } = "";
    public string TaskName { get; set; } = "";
    public string? TaskDescription { get; set; }
    public int? AssignedOrganisationStructureId { get; set; }
    public string? AssignedDepartmentName { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsMandatory { get; set; } = true;
    public int SequenceOrder { get; set; }
    public string? ClearedByName { get; set; }
    public DateTime? ClearedAtUtc { get; set; }
    public string? Remarks { get; set; }
    public string? WaiverReason { get; set; }
    public bool OutstandingObligation { get; set; }
    public decimal? OutstandingObligationAmount { get; set; }
    public string? OutstandingObligationDetails { get; set; }
}

public sealed class AssetClearanceDto
{
    public int SeparationAssetClearanceId { get; set; }
    public int StaffSeparationId { get; set; }
    public int? AssetCustodyId { get; set; }
    public int AssetItemId { get; set; }
    public string AssetCode { get; set; } = "";
    public string AssetName { get; set; } = "";
    public string? SerialNumber { get; set; }
    public string CategoryName { get; set; } = "";
    public string DisposalAction { get; set; } = "PendingReturn";
    public decimal? AssessedDamageCost { get; set; }
    public bool DeductionFromFinalSettlement { get; set; }
    public string Status { get; set; } = "PendingReturn";
    public string? VerifiedByName { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? Remarks { get; set; }
}

public sealed class FinanceItemDto
{
    public int SeparationFinanceItemId { get; set; }
    public int StaffSeparationId { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsCredit { get; set; }
    public decimal Amount { get; set; }
    public bool IsCleared { get; set; }
    public string? PayrollBatchReference { get; set; }
    public string? Remarks { get; set; }
    public string? VerifiedByName { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}

public sealed class AccessRevocationDto
{
    public int SeparationAccessRevocationId { get; set; }
    public int StaffSeparationId { get; set; }
    public string SystemType { get; set; } = "";
    public string SystemName { get; set; } = "";
    public string AccountIdentifier { get; set; } = "";
    public string? DoorBuildingName { get; set; }
    public string? DoorCardNumber { get; set; }
    public string RevocationAction { get; set; } = "";
    public DateTime ScheduledRevocationDate { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByName { get; set; }
    public string? ExecutionNotes { get; set; }
}

public sealed class SeparationDocumentDto
{
    public long SeparationDocumentId { get; set; }
    public int StaffSeparationId { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentVariant { get; set; } = "";
    public string Title { get; set; } = "";
    public string ContentType { get; set; } = "application/pdf";
    public string Sha256Hash { get; set; } = "";
    public string? Base64Data { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string GeneratedByName { get; set; } = "";
}

public sealed class SeparationEventDto
{
    public long SeparationEventId { get; set; }
    public int StaffSeparationId { get; set; }
    public string EventType { get; set; } = "";
    public string EventTitle { get; set; } = "";
    public string? DetailsJson { get; set; }
    public string ActorName { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
}

// Input DTOs
public sealed class StaffSeparationInitiateInput
{
    public int? StaffSeparationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int SeparationTypeId { get; set; }
    public string ReasonDetails { get; set; } = "";
    public DateTime RequestedNoticeDate { get; set; }
    public DateTime LastWorkingDate { get; set; }
    public DateTime EffectiveSeparationDate { get; set; }
    public bool NoticeWaiverRequested { get; set; }
    public int NoticeWaiverDays { get; set; }
    public string? PersonalContactNumber { get; set; }
    public string? PersonalEmail { get; set; }
    public string? ForwardingAddress { get; set; }
}

public sealed class StaffSeparationEmployeeAcknowledgeInput
{
    public int StaffSeparationId { get; set; }
    public string AcknowledgementStatus { get; set; } = "Acknowledged";
    public string? ExitComments { get; set; }
    public string? PersonalContactNumber { get; set; }
    public string? PersonalEmail { get; set; }
    public string? ForwardingAddress { get; set; }
}

public sealed class StaffSeparationHandoverItemInput
{
    public int? WorkHandoverItemId { get; set; }
    public int StaffSeparationId { get; set; }
    public string ItemType { get; set; } = "ProjectAssignment";
    public string ItemTitle { get; set; } = "";
    public string? Details { get; set; }
    public int RecipientIndividualId { get; set; }
    public DateTime TargetCompletionDate { get; set; }
    public string Status { get; set; } = "Pending";
    public string? HandoverNotes { get; set; }
}

public sealed class StaffSeparationHandoverReviewInput
{
    public int StaffSeparationId { get; set; }
    public bool IsSupervisor { get; set; }
    public string Decision { get; set; } = "Approved"; // Approved, ChangesRequested
    public string? Comments { get; set; }
}

public sealed class StaffSeparationAssetInspectionInput
{
    public int SeparationAssetClearanceId { get; set; }
    public string DisposalAction { get; set; } = "ReturnedInGoodOrder";
    public decimal? AssessedDamageCost { get; set; }
    public bool DeductionFromFinalSettlement { get; set; }
    public string Status { get; set; } = "Cleared";
    public string? Remarks { get; set; }
}

public sealed class StaffSeparationFinanceItemInput
{
    public int? SeparationFinanceItemId { get; set; }
    public int StaffSeparationId { get; set; }
    public string Category { get; set; } = "SalaryArrears";
    public string Description { get; set; } = "";
    public bool IsCredit { get; set; } = true;
    public decimal Amount { get; set; }
    public bool IsCleared { get; set; }
    public string? PayrollBatchReference { get; set; }
    public string? Remarks { get; set; }
}

public sealed class StaffSeparationClearanceTaskActionInput
{
    public int ClearanceTaskId { get; set; }
    public string Status { get; set; } = "Cleared"; // Cleared, Waived, Blocked
    public string? Remarks { get; set; }
    public string? WaiverReason { get; set; }
    public bool OutstandingObligation { get; set; }
    public decimal? OutstandingObligationAmount { get; set; }
    public string? OutstandingObligationDetails { get; set; }
}

public sealed class StaffSeparationAccessRevocationActionInput
{
    public int SeparationAccessRevocationId { get; set; }
    public string Status { get; set; } = "Revoked"; // Revoked, CardCollected, Scheduled, Waived
    public string? ExecutionNotes { get; set; }
}

public sealed class StaffSeparationFinalReviewInput
{
    public int StaffSeparationId { get; set; }
    public string? HrReviewRemarks { get; set; }
    public bool ExitInterviewCompleted { get; set; }
    public DateTime? ExitInterviewDate { get; set; }
    public string? ExitInterviewNotes { get; set; }
    public bool ExecuteTerminationNow { get; set; } = true;
}

public sealed class StaffSeparationFormLookupsDto
{
    public List<SeparationTypeDto> SeparationTypes { get; set; } = new();
    public List<LookupItemDto> Departments { get; set; } = new();
    public List<LookupItemDto> ActiveStaff { get; set; } = new();
}

public sealed class LookupItemDto
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string? SecondaryText { get; set; }
}
