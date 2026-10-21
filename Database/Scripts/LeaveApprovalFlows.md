# Request approval flows

Apply `20261020_LeaveApprovalFlows.sql` before deploying this code (or run
the `20261020120000_AddLeaveFlowScope` EF migration). The script adds two
nullable columns and preserves existing workflow definitions and requests.
Also apply `20261020_LeaveApprovalUserGroups.sql` (or migration
`20261020130000_AddLeaveApprovalUserGroups`) to allow UserGroup and
SupervisorGroup approver types. No additional columns are needed: the selected
UserGroupId is stored in WF_StageApprovers.ApproverReferenceID.
Apply `20261020_IndependentLeaveChangeRequests.sql` (or migration
`20261020140000_IndependentLeaveChangeRequests`) to create Request Type 105,
Early Return Request (ER numbering), and 106, Leave Extension Request (LEX
numbering). The script rejects ID collisions and is safe to rerun. Existing
requests of type 48 remain supported without rewriting their workflow history.

Open **Settings → Leaves Management → Request Approval Flows** (`/admin/settings/leaves/approval-flows`) as HR
or an administrator. Select the current organisation in the app, then select
Job Type, Request Type and approval level. Early Return Request and Leave
Extension Request are independent Request Types; there is no request-kind
selector. Level 1 has one stage,
Level 2 two stages, and so on (up to 20). Give each stage a name, User Group,
distinct processing status and reviewer scope. Reorder stages with Move up/down.
Level 0 is reserved for future AI/condition-based approval and cannot be enabled.
Save each required
combination. The saved flow applies to all staff whose job has that Job Type,
independently of Leave Type. It uses the existing RequestTypeID column;
new versions have a null LeaveTypeId. The original leave-specific definitions
remain attached to in-flight requests. An unambiguous old flow is offered as
a draft when configuring its Request Type, then saved as a new version.
staff do not need individual configuration. Early returns use HR only, extensions use Supervisor → HR, and
new applications can have multiple stages. Supervisor stages require membership
in the chosen User Group AND an active team/department supervisor relationship to the
applicant. Other stages resolve active group members in the organisation.
Any one eligible assigned reviewer completes a stage. Stages advance strictly
in sequence; only the last stage can approve the request. Group and supervisor
eligibility are rechecked when a reviewer acts.

New leave submissions require an exact configured combination; they do not
silently fall back to generic request workflows. Every stage must resolve an
eligible user other than the applicant. Existing submissions retain their
workflow version. A returned application restarts the first stage of its saved
workflow version. Leave change requests show their current stage and pending
reviewers on `/online/leaves/leave-change-request`.

RequestStates 1, 2 and 3 must be processing/non-final states. States 4, 5 and 6
(Approved, Cancelled, Rejected) must be final. Saving a flow validates these
flags and requires the stage transitions to already exist in
RequestTypesAllowedRequestStateTransitions. Manage them under
Settings → State Transitions (`/admin/settings/states`). Saving a workflow does
not grant access to that page: reading or modifying transition configuration
requires membership in the Developer user group; administrator privileges alone
do not bypass this requirement. Saving a workflow does
not create or restore allowed transitions. Missing transitions block the action
before status, balance, approval or audit changes are saved. Leave dates and
balances change only on final approval.
