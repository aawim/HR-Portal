# Approval configuration alerts

Missing, ambiguous or unusable approval flows return a staff-friendly response without configuration details. The failed submission is rolled back before an alert is saved.

Alerts use the existing `StaffNotifications` table and notification bell. No schema migration is required.

Recipients are active users in the affected organisation's HR Staff/Head User Groups, applicable Developer User Group members or Developer permission holders, and users with effective `Super.Admin.Manage`. HR notifications stay within the source organisation; global Developer membership and Super Admin permission allow support alerts across organisations. Permission-based recipients use the existing effective permission service, including organisation limits and portal grants.

Each recipient receives at most one alert per organisation/Job Type/Request Type combination in 24 hours. The alert identifies the organisation, Job Type, Request Type and configuration issue, without identifying the applicant. Its link selects the relevant flow only when the recipient is already in that organisation; it never switches tenant context.

Delivery failures are logged and do not expose internal errors or change the staff response. No leave dates, balances or request records are committed when startup fails.
