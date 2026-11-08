# Staff working patterns

Open **My Workspace → Staff → Schedule → Official working hours → Change**.

Choose a working pattern:

- **Normal staff:** Sunday through Thursday; Friday and Saturday are rest days.
- **Shift – selected weekdays:** select the actual duty weekdays, including Friday/Saturday when applicable.
- **Rotating shifts:** set the date of cycle day 1, add days in sequence, and choose a saved shift or Rest day for each day. The cycle repeats continuously. For example: Morning, Evening, Rest, Rest. A shift can end the next day using its saved template's overnight setting.

The effective date range controls when the assignment applies. The cycle anchor is preserved when a temporary replacement ends and the previous assignment resumes. Extra duty schedules remain separate and may make an otherwise resting day a workday.

Scheduling requires the existing **Schedule.Attach** permission and access to the current tenant. Every selected shift must belong to that tenant or be global. Level/template availability is validated on save and generation. Existing finalized/used attendance plans retain their history.

Run `20261021_RotatingStaffSchedules.sql` before deploying the updated app. It adds these columns to `JobWorkTemplateAssignments`:

| Column | SQL type | Meaning |
| --- | --- | --- |
| ScheduleMode | nvarchar(20), required | Standard, Weekly, or Rotation |
| WorkingDaysMask | int, required | Sunday bit 0 through Saturday bit 6; normal default 31 |
| RotationAnchorDate | date, nullable | Date corresponding to cycle day 1 |
| RotationTemplateIds | nvarchar(2000), nullable | Ordered JSON array of work-template IDs; 0 means rest |

Normal schedules default to Sunday–Thursday. Existing duty attachments still apply throughout their assigned date range. No employee is automatically assigned a new rotation. Configure each shift worker's actual pattern before relying on shift-based absence reporting.

Clock events match scheduled boundaries independently: 08:00 official in, 10:00 duty in, 12:00 duty out, 14:00 official out. A duty checkout does not satisfy the official checkout. In the test clock, 10:00 means 10 AM and 22:00 means 10 PM; the selected date and both time formats are displayed before clocking.
