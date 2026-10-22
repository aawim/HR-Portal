# Staff notifications

Install the rerunnable `20261022_StaffNotifications.sql` before running the updated application. EF models and DbContext include the new table.

`SN_StaffNotifications` columns: `StaffNotificationId`, `UserId`, `IndividualId`, `OrganisationId`, `Kind`, `Message`, `Url`, `CreatedAtUtc`, `ReadAtUtc`.

The header bell combines request notifications and staff activity, refreshing every three seconds. It uses the authenticated account and selected tenant, and supports opening an item, marking one read, and marking all read. Account ownership and tenant scope are checked again on each read/write.

Successful EF saves record staff notifications for clock events, employment state/details, leave entitlements and balances, official/duty schedule attachments, manually assigned duties, team membership and supervisor responsibilities, and position assignments. Attendance requests created on another staff member's behalf notify that staff member. Pure reads and automatic template-based work-plan generation do not create activity alerts.

Request changes notify the applicant and linked leave, job and attendance subjects. Processing actions can notify even when the state remains unchanged. Work handover and eligible approver notifications continue through the existing request inbox.

Duty transfers and swaps notify both participating staff to accept independently. Missing pending acceptance reminders are reconciled; a filtered unique index prevents duplicate acceptance reminders. Staff who already accepted, closed requests, and duties which have started no longer display actionable acceptance reminders. Opening an approval stage queues a notification for its eligible current approvers. Acceptance, readiness for supervisor approval, decline, cancellation, rejection, approval and intermediate approval votes have distinct messages. Creators receive outcome updates when submitting on someone else's behalf.

Failed submissions display an immediate error and do not send another employee a notification for a nonexistent request. Changes and their notifications commit together; historical activity is not fabricated. Updates made outside EF must explicitly enqueue notifications in the same transaction.

Tests cover bell delivery, ownership, disabled accounts, tenant isolation, read tracking, repeated saves, duty reminder reconciliation and action/outcome messages.
