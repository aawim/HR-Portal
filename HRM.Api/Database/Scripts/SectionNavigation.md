# Section URLs

Application category menus now update the browser URL and restore their
selection from route parameters on direct navigation and browser back/forward.
The existing services continue to check permissions and organisation scope.

Examples:

- `/admin/settings/org/organisations`
- `/admin/settings/org/permissions`
- `/admin/settings/org/teams`
- `/admin/settings/org/teams/departments`
- `/admin/settings/org/organisations/3/structure`
- `/admin/settings/positions/overview`
- `/admin/settings/user/admin-portal-access`
- `/admin/settings/workplaning/templates`
- `/admin/hr/individuals/search`
- `/admin/hr/staff/staff-onboarding`
- `/admin/attendance/manage-attendance/team-attendance/summary`
- `/admin/my/workspace/calendar`
- `/admin/my/workspace/staff/918/attendance`
- `/online/attendance/my-attendance/break-late`

The same pattern applies to the Payroll and Budget category menus. Employee
leave sections already had routes and retain their existing URLs. Attendance
tab navigation preserves unrelated query parameters and creates history
entries; older `?tab=` links remain supported. Workspace query links using
`entityType` and `entityId` also remain supported. Each workspace entity is
resolved through the existing scoped recent-search service before display.

Older standalone URLs, including `/admin/settings/org/team`, continue to
work. The organisation settings entry now displays the category menu so users
can move between organisation details, teams and permission management.
The old organisation search wildcard route was narrowed to prevent it from
intercepting `/admin/hr/individuals/search` and `/admin/hr/staff/search`.

No database migration is required.
