# Overtime requests

Open `/online/overtime-requests`. The existing workspace pending OT component also lists preapprovals and missing-clock requests. HR Requests lists all three Request Types and links to this page.

1. **OT preapproval:** select from/to dates, enter one task per line and the purpose. Submit to the configured approval flow. Preapproval does not create payable hours.
2. **Planned OT claim:** after final approval, select the request, a task, work date and saved start/end clock events. Add work performed and submit the claim for approval.
3. **Unplanned OT claim:** select saved events and explain the work performed; no preapproval is required. The claim still follows the configured approval flow.
4. **OT clock events:** request missing start/end punches and explain them. No attendance log is created until final approval. Then refresh clock events and submit a planned or unplanned claim.

Under Settings → Configurations → Job Type Configurations → OT Config, enable overtime and set daily/weekly limits. Under Request approval flows configure **Overtime Preapproval Request**, **Overtime Clock Event Request**, and **Overtime Claim Request** for the tenant and Job Type. Configure their allowed state transitions in the existing developer settings. No approval stages or permissions are silently granted by deployment. Missing configuration uses the existing HR/developer configuration-alert mechanism.

Claims accept database log IDs, not entered hours or timestamps. Both events must be valid, owned by the requesting employee and current tenant, ordered, completed and within the three-month claim window. Intervals are at most 24 hours and entirely outside official work hours, including approved schedule amendments and late-arrival adjustments. A configured off day is allowed; a working day without a dated official plan is blocked until its schedule can be verified. Night intervals are split across daily and weekly limits; pending and approved claims consume limits. Overlapping claims cannot reuse an interval. Employment and Job Type eligibility are rechecked on approval. Approved tasks must belong to the same employee, tenant and job and cover the work dates. Rejected/cancelled claims release their reservation.

Existing historical claims remain marked **Legacy** with their original approval/payment records. New claims on a date with a pending or approved legacy claim require HR review to avoid double claiming unsupported historical time. Existing paid claims are preserved. Missing-clock approval records its audit log and exact log IDs and does not alter official schedules or resolve the punches as official attendance.

Tables:

- `OT_WorkRequests`: RequestId, OrganisationId, IndividualId, JobId, Kind, FromDate, ToDate, ProposedStart, ProposedEnd, StartLogId, EndLogId, Reason, CreatedAtUtc.
- `OT_WorkTasks`: Id, WorkRequestId, Description.
- Existing `OT_OvertimeRequests`: adds ClaimSource, StartLogId, EndLogId, StartedAt, EndedAt, PreapprovalTaskId. Existing columns and rows remain.
- Existing `Requests`, `WF_` workflow tables and notification tables retain request state, actions, approvers and notifications.

Deploy migration `20261026085804_OvertimeWorkRequests` before restarting the application. It adds request-type records using generated IDs and preserves all existing business data. Rollback requires the pre-upgrade backup; evidence is never dropped by a down migration.
