# Organisation permission limits

Manage permissions under **Admin Settings → Organisation → Organisation
Permissions**, or `/admin/settings/organisation-permissions`.

The existing `UserOrganisationRoles` table provides the tenant permission
ceiling. Its active RoleID values limit direct UserRoles and UserGroupRoles,
including global groups, when effective permissions are resolved. No new
tables or columns are required. Existing organisation-default grants remain
applicable; Admin-context permissions still require an explicit approved
Admin portal grant. Removing a limit does not delete user/group assignments,
but those assignments no longer provide the removed permission.

Admin access requests reject unavailable direct permissions both at submission
and final approval. The UI marks unavailable permissions, and only allowed
group permissions appear checked. Account permission editing applies the same
direct-permission validation.

Explicit platform administrators (`Super.Admin.Manage` assigned directly or
through a valid group) can select organisations and enable permissions.
Organisation administrators use their current organisation and cannot expand
the ceiling beyond their own effective permissions. Viewing and saving both
enforce organisation scope. Platform authority is checked independently for
this management operation so a tenant permission removal cannot prevent its
restoration. The same guarded view is available at
`/online/organisation-permissions` for recovery if portal access is blocked.
This does not grant access to other resources.

The page supports search, area/status filters, grouped lists, bulk changes to
displayed permissions, change review, a reason, reset and optimistic conflict
checks. Saving records additions/removals in OperationLogs and invalidates all
affected user contexts. Tests cover direct/group limits, disabled limits,
tenant isolation, platform management, stale edits, audit and revocation.
