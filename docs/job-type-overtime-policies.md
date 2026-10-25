# Job Type overtime policies

Settings → Configurations → Job Type Configuration → select a Job Type → Overtime (OT).

Each organisation configures its own policy for each shared Job Type. Allow overtime, set a required weekly OT cap, and optionally set a daily OT cap. Decimal hours accept up to two decimal places (1.50 = 90 minutes). Disabling OT saves zero weekly hours and no daily limit. Missing policy means no OT eligibility has been configured; it does not change attendance, schedules, or raw worked-hour totals.

Permission: Attendance.WorkingHours.Configure, with the existing Admin/Super Administrator handling. Every read and write checks current organisation access. Stale tenant forms and concurrent versions are rejected. Saves produce operation logs. Unique organisation/Job Type index and database checks enforce policy consistency. Limits are for overtime in addition to normal template-derived hours; they are not total weekly working-hour targets.

This change provides configuration. The existing attendance calculations still report potential overtime; there is no overtime claim/payroll approval flow in this change. Future OT requests must enforce these policies against cumulative daily/weekly OT before approval. Saving a policy does not authorise payment or truncate attendance hours.

Table: OT_JobTypeOvertimeRules. Columns: Id, OrganisationId, JobTypeId, IsAllowed, MaximumWeeklyHours (decimal 5,2), MaximumDailyHours (nullable decimal 5,2), Version, OperationLogId.

Deploy migration 20261025120400_JobTypeOvertimeRules before using the editor. Existing policies and staff balances are not seeded or changed. Rollback preserves the table and audit history.
