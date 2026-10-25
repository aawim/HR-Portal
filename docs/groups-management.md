# Operational groups management

Canonical editor: HR → Groups → Groups Management, /admin/hr/groups/management. Groups and Group types are two tabs in this one editor. Existing /admin/hr/manage-groups, /admin/hr/manage-group-types and category group/group-type URLs redirect here. Organisation settings → Groups links here with its organisation selected. Budget placeholder links also point here.

Group names, types and approved organisation structures are maintained by GroupsManagementService. Group types and structures must belong to the selected organisation; structures must be approved (state 3). Current organisation is enforced for non-Super Admin users. Super Admin may select an organisation. Writes require existing Admin, Organisation.Edit, or UserOrganisations.Edit permissions (or Super Admin), and are audited. Existing group updates reject stale operation-log versions. Linked staff, schedule or leave history prevents deletion. No new tables or permission grants are introduced.

The central View panel retains group staff/history, shared schedule links and leave set details. Leave-set assignment now uses the same tenant-checked service and real operation-log ID. Existing standalone group/type placeholder save handlers and organisation inline create form were removed.

Operational Groups are different from access-control User Groups. Access-control User Groups and permission membership remain under Settings → Roles & Permissions; they are not merged with HR groups.
