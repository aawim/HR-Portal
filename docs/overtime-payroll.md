# Overtime payroll

Open Overtime → Approved & payroll (also linked from workspace pending overtime). HR needs the effective `Overtime.Pay` permission within the current organisation's permission ceiling. No new grants are assigned automatically.

Filter the work-date period and payroll status, select approved claims, choose the payroll month and reference, then Process payroll. Work period and payroll month are separate to support arrears. The full selection is validated atomically. Own claims, pending claims and previously processed/paid claims cannot be processed. Retry keys and a unique claim key prevent duplicate processing. Processing is a payroll allocation, not a payment confirmation.

Approval state remains Approved. Payroll status becomes Payroll processed with month and reference. These claims, including legacy paid claims, cannot be cancelled, rejected, returned or otherwise moved to another request state; a SQL trigger also enforces that lock.

Before processing, the requesting employee can cancel their approved claim; the current independent department/team supervisor can cancel, reject or return it as Incomplete. Each action requires a comment and an explicit RequestTypesAllowedRequestStateTransitions edge. The migration adds only approved-OT edges 4→1, 4→5, 4→6 and returned-OT 1→5. Other request types retain their final-state lock.

A returned claim retains its clock reservation. Its employee may correct the reason and resubmit the saved clock evidence through the configured approval stages; approval attempts and previous policy snapshots remain in audit history. Cancelled/rejected claims release their date/time evidence and allowance for reuse. Processed claims retain their reservation.

## Tables

- `OT_PayrollBatches`: Id, OrganisationId, PayrollMonth, Reference, ClientBatchId, ProcessedByUserId, ProcessedAtUtc, OperationLogId.
- `OT_PayrollItems`: RequestId (unique), BatchId, IndividualId, JobId, WorkDate, Minutes, RequestReference.

Existing OT claims, payments, workflow history and leave balances are preserved. Apply the additive OvertimePayrollProcessing migration after the restored-database rehearsal. A downgrade that discards payroll records is refused.
