# Organisation settings sections

The organisation settings menu follows the User Management layout:

| Section | URL | Purpose |
| --- | --- | --- |
| Manage | `/admin/settings/org/manage` | Existing organisation editor, including details, addresses, contacts, structure, staff and teams |
| Manage type | `/admin/settings/org/types` | OrganisationStructureTypes catalogue |
| Calendar | `/admin/settings/org/calendar` | Assigned calendars and effective periods; assign an available calendar |
| Locations | `/admin/settings/org/locations` | Linked organisation locations; open the existing address editor |
| Location Types | `/admin/settings/org/location-types` | LocationTypes catalogue |
| Organisation Types | `/admin/settings/org/organisation-types` | OrganisationTypes catalogue |
| Structure Request Types | `/admin/settings/org/structure-request-types` | OrganisationStructureRequestTypes catalogue for structure changes |
| Organisation Permissions | `/admin/settings/org/permissions` | Existing permission limits management |

The separate Teams and supervisors menu entry is removed. Teams remain in
Manage and old direct team links still work. Existing `/org/organisations`
URLs are aliases of Manage. New organisation record URLs use
`/admin/settings/org/manage/{id}/{section}`.

The three reference type tables are global and have no tenant ownership
column. Their creation/rename actions therefore require Super Admin and are
audited. Organisation calendar/location/request data is scoped to the current
organisation; Super Admin may select another organisation. Calendar assignment
rejects overlapping assignments and calendars private to another tenant.

No database schema changes or initial data changes are required.
