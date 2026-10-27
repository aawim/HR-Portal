# Former staff

HR → Staff now owns individual registration and registry search. The former Individuals category redirects to those canonical Staff sections so existing links continue to work.

Former Staff lists people with ended employment or ended approved position assignments and no current employment in the selected organisation. Future termination dates remain current until effective. Legacy approved employment without dated approved position history stays current; pending onboarding is not classified as former employment.

The list searches names, employee numbers, national IDs and passports. It is scoped to the selected organisation, including for superadministrators. HR/organisation administrators, superadministrators or the effective Staff.Details/Admin permission may view it. Normal attendance permissions still apply to attendance reports.

The Staff workspace renders FormerStaffRecord for former employees. It provides read-only personal/identity data, contacts and addresses, employment and position history, position salary records, leave entitlements/history, requests, OT/payroll allocations, work plans, groups/teams and document metadata. Attendance has date-range search and its existing Excel/PDF reports. Information and search-list exports use the same authorised report snapshot as the view. No user account password, token or secret is exported.

HrmTeContext checks changed records before saving or generating notifications. It follows ownership foreign keys to staff/employment/request records and checks original as well as new ownership, preventing reassignment bypasses. Changes to former employee records and ended employment are rejected independently of the caller's permissions; there is no superadministrator override. Shared address/document changes are checked against their employee links. Notifications, unread markers, preferences and chat operations remain operational, since they are not changes to the employment archive.

An in-progress transaction may complete the approval which ends an active employment/position; subsequent transactions cannot edit that ended record. This prevents the write guard from blocking the final workflow/audit save of the termination itself. No new tables or migrations are required.

Verification includes former/current and tenant boundaries, ended-position detection, identity/ownership reassignment protection, attendance additions, profile/job/leave/request edits, deleted employment, superadmin denial, information Excel/PDF exports, and restored SQL Server history/attendance queries and write rejection. The web application and its background workers are not started by these checks.
