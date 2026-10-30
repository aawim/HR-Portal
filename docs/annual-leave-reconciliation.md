# Annual Leave catalogue reconciliation

Status: approved by the user and applied on 30 October 2026 as migration `20261030090000_ReconcileAnnualLeaveCatalogue`.

The running leave application, policy and set services use the unified `LT_LeaveTypes` catalogue. The duplicate is two records in that catalogue, rather than a second runtime legacy catalogue.

| Record | Scope | Code | Request type | Proposed handling |
| --- | --- | --- | --- | --- |
| Leave type 1, Annual Leave | Shared | AL01 | 34 | Keep as the canonical type |
| Leave type 66, Annual Leave | NCIT (organisation 3) | LT-66 | 34 | Retained as inactive audit history |

Verified impact on 30 October 2026:

- Repoint three existing `JobLeaveTypes` records from 66 to 1, retaining their IDs, balances and effective periods.
- Repoint seven existing `Leaves` records from 66 to 1. All seven are cancelled or rejected. Preserve IDs, dates, durations and states.
- Map one saved `LeaveReasons` reference to the matching reason under type 1; preserve its text and keep the old reason templates for historical metadata.
- Retire the redundant zero-balance assignment 9678 for Job 906 (`IsValid=false`), keeping its zero balance and record. Assignment 9683 has 30 days. Do not add balances together or create entitlement days.
- Disable the redundant type-66 link in handover rule 1; its type-1 link is already active.
- No type-66 leave-set, accrual-rule, leave-policy or workflow-definition references currently exist; those configurations require no rewrite.

The correction must run atomically, snapshot each changed record in the existing `LT_TypeMigrationAudits`, assert these identities and dependency counts, and verify that balance amounts and leave dates/durations/states are unchanged. Changed conditions must stop the correction for review. Keep the old type and all employment/leave records; do not delete them.

Code safeguards prepared separately: reject conflicting active names across shared/tenant visibility (including reactivation), and restrict leave-set and accrual selectors/assignment validation to active unified catalogue records. Management views retain inactive definitions for audit.

The user explicitly approved this correction after the initial automatic approval review. A transaction-only trial passed and was rolled back before applying the correction. Post-commit verification found one active Annual Leave type, all three transferred balances still at 30 days, zero amount mismatches against the snapshots, and the seven leave states unchanged (two cancelled, five rejected). Fourteen snapshots were saved. Reapplying the deployment script made no further changes. Five catalogue regression tests passed.

Deployment: run EF migrations normally, or run `HRM.Api/Database/Scripts/ApplyAnnualLeaveReconciliation.sql` with SQLCMD from the repository root. Both use the same embedded correction and migration identity; the deployment wrapper records migration history atomically with the correction. The correction asserts the approved record IDs and dependency counts and refuses an automatic rollback that could split balances subsequently used.
