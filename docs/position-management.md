# Position management and HR allowance assessment

Canonical pages:

- `/admin/settings/positions/manage-positions`: create, edit, activate/deactivate, search and filter the shared position catalogue.
- `/admin/settings/positions/overview`: current staff counts and expandable staff lists across the schemes.
- `/admin/settings/positions/position-allowances`: required position allowances. The payroll configuration page links here instead of providing another editor.

The existing `Positions` table remains the single position catalogue. `PositionFrameworkID` references the existing `PF_Frameworks` values. The current database defines `LEGACY` (custom/existing positions) and `NEW_PAY` (government Job Matrix). Their IDs and database names are retained. `PositionMatrixID` links government positions to existing `PF_PositionMatrix` entries; custom positions leave it null.

The UI uses the actual framework names and reads existing government definitions from `PF_PositionMatrix`, `PF_JobMatrix` and `PF_JobTitles`. These existing tables are excluded from EF table-creation migrations. It does not invent government grades, pay scales or matrix records. The current database has no `PF_PositionMatrix` entries, so government position creation is unavailable until a valid current-tenant matrix entry exists. This change implements the shared position catalogue and existing matrix selection, not government pay-matrix definition maintenance.

Current-tenant `Positions.Configure` grants position configuration. `Position.Details`/`Staff.Positions.Manage` allow catalogue reads. Only actual superadministrators can edit global positions. A tenant officer cannot edit another organisation's positions, global positions or unscoped shared legacy records. Government matrix selections must belong to the current organisation and be currently effective. A position with employment history cannot change its framework, matrix link or global scope; create a new position instead. Renaming/deactivation preserves staff assignments and their approval history. There is no destructive delete action.

Position allowance rules retain the existing `StaffFinance.Configure` permission. Staff-requested allowance pricing uses the separate `StaffFinance.Assess` permission. Both must be allowed by organisation permission limits and assigned through the existing permission/User Group process; this migration does not grant staff or organisations access automatically.

Migration `20261029151054_PositionFrameworksAndAllowanceAssessments` conditionally adds the two position link columns only if absent and preserves existing values. It creates `AL_AllowanceAssessments`, adds the two permissions and installs an append-only assessment trigger. It does not rename or replace `Positions`, reclassify staff or overwrite existing payroll amounts. Existing approved allowances remain unchanged; pending allowance requests require an independent HR assessment before final approval.
