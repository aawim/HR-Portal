# Shared schedule assignments

## Configuration

Open **Settings → Work Planning → Schedule assignments** (`/admin/settings/workplaning/schedule-assignments`). This is the only editor for shared schedule attachments. Organisation, Structure and Group tabs use existing templates; each has Official (default) and additional Duty purposes, effective periods and Standard, Weekly and Rotation patterns. Rotations reuse template IDs and 0 means a rest day. Structure choices are restricted to approved state 3 in the selected tenant. Group schedules use staff groups (`Groups`/`GroupStaffs`), not permission User Groups.

The selected tenant and `Schedule.Attach` permission are checked for every read/write in this configuration, including super administrators selecting an organisation. No live policy is seeded. Staff-specific schedules remain overrides in the workspace; inherited rows identify their source and link to the shared editor. Inherited rows cannot be ended through the individual attachment action. The group detail Schedules tab displays the new attachments and redirects Manage to this editor.

## Resolution

Official default precedence: staff override, group, nearest approved structure, its approved ancestors, organisation. Group membership dates and structure hierarchy dates are respected. Higher group priority wins; overlapping official group rules must use different priorities. A higher-priority rest day remains a rest day; lower scopes do not fill it. Duty attachments from all applicable scopes are additive, deduplicated by template with the more specific attachment winning. Multiple distinct duty templates are supported. Legacy staff templates continue as staff defaults; legacy GroupSchedules and OrganisationStructureSchedules are preserved without silently applying unknown legacy data.

The same resolver drives generation, manual generation validation, official-default prerequisites, attendance eligibility, rest days, staff schedule summaries and template-derived weekly hours. Scope changes invalidate unused generated plans for affected jobs so the next generation uses the new configuration. Plans with recorded attendance, manual plans and finalized plans are preserved. Organisation planning must use the existing template-based work-planning provider; legacy shift planning remains a separate provider.

Duty requires official default coverage across its effective dates, including scope defaults. Default attachments can cover roster rest days, so weekend duty remains possible. Saving and ending scope assignments validates existing affected duties, including dated duties. Ending a higher scope resumes the lower-priority official default. Employee attendance still requires approved employment/position and an active schedule. Existing physical punches and leave balances are never changed by scope configuration.

## History and notifications

New periods start today or later (Maldives time). Assignments can be edited from today or a future date. Editing a started assignment retains its earlier dates as a separate historical period. Disabled assignments remain disabled when edited; enabling is an explicit action. End accepts yesterday or later as the last scheduled day. Ending before a future start withdraws the assignment. Optimistic versions and serializable transactions prevent stale and conflicting writes. Configuration writes create operation logs and affected staff receive in-app notifications.

## Database

New table: `WP_ScopeScheduleAssignments`.

Columns: `Id`, `OrganisationId`, nullable `OrganisationStructureId`, nullable `GroupId`, `WorkTemplateId`, `SchedulePurpose`, `ScheduleMode`, `WorkingDaysMask`, `RotationAnchorDate`, `RotationTemplateIds`, `EffectiveFrom`, `EffectiveTo`, `Priority`, `IsActive`, `Version`, `OperationLogId`.

Both target IDs null means organisation scope; one is set for Structure or Group. Foreign keys and checks protect targets, purposes, modes, dates, weekday masks and priorities. Tenant ownership is enforced in the service/resolver. No legacy table is renamed or replaced.

Deploy the idempotent script `Database/Scripts/20261025_ScopeScheduleAssignments.sql` or additive migration `20261025120300_ScopeScheduleAssignments`. The EF model and snapshot include the new table. Rollback preserves configuration history instead of dropping the table.

## Organisation management entry point

`/admin/settings/org/manage/{organisationId}/schedules` embeds the same editor restricted to organisation scope. It supports add, edit, enable, disable, end and status filtering. Super Admin can manage the selected organisation without changing session context; other users are restricted to the current organisation and need `Schedule.Attach`. Disabling a started assignment splits off its earlier enabled period before disabling today/future coverage. Enabling checks overlaps, template availability and duty prerequisites. Ended periods cannot be enabled or edited; add a new period instead. These actions reuse `IsActive`, version and operation logs; no new database columns are required.
