using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Enums;

namespace HRM.DTOs.StaffTransfer;

/// <summary>
/// Domain and business validation rules for Internal Staff Transfer Management.
/// Enforces tab completeness, date relationships, appointment modes, material-change reconfirmation,
/// and approval authority constraints.
/// </summary>
public static class StaffTransferValidator
{
    public const string DefaultConfirmationStatement =
        "I confirm that I have reviewed the transfer information, proposed job and work arrangements, reporting instructions, and access arrangements presented in this request. I understand the transfer details and acknowledge the stated reporting requirements.";

    /// <summary>
    /// Validates a transfer draft or submission input across Tabs 1–6.
    /// </summary>
    public static void ValidateSaveInput(StaffTransferSaveInput input, int sourceOrganisationId, int sourceStructureId)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Job);
        ArgumentNullException.ThrowIfNull(input.Work);
        ArgumentNullException.ThrowIfNull(input.Reporting);

        if (input.IndividualId <= 0 || input.CurrentJobId <= 0)
            throw new InvalidOperationException("Select an eligible employee and active job record.");

        if (input.Job.DestinationOrganisationId <= 0 || input.Job.DestinationOrganisationStructureId <= 0)
            throw new InvalidOperationException("Select a valid destination organisation and department/structure.");

        if (!TransferAppointmentModes.All.Contains(input.Job.TransferAppointmentMode))
            throw new InvalidOperationException("Select a valid transfer appointment mode.");

        // Prevent a no-op reassignment where neither structure nor position changes
        var sameStructure = sourceOrganisationId == input.Job.DestinationOrganisationId &&
                            sourceStructureId == input.Job.DestinationOrganisationStructureId;
        var samePosition = input.Job.ExistingPositionId == input.Job.DestinationPositionId;
        if (sameStructure && samePosition && input.Job.TransferAppointmentMode == TransferAppointmentModes.OrganisationalReassignment)
            throw new InvalidOperationException("Destination department/structure must differ from the current department for an organisational reassignment.");

        if (input.Job.TransferAppointmentMode is TransferAppointmentModes.SameJobPositionChange or TransferAppointmentModes.NewEmploymentAppointment &&
            (!input.Job.DestinationPositionId.HasValue || input.Job.DestinationPositionId.Value <= 0))
            throw new InvalidOperationException("Select the destination job position when changing position or creating a new employment appointment.");

        if (string.IsNullOrWhiteSpace(input.Job.TransferReason) || input.Job.TransferReason.Trim().Length > 1000)
            throw new InvalidOperationException("Enter a transfer reason and justification of up to 1,000 characters.");

        var effectiveDate = input.Job.EffectiveDate.Date;
        if (effectiveDate.Year is < 1900 or > 2200)
            throw new InvalidOperationException("Select a valid transfer effective date.");

        var reportingDate = input.Reporting.ReportingDate.Date;
        if (reportingDate.Year is < 1900 or > 2200)
            throw new InvalidOperationException("Select a valid reporting date.");

        if (input.Reporting.HandoverRequired && input.Reporting.HandoverDeadline.HasValue &&
            input.Reporting.HandoverDeadline.Value.Date > reportingDate.AddDays(30))
            throw new InvalidOperationException("The handover deadline cannot exceed 30 days after the reporting date.");

        if (input.SubmitImmediately)
        {
            if (string.IsNullOrWhiteSpace(input.Work.ProposedLocationName))
                throw new InvalidOperationException("Specify the proposed work location in the Work tab before submitting.");

            if (input.Work.WeeklyWorkingHours is <= 0 or > 168)
                throw new InvalidOperationException("Weekly working hours must be between 0.5 and 168 hours.");

            if (string.IsNullOrWhiteSpace(input.Reporting.ReportingLocation))
                throw new InvalidOperationException("Specify the reporting location in the Reporting tab before submitting.");
        }
    }

    /// <summary>
    /// Validates employee confirmation or correction/clarification input (Tab 7).
    /// </summary>
    public static void ValidateEmployeeConfirmation(
        StaffTransferEmployeeConfirmationInput input,
        int currentRequestVersion,
        string currentStatus)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (currentStatus is not (StaffTransferStatuses.Submitted or StaffTransferStatuses.PendingStaffConfirmation))
            throw new InvalidOperationException("This transfer is not currently awaiting employee confirmation.");

        if (input.ReviewedVersion != currentRequestVersion)
            throw new InvalidOperationException("The transfer details have been updated since you opened this view. Please review the latest version before confirming.");

        if (!StaffTransferConfirmationResponses.All.Contains(input.ResponseKind))
            throw new InvalidOperationException("Select a valid confirmation response.");

        if (input.ResponseKind == StaffTransferConfirmationResponses.Confirmed && !input.AcknowledgedStatement)
            throw new InvalidOperationException("Acknowledge the confirmation statement before confirming the transfer.");

        if (input.ResponseKind != StaffTransferConfirmationResponses.Confirmed &&
            string.IsNullOrWhiteSpace(input.EmployeeRemarks))
            throw new InvalidOperationException("Enter details explaining the requested correction or clarification.");
    }

    /// <summary>
    /// Validates the final receiving department approval, rejection, or return for correction.
    /// Enforces Rules 2, 5, 6, 7, 8, and 9 from Section 2.
    /// </summary>
    public static void ValidateReceivingDecision(
        StaffTransferDecisionInput input,
        string currentStatus,
        int currentVersion,
        int? confirmedVersion,
        int requestedByUserId,
        int actingUserId,
        bool allowSelfApprovalOverride = false)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Decision is not ("Approve" or "Reject" or "ReturnForCorrection"))
            throw new InvalidOperationException("Select a valid decision (Approve, Reject, or ReturnForCorrection).");

        if (input.ExpectedVersion != currentVersion)
            throw new InvalidOperationException("The transfer request version changed. Refresh before recording a decision.");

        if (actingUserId == requestedByUserId && !allowSelfApprovalOverride)
            throw new UnauthorizedAccessException("The final approver must be different from the transfer initiator.");

        if (input.Decision is "Reject" or "ReturnForCorrection" && string.IsNullOrWhiteSpace(input.Remarks))
            throw new InvalidOperationException("A reason is required when rejecting or returning a transfer request for correction.");

        if (input.Decision == "Approve")
        {
            if (currentStatus != StaffTransferStatuses.PendingReceivingApproval)
                throw new InvalidOperationException("Receiving department approval can only occur when the transfer is in PendingReceivingApproval state.");

            if (!confirmedVersion.HasValue || confirmedVersion.Value != currentVersion)
                throw new InvalidOperationException("Receiving approval cannot occur while mandatory employee confirmation for the current version is outstanding.");
        }
    }

    /// <summary>
    /// Computes a deterministic SHA-256 fingerprint of material transfer fields across Tabs 2–6.
    /// Any change to this fingerprint after employee confirmation invalidates ConfirmedVersion and requires reconfirmation.
    /// </summary>
    public static string ComputeMaterialFingerprint(StaffTransferSaveInput input)
    {
        var materialPayload = new
        {
            input.CurrentJobId,
            input.Job.DestinationOrganisationId,
            input.Job.DestinationOrganisationStructureId,
            input.Job.DestinationJobTypeId,
            input.Job.DestinationPositionId,
            input.Job.DestinationPositionMatrixId,
            input.Job.DestinationJobMatrixStepId,
            input.Job.TransferTypeId,
            input.Job.TransferAppointmentMode,
            input.Job.HasSalaryBenefitChange,
            input.Job.ProposedBasicSalary,
            input.Job.ProposedAllowance,
            input.Job.NewReportingSupervisorIndividualId,
            EffectiveDate = input.Job.EffectiveDate.Date,
            input.Work.ProposedLocationId,
            ProposedLocationName = input.Work.ProposedLocationName.Trim(),
            input.Work.ProposedPlanningProviderId,
            input.Work.ProposedWorkTemplateId,
            input.Work.ProposedScheduleMode,
            input.Work.WorkingDaysMask,
            input.Work.StandardStartTime,
            input.Work.StandardEndTime,
            input.Work.WeeklyWorkingHours,
            input.Work.RequiredAttendanceMethod,
            input.Work.IsOvertimeEligible,
            input.Work.WorkArrangementMode,
            ScheduleEffectiveDate = input.Work.ScheduleEffectiveDate.Date,
            ReportingDate = input.Reporting.ReportingDate.Date,
            input.Reporting.ReportingTime,
            ReportingLocation = input.Reporting.ReportingLocation.Trim(),
            input.Reporting.ReportingToIndividualId,
            input.Reporting.HandoverRequired,
            HandoverDeadline = input.Reporting.HandoverDeadline?.Date,
            Access = input.AccessChanges
                .OrderBy(a => a.AccessAreaCode, StringComparer.Ordinal)
                .Select(a => new { a.AccessAreaCode, a.TransitionAction, a.ProposedScopeSummary, a.RoleId, a.UserGroupId }),
            DoorAccess = input.DoorAccessChanges
                .OrderBy(d => d.BuildingName, StringComparer.Ordinal)
                .ThenBy(d => d.DepartmentOfficeArea, StringComparer.Ordinal)
                .Select(d => new { d.BuildingName, d.FloorName, d.DepartmentOfficeArea, d.RestrictedRoomName, d.TransitionAction, AccessStartDate = d.AccessStartDate.Date })
        };

        var json = JsonSerializer.Serialize(materialPayload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
