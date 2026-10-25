# Working hours and temporary official schedule amendments

## Configuration

Open **Settings → Work Planning → Working Hours** (`/admin/settings/workplaning/working-hours`). This is the single editor for both features. All changes use the current authorised organisation context, including a super administrator's selected organisation.

### Template-derived hours and Job Type policies

Normal required hours and working days come from assigned official templates and dated rosters. Configure the week start, an effective period covering complete weeks, and whether duty contributes to weekly hours. A separate manual weekly target is not required. Each new Job Type appears automatically. Overlapping rule periods are rejected. Existing historical rule periods are retained. No tenant hours are assumed or silently assigned.

The dashboard Worked Hours card shows required, worked and remaining hours, official and duty hours, organisation waivers, approved leave credit, approved exception credit and potential overtime. The selected employee's Attendance tab in My Workspace contains weekly reconciliation, with Excel/PDF export. Access is limited to the employee, their current supervisor or authorised tenant reviewers.

Only completed clock pairs count. Official and duty intervals are merged when duty contributes, so overlap never counts twice. Open shifts do not invent worked time. Overnight intervals are clipped to calendar-week boundaries, including a shift beginning the preceding day. Approved positions and active schedules remain required for official attendance. Required hours are summed from eligible official roster days, including rotating patterns. Days before approved employment, days without an active schedule and roster rest days create no requirement. Approved leave credits the scheduled hours for its dates. Approved early checkout does not reduce it: its missed time must be made up in the same week. Duty can settle make-up only when the rule counts duty toward weekly hours. Overtime figures remain provisional and do not approve pay.

### Temporary daily working hours

Under the same settings page, expand **Temporary daily working hours**. Choose all Job Types or a specific Job Type, From/To dates, hours per working day and a reason. For an 8-hour week over a normal 08:00–14:00 template, each working day becomes 08:00–16:00. A specific Job Type takes precedence over an all-types requirement. Off days remain off; longer template requirements are not reduced. The base template and dated snapshots are unchanged.

Rain is applied after the increase: 08:30–16:00 means 7.5 required hours, with no make-up for the waived half-hour. Approved early checkout remains compulsory and its unworked requirement remains owed in that week. New periods must start today or later; an already-started period cannot be edited through this configuration screen. Future periods can be edited or withdrawn, with concurrency, overlap, tenant and permission checks. Staff receive notifications.

### Temporary official hours

Select **Temporary official hours** and enter a date, original start, later start and reason. Example: original **08:00**, revised **08:30**. The official end remains unchanged. Arrivals at **08:15** or **08:30** are not late against that amended start. The waived 30 minutes reduce the weekly requirement; they are not owed as make-up.

Amendments match the organisation, date and original official start. Duty schedules and other start times are not shifted. Templates, physical clock timestamps and base dated snapshots are preserved: planning, clock resolution, daily evaluation, attendance reports and weekly reconciliation apply the same dated overlay. Personal time-change approvals materialise the effective amended schedule before applying their own approved adjustment. An amendment can be edited or withdrawn today or in the future; past dates and finalized dated plans are protected. Saving or withdrawing sends an in-app notice to active staff in the organisation.

### Weekly exceptions

Staff submit **Weekly hours exception** under Attendance Requests (`/online/attendance/attendance-request/weekly-hours-exception`). Configure **Weekly Hours Exception Request** per tenant and Job Type in the existing Request Approval Flows editor, with the appropriate supervisor User Group/stage, and configure its permitted state transitions. The new type is not given an automatic approval bypass.

Assigned approvers receive notifications and see pending exceptions in My Workspace. The applicant can cancel a processing request but cannot approve their own request. Stage sequence, approval mode, independent current approver eligibility and `RequestTypesAllowedRequestStateTransitions` are enforced by the shared request processor, also used for late check-in and early checkout. Only final Approved exceptions credit weekly hours. Credit is capped at the shortfall so an exception cannot manufacture overtime. Missing workflow configuration uses the existing HR/developer notification mechanism and a friendly employee error.

## Permissions (Roles)

- `Attendance.WorkingHours.Configure`
- `Attendance.Schedule.Amend`
- `Attendance.WorkingHours.Review`

HR Staff/Head User Groups receive the new permissions on installation. Tenant permission ceilings remain enforced. A tenant's catalogue is extended only when equivalent Admin/Attendance.Adjust (or Attendance.View for review) permission already exists; disabled permission entries are preserved. Other tenants can enable the permissions through Organisation Permissions. Super administrators and users with the tenant Admin permission can manage settings. Supervisors without broad review permission can review only their own supervised staff.

## Database

| Table | Columns |
| --- | --- |
| `WH_JobTypeWorkingHourRules` | Id, OrganisationId, JobTypeId, RequiredWeeklyMinutes, WorkingDaysMask, WeekStartsOn, CountDutyHours, EffectiveFrom, EffectiveTo, Version, OperationLogId |
| `WH_OrganisationWorkingHourRequirements` | Id, OrganisationId, JobTypeId (nullable/all), FromDate, ToDate, RequiredDailyMinutes, Reason, IsActive, Version, OperationLogId |
| `SA_OrganisationScheduleAmendments` | Id, OrganisationId, WorkDate, OriginalStart, RevisedStart, Reason, IsActive, Version, OperationLogId |
| `WH_WeeklyHourExceptionRequests` | RequestId, OrganisationId, JobId, IndividualId, WeekStart, ExcusedMinutes, Reason, CreatedAtUtc |

Request state and approval history use existing `Requests`, `WF_` and notification tables. Configuration changes use existing operation logs and optimistic concurrency; overlapping saves are checked within serializable transactions.

Deployment: run `Database/Scripts/20261025_WorkingHoursAndScheduleAmendments.sql`, or the additive migration `20261025120000_WorkingHoursAndScheduleAmendments`. The script is idempotent and was applied to the current local development database. No tenant rule, amendment or exception was created. Reauthenticate if cached permissions need refreshing.

Migration `20261025120100_WorkingHoursRuntimeModelBaseline` updates EF metadata after the prior unused-model cleanup. Its Up/Down methods are deliberately empty. The snapshot and designer describe the current runtime model; legacy physical tables and records remain intact. No scaffolded drop or unrelated schema alteration is executed.

The application build and CSS build completed successfully. EF reports no pending model changes against the updated snapshot. The application reported zero compiler warnings/errors. Regression tests are included in `Tests/AttendanceRefresh/WorkingHoursTests.cs`; run the focused suite with `--working-hours-only` or the full AttendanceRefresh suite.

Deploy `Database/Scripts/20261025_TemplateDerivedWorkingHours.sql` or migration `20261025120200_TemplateDerivedWorkingHours` after the initial working-hours migrations. Legacy `RequiredWeeklyMinutes` is retained, nullable and ignored by the calculation; existing data is preserved. `WorkingDaysMask` remains a legacy field and does not override the roster. No live hours or date periods are seeded.

Validation: the full AttendanceRefresh suite passed all 27 groups. The expanded focused working-hours suite also passed, covering 8-hour daily increases, rain composition, template fallback without a policy, tenant/Job Type/date isolation, withdrawal, overlap prevention, permissions and stale edits. Application build completed with zero warnings/errors; CSS build passed.
