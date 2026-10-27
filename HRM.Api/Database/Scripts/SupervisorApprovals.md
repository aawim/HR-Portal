# Supervisor assignment and removal approvals

Canonical UI: Admin → HR → Supervisors Management (`/admin/hr/supervisors/management`). Request history/actions are available there and at `/online/supervisor-requests`; the workspace shows pending requests.

Both addition and removal are proposals. Neither changes OrganisationStructureSupervisors or TeamStaffs before the final configured approval. Rejection/cancellation preserve the current assignment. Approved removal clears supervisor authority while retaining team membership/history. The previous direct supervisor setter refuses writes.

Configure **Supervisor Assignment Request** and **Supervisor Removal Request** under Settings → Configurations → Request Approval Flows for each organisation and relevant Job Type. Routing uses the proposed/current supervisor's employment Job Type. Configure allowed state transitions through the Developer-only transition editor. Missing configuration blocks submission/action and notifies responsible administrators using the existing configuration alert path; it does not choose an approver automatically.

Stages use existing User Groups, ordered approvals, live approver eligibility and transition checks. Subjects cannot approve their own supervisor change; ordinary Admin permission does not bypass assigned approval. The submitting officer or subject can cancel through the allowed transition. Submission uses existing department/team supervisor-management permissions. Only Super Administrators can select another organisation; other staff are limited server-side to their current organisation.

Migration: `20261026034611_SupervisorChangeApprovals`. New physical table: `SV_SupervisorChangeRequests`. Columns: RequestId (PK/FK), OrganisationId, IndividualId, JobId, TargetKind, TargetId, TargetName, IsAssignment, IsOpen, OriginalVersion, Reason, CreatedByUserId, CreatedAtUtc, AppliedAtUtc. A filtered unique index allows only one open change for a person/department-or-team. The target type is checked; actual target ownership and eligibility are revalidated at every approval. Snapshot fingerprints detect stale underlying assignments.

Existing WF_ workflow instances/stages/actions retain approval history. Request notifications reach the subject, eligible approvers and submitting officer; final assignment changes also use existing staff activity notifications. Pending work-handover supervisor routing is resynchronised only when the change is applied.

No existing supervisor assignments are converted or removed by this migration. Configuration is tenant-controlled; the migration installs request types and numbering without inventing approval roles or granting permissions.
