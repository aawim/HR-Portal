# Admin portal access

Run `20261022_AdminPortalAccess.sql` before deploying the application change.
The script is transactional and idempotent. It preserves existing explicit
Admin-context direct/group assignments, excluding organisation-default grants.

Also run `20261022_AdminAccessGrantWorkflow.sql` to register **Admin access
grant** and its request storage. Configure its tenant/JobType approval stages
under **Settings → Configurations → Request Approval Flows**. No approvers are guessed;
submission requires a configured flow with independent active approvers who
already have Admin portal access.

New accounts default to employee-only access. In **Admin Settings → User →
Admin Portal Access**, an administrator selects an account, User Groups,
direct permissions and a reason, then submits for approval. The selected
organisation groups/direct permissions replace existing ones only at final
approval. Global memberships are preserved. The recipient cannot approve
their own access, and an administrator cannot request their own access change.
Super Admin (`Super.Admin.Manage`) can select an organisation. Other users are
restricted to their current organisation, including server-side validation.
Only Super Admin can request Super Admin privileges. Normal account editing
and group assignment do not enable portal access. Revocation remains an
immediate audited administrator action; a pending grant must first be closed.

The page lists requests, configured stage actions, requested assignments and
history. Approvers, recipient and submitter receive request notifications.
Recipient notifications open the employee-readable request view.

Grants apply to the account's UserOrganisationId. Reassignment requires a new
grant. Revocation filters Admin-context permissions on the next authorization
check and blocks the portal on the next navigation. Each change records the
actor, target and reason in OperationLogs through USER_ROLE_UPDATE.

## Database changes

Users:
- AdminPortalAccessEnabled: bit, not null, default 0.
- AdminPortalAccessOrganisationId: nullable int, the UserOrganisationId at grant.

Existing OperationLogId, LastUpdatedByUserId and LastUpdatedDate record the
latest update. No new permission is implicitly assigned by this process.

AA_AdminAccessGrantRequests: RequestId (PK/FK), UserId, OrganisationId, JobId,
CreatedByUserId, GroupIdsJson, PermissionIdsJson, Reason, CreatedAtUtc, IsOpen,
RowVersion. A filtered unique index permits one open request per account.
Requests and existing Wf tables store the request state, ordered stages,
assignments, decisions and audit history. Every state change requires a route
in RequestTypesAllowedRequestStateTransitions.
