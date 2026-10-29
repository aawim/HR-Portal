# Work Type management

Open Settings → Configurations → Work Types (`/admin/settings/config/work-types`) for the shared catalogue. Platform administrators maintain it. Open a staff member in Admin → My Workspace → Work types to manage their dated assignments; the selected staff member and current job are used automatically. Staff with `WorkTypes.Manage` manage assignments in their current organisation. Grant this permission through the existing organisation permission limits and User Groups/direct permissions. The previous Job and Work Planning URLs redirects to the catalogue.

Existing physical tables `WorkTypes` and `AssignedWorkTypes` are retained; these are legacy tables reattached by the existing mapping, not newly created replacement tables. Both mappings remain excluded from migrations. No historical assignments are backfilled or guessed. Work type removal uses deactivation and assignment removal retains history.

Apply migration `20261029090000_WorkTypeManagementPermission` to register the permission. It does not grant permissions to any user or organisation. No live migration is applied by this task.

The employee clock button and development clock card require an active work type and a valid assignment to the user's current approved job, in the current organisation, covering today's Maldives date (inclusive). The backend clock endpoint checks the same rule before writing, including custom development clock events. No assignment means no clock access; assign eligible staff before enabling this release.
