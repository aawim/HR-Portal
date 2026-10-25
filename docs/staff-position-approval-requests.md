# Staff position approval requests

All actions in Workspace → Staff → Position submit a request: Assign, Change, Edit dates, End / remove. Approved employment records are updated only by the final workflow approval callback. Rejection and cancellation keep approved assignments unchanged; removal ends the dated assignment and retains history. Initial onboarding continues through its existing approval workflow.

Configure the tenant and Job Type flows in Settings → Configurations → Request Approval Flows using the new Request Types: Staff Position Assignment Request, Staff Position Change Request, Staff Position Dates Request, Staff Position Removal Request. No approvers or transitions are guessed or automatically granted. Missing/invalid workflow configuration blocks submission, shows a friendly message, and notifies HR, Super Admin and Developer through the existing configuration alert mechanism.

Staff.Positions.Manage or existing Admin/Super Admin access allows submission; the existing tenant permission ceiling still applies. Assign this permission through Organisation Permissions and User Groups as appropriate. Approval/rejection always require a current configured independent stage assignment, including for administrators. Staff cannot approve their own position request. Staff and the submitting HR officer can cancel a pending request, subject to allowed state transitions.

Requests appear in the staff Position tab and the workspace's pending position requests card. A standalone authenticated page /online/staff-position-requests shows the current organisation's own, submitted, authorised managed, or assigned requests. Notifications open the same request list. Current stage and processing/final status are preserved, with workflow action history.

Submission and approval use serializable transactions. Before each approval the service rechecks tenant, approved employment, position eligibility, dates, overlapping assignments, and an immutable snapshot version of the original approved assignment. Only one nonfinal position request per employment can be submitted. Stale proposals must be cancelled/rejected and resubmitted. Prior assignments and linked salary/request records are retained.

New table: SP_StaffPositionChangeRequests. Columns: RequestId (PK/FK Requests), OrganisationId, JobId, IndividualId, PositionId, ChangeKind, FromDate, ToDate, OriginalJson (approved assignment snapshot), Reason, CreatedByUserId, CreatedAtUtc. Workflow stage instances, decisions, transitions, and notifications use existing WF_ and notification tables.

Deploy additive migration 20261025120500_StaffPositionChangeRequests or Database/Scripts/20261025_StaffPositionChangeRequests.sql. The migration also installs four system Request Types with numbering sequences and the submission permission. No existing staff assignments or approval flow definitions are changed.
