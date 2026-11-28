# DHR Schedule Manager — deployment and shadow-mode checklist

## Migration and rollout

1. Back up the production database and confirm the active EF migration history.
2. Apply the `WS_` migrations in chronological order during a maintenance window:
   `WorkforcePolicyFoundation`, `WorkforceRosterManagement`, `DutyOperations`, `WorkTimeLedger`, `WorkforceAttendanceSessions`, and `WorkforcePayrollFinalization`.
3. Verify the `WS_` tables, foreign keys, indexes, and existing `WORK_PLANNING` data remain intact. No legacy work plans or attendance records are replaced.
4. Grant Schedule Manager access separately from payroll export. Payroll export requires `Payroll.Export`; corrections require `Payroll.Finalise`.

## Shadow mode

1. Configure duty types and effective-dated policies for one organisation without publishing a workforce roster.
2. Create draft rosters for a representative period and compare validation output with the existing duty/attendance schedule:
   coverage, overlap, approved leave, rest, and qualification-matrix rules.
3. Do not publish plans or export payroll during the comparison period. Treat hard blocks as deployment defects and record override-required/warning results for policy tuning.
4. After business sign-off, publish one roster version and compare generated work plans, attendance sessions, and calculated ledger entries to the legacy process.
5. Enable payroll exports only after ledger reconciliation; record the external payroll batch reference used for every export.

## Monitoring

- Watch failed `DutyOperationApprovalCompletionService` log entries; approved operations remain retryable while their status is `Requested`.
- Review unpublished roster validation failures and policy warnings daily during shadow mode.
- Monitor ledger entries that remain `Calculated` beyond the agreed payroll cut-off and exported entries without an external reconciliation acknowledgement.
- Audit policy changes, policy assignments, roster publication, payroll exports, and correction adjustments through their operation logs.

## Rollback

- Disable Schedule Manager permissions and stop publishing new roster versions; existing work plans and attendance evidence continue to operate.
- Do not delete `WS_` data. Revert by disabling duty types/policies or by superseding roster versions.
- Do not reverse finalized ledger entries in place. Create an approved correction adjustment and export it in the next payroll batch.
- Database migration rollback is a last resort and requires a tested backup/restore plan because ledger exports may already have been reconciled externally.
