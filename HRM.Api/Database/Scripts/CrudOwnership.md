# Canonical CRUD ownership

Use these editors for the consolidated operations. Legacy UI entry points redirect here; legacy service methods delegate to the same implementation.

| Operation | Editor | Write implementation |
| --- | --- | --- |
| Organisation details and contacts | `/admin/settings/org/manage` | `OrganisationSettingsService` |
| Organisation addresses | `/admin/settings/org/manage/{id}/address` | `OrganisationSettingsService` authorisation, `BusinessEntityAddressWriter` persistence |
| Personal addresses | Existing personal address editor | `AddressService` ownership checks, `BusinessEntityAddressWriter` persistence |
| User account details and state | `/admin/settings/user/account?userId={id}` | `UserSettingsService.SaveAccountAsync` |
| User Groups and direct permission assignments | Existing user access editor or approved Admin Access Grant | `UserAccessAssignmentWriter` persistence |
| Clock events | Existing time clock | `AttendanceLogService.AddClockEventAsync` |
| Audit log creation | Existing audit callers | `AuditLogger.CreateNewLogAsync` |

Organisation creation, the old organisation registry search, and embedded legacy organisation/address forms redirect to Organisation management. Profile links to Manage Account when the current user has permission. Compatibility account writers reject changes to login and employee identity and use the canonical account service.

Business workflows retain their own authorisation, approval stages, transactions and audit records. Sharing permission persistence does not bypass Admin Access Grant approval or enable portal access. Organisation permission limits and owner checks remain enforced.

No database schema changes are required.
