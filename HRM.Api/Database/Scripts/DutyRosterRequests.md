# Duty roster and duty change requests

Open **Duty roster** from the employee or admin navigation, or **Today's Work Plan → Today's duty roster**. It defaults to today and displays staff, department, dated duty hours and required clock events. Normal staff see their department and their own duties. Staff with `Schedule.Attach` can view the current tenant and arrange changes on staff members' behalf.

1. Select a future duty and choose **Transfer** or **Swap** on that staff member's roster row. That person and duty remain fixed.
2. For transfers, select receiving staff from the dropdown. For swaps, choose another dated duty from the displayed staff-and-duty list; there is no staff dropdown. The other duty's owner sets the other side automatically. Enter a reason.
3. Both staff members independently accept or decline from Duty requests or their notification bell. Submitting does not count as acceptance. Either person may accept first; the request waits until both accept.
4. After both accept, approval follows the saved tenant / JobType / Request Type workflow. The installed default is **Supervisor approval**, restricted to the actual supervisor and configured Supervisor UserGroup. Participants and the submitting user cannot approve. The roster changes only after final approval.
5. The final approval moves dated duty assignments and clock segments to each receiving staff member's daily plan. A swap commits both movements in one database transaction. Official schedules remain in place.

The admin workspace has **Pending duty requests** linking to acceptance and approval actions. Notifications link directly to the roster. Cancelled, declined, rejected and approved requests remain in request history.

## Configuration and installation

Run `20261021_DutyRosterRequests.sql` once per database; it is rerunnable. It adds Duty Transfer Request and Duty Swap Request and supervisor defaults for active JobTypes in tenants with work planning configured. Existing custom flows are preserved. For new tenants or JobTypes, configure the two Request Types in **Settings → Configurations → Request Approval Flows**. Multiple sequential stages remain supported; Level 0 remains reserved.

The Request Type state-transition table remains authoritative. Defaults: Incomplete (1) → Pending staff completion (10) → Pending approval (3) → Approved (4), with cancellation (5) and rejection/decline (6) from states 10 and 3. Additional configured stages need their exact transitions and cancellation/rejection routes configured by a developer in State Transitions. State 10 is reserved for both staff members' acceptance. The workflow version is saved at submission.

For an existing installation, `20261021_DutyBothStaffAcceptance.sql` adds `SourceAcceptedByUserId` and `SourceAcceptedAtUtc`. Existing `AcceptedByUserId` and `AcceptedAtUtc` record the receiving staff member's acceptance. Source consent is never inferred from submission.

## Storage

New table **DC_DutyChangeRequests**:

`DutyChangeRequestId`, `RequestId`, `OrganisationId`, `Kind`, `SourceAssignmentId`, `SwapAssignmentId`, `SourceJobId`, `TargetJobId`, `SourceIndividualId`, `TargetIndividualId`, `SourceStart`, `SourceEnd`, `SwapStart`, `SwapEnd`, `Reason`, `SourceFingerprint`, `SwapFingerprint`, `Acceptance`, `CreatedByUserId`, `CreatedAtUtc`, `AcceptedByUserId`, `AcceptedAtUtc`, `IsOpen`, `TargetNewAssignmentId`, `SourceNewAssignmentId`.

Existing `Requests`, `WF_RequestInstances`, `WF_RequestStageInstances`, `WF_RequestApprovals`, `WF_RequestActions`, `RN_RequestNotifications` and `OperationLogs` record the workflow and audit. Final approval updates `WorkAssignments`, `WorkAssignmentOwners`, `WorkAssignmentSegments`, `WorkPlans` and `WorkPlanSegments`. Templates are reused. Original dated plans are retained as manual exceptions so regeneration cannot restore transferred duties.

## Checks

Future, active, unfinalized duty only; active tenant-bound jobs and recipient account; acceptance before approval; current eligible approver and state; no participant self approval; no duplicate pending reservation; approved-leave and overlapping-duty conflicts; receiving finalized plans and duty clock records. Official hours can overlap duty. Conflicts are rechecked before applying approval. Existing clock logs are not rewritten.

Automated coverage: `dotnet run --project Tests/AttendanceRefresh/AttendanceRefresh.csproj -- --duty-only`. Uses isolated test data.
