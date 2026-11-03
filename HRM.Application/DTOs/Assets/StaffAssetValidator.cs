using HRM.Enums;

namespace HRM.DTOs.Assets;

public static class StaffAssetValidator
{
    public const string DefaultAcknowledgementStatement =
        "I confirm that I have reviewed the physical item(s) listed in this handover record. For each accepted item, I acknowledge physical receipt and responsibility for its safe custody and return in accordance with organizational policy. (System-recorded employee acknowledgement; not a statutory qualified digital signature.)";

    public static List<string> ValidateAssetItem(AssetItemSaveInput input, bool requiresSerialNumber)
    {
        var errors = new List<string>();
        if (input.CategoryId <= 0)
            errors.Add("Asset category is required.");
        if (string.IsNullOrWhiteSpace(input.AssetCode))
            errors.Add("Asset code is required.");
        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add("Asset name/description is required.");
        if (requiresSerialNumber && string.IsNullOrWhiteSpace(input.SerialNumber))
            errors.Add("Serial number is required for the selected asset category.");
        if (input.CurrentConditionId <= 0)
            errors.Add("Current condition is required.");
        if (!AssetInventoryStatuses.All.Contains(input.Status))
            errors.Add($"Invalid inventory status '{input.Status}'.");
        return errors;
    }

    public static List<string> ValidateIssuanceDraft(AssetIssuanceDraftInput input)
    {
        var errors = new List<string>();
        if (input.IndividualId <= 0)
            errors.Add("Employee selection is required.");
        if (input.JobId <= 0)
            errors.Add("Active job selection is required.");
        if (string.IsNullOrWhiteSpace(input.Reason))
            errors.Add("Issuance reason is required.");
        if (input.Lines.Count == 0)
            errors.Add("At least one serialized asset item must be added to the issuance.");

        var distinctAssets = new HashSet<int>();
        foreach (var line in input.Lines)
        {
            if (line.AssetItemId <= 0)
                errors.Add("Each issuance line must select a valid physical asset.");
            else if (!distinctAssets.Add(line.AssetItemId))
                errors.Add($"Asset #{line.AssetItemId} is selected more than once in the same issuance.");

            if (line.ConditionAtIssueId <= 0)
                errors.Add("Condition at issue is required for every line.");
        }

        return errors;
    }

    public static List<string> ValidateStaffDecisionBatch(StaffAssetDecisionBatchInput input)
    {
        var errors = new List<string>();
        if (input.AssetIssuanceId <= 0)
            errors.Add("Issuance reference is required.");
        if (!input.ExplicitStatementConfirmed)
            errors.Add("You must confirm the acknowledgement statement before submitting your decision.");
        if (input.Decisions.Count == 0)
            errors.Add("At least one item decision is required.");

        foreach (var d in input.Decisions)
        {
            if (d.AssetIssuanceItemId <= 0)
                errors.Add("Invalid issuance item line.");
            if (!AssetStaffDecisions.All.Contains(d.Decision))
                errors.Add($"Invalid decision '{d.Decision}'.");
            if (d.Decision == AssetStaffDecisions.Rejected)
            {
                if (string.IsNullOrWhiteSpace(d.RejectionReasonCode) && string.IsNullOrWhiteSpace(d.Remarks))
                    errors.Add($"Rejection reason and remarks are mandatory when rejecting an item (line #{d.AssetIssuanceItemId}).");
                if (string.IsNullOrWhiteSpace(d.Remarks))
                    errors.Add($"Please provide detailed remarks for rejected item line #{d.AssetIssuanceItemId}.");
            }
        }

        return errors;
    }

    public static List<string> ValidateHrReviewBatch(AssetHrReviewBatchInput input)
    {
        var errors = new List<string>();
        if (input.AssetIssuanceId <= 0)
            errors.Add("Issuance reference is required.");
        if (input.Decisions.Count == 0)
            errors.Add("At least one HR item decision is required.");

        foreach (var d in input.Decisions)
        {
            if (d.AssetIssuanceItemId <= 0)
                errors.Add("Invalid issuance item line.");
            if (!AssetHrDecisionKinds.All.Contains(d.Decision))
                errors.Add($"Invalid HR decision '{d.Decision}'.");
            if (d.Decision is AssetHrDecisionKinds.ReturnedForCorrection or AssetHrDecisionKinds.Closed &&
                string.IsNullOrWhiteSpace(d.Comments))
                errors.Add($"Comments are required when returning for correction or closing line #{d.AssetIssuanceItemId}.");
        }

        return errors;
    }
}
