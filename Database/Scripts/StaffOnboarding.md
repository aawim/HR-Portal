# Staff onboarding

Apply `20261022_StaffOnboarding.sql` before deploying the application. The script is additive and rerunnable; it does not onboard any person or create an employee account.

## User flow

1. HR searches `/admin/hr/individuals` and selects **Add as Staff**.
2. On `/admin/hr/onboarding`, HR selects a department, Job Type and joining date. An existing position is optional; position catalogue management is outside this change. Address and contact details are not required for submission or approval.
3. Submission creates a central **Job Position Request** and a proposed appointment in `SO_StaffOnboardingRequests`. It creates neither `Jobs` nor `Users` while approval is pending.
4. The existing tenant/JobType **Request Approval Flows** configuration determines ordered approval stages and User Groups. Supervisor stages resolve against the proposed department because the individual has no approved job yet. The individual cannot approve their own appointment. A submitting HR officer can approve another individual's appointment only when assigned through the configured flow. Level 0 is not enabled.
5. Assigned approvers receive bell notifications and can open the request directly. The workspace right panel includes **Pending Staff Onboarding**. Reject/cancel require a reason and an allowed request-state transition.
6. Final approval creates or reuses `Staffs`, creates the approved `Jobs` record and, if selected, `JobPositions`; links `JobRequests`, `RequestJobs` and optionally `JobPositionsRequests`; creates or reuses the individual’s `Users` account. All writes, final workflow state and notifications commit in one transaction.
7. A new account is linked to the individual’s recorded National ID and the tenant’s User Organisation. No direct permissions or User Groups are granted by onboarding. Existing accounts retain their restrictions; mismatched identity/home organisation must be resolved by an administrator. The employee receives a notification to complete personal details later.

No approval stages existed for this request type when this change was installed. Configure **Settings → Leaves → Request Approval Flows**, select **Job Position Request** and each required Job Type, then choose the stages and User Groups. Submission fails with an inline explanation until a valid flow and independent approvers exist. Custom processing-state routes must also exist in `RequestTypesAllowedRequestStateTransitions`.

`20261022_StaffOnboardingApproverRepair.sql` restores a previously excluded submitting HR officer only when they already belong to the current stage's configured User Group and are not the individual being appointed. It queues the action notification without approving the request or creating an account. Workflow completion uses the database's permitted values: stage `Completed`, execution `Succeeded`, and unused approval decisions `Skipped`.

## Database changes

New table `SO_StaffOnboardingRequests`:

| Column | Purpose |
| --- | --- |
| RequestId | Primary key and central request link |
| IndividualId | Existing registry identity |
| OrganisationId | Owning tenant |
| OrganisationStructureId | Proposed department |
| JobTypeId | Proposed employment type / approval-flow selection |
| PositionId | Optional existing position |
| JoinedDate | Proposed joining date |
| Remarks | Appointment remarks, up to 1,000 characters |
| CreatedByUserId / CreatedAtUtc | Submitting HR officer and timestamp |
| IsOpen | Pending reservation; unique per individual while open |
| JobId / UserId | Resulting employment and account, set after approval |
| RowVersion | Concurrent action protection |

The existing **Job Position Request** type gets the onboarding view URL. Missing standard transitions are added without deleting existing transitions. Approval assignments, stage instances, decisions and action history use the existing `WF_*` tables. No existing staff balances, schedules, employment or user accounts are changed by installation.

The shared registry remains searchable for unassigned people. An open onboarding request reserves an individual for its organisation; after approval, existing employment visibility rules apply. Final approval revalidates identity, tenant, department, Job Type, optional position and overlapping employment. Per-person and per-request locks plus a unique open-request index prevent duplicate appointments.
