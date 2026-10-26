# Job Type configuration tabs

`/admin/settings/config/job-types` has OT Config, Leave Config and Leave Set Config tabs. The selected tab is retained in the `tab` query parameter (`ot`, `leave`, `leave-sets`). Existing Job Type catalogue editing and the canonical approval-flow link remain available.

OT Config edits the selected Job Type's current-tenant overtime policy. Leave Config reuses the accrual editor scoped to that Job Type. Opening either tab does not post grants or alter staff balances.

Leave Set Management lives under Settings → Leaves at `/admin/settings/leaves/manage-leave-sets`. It is the single CRUD editor for set metadata and leave-type membership. The former HR leave-set route redirects here; HR no longer contains a leave-set CRUD entry. Set contents use existing LeaveSetLeaveTypes rows; removals mark links invalid and retain history. Current-tenant and explicitly global leave types are available. Public/global sets require Super Administrator authority to edit.

Leave Set Config assigns one or more of the current organisation's sets to the selected active Job Type. The panel lists each set's current leave types. Empty sets cannot be assigned, and the final leave type cannot be removed from a set while it has an active Job Type assignment. Removing a Job Type association deactivates its row, preserving audit/history. Sets with type, group or Job Type history cannot be deleted.

Migration `20261026050306_JobTypeLeaveSetAssignments` adds `LS_JobTypeLeaveSets`: Id, OrganisationId, JobTypeId, LeaveSetId, IsActive, OperationLogId. There is a unique organisation/Job Type/set index and restrictive foreign keys. Existing LeaveSets, LeaveSetLeaveTypes and GroupLeaveSets are preserved. No default assignments are guessed and existing staff balances are not rewritten.

LeaveSetConfigurationService checks current-tenant permission (`Admin` or `Leave.Configuration.Manage`, or Super Administrator), ownership, reference availability, duplicates and stale writes. Membership and Job Type assignment changes are serializable and audited. Existing configuration and staff-entitlement processes remain responsible for granting days; assigning a set is not a grant of a leave balance.
