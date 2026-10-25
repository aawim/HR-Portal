# Concurrent official and duty schedules

Apply `20261021_ConcurrentStaffSchedules.sql` before deploying the updated application.
Apply `20261021_DutyOfficialHours.sql` afterward to retain schedule purpose in generated assignments and segments.
Existing generated history retains its original classification; this migration does not reclassify historical assignments or attendance.

`JobWorkTemplateAssignments.SchedulePurpose` is `Official` or `Duty`. Existing rows default to `Official`; no historical assignment dates are changed by the migration.

In the staff workspace Schedule tab, each purpose has its own card, assignment dates, history and removal action. Official working hours are the default schedule. An active default attachment must cover the complete duty date range before a duty can be attached; default rest days do not prevent duty on those dates. One official default and multiple duty templates can be attached concurrently. Assigning a different duty template preserves existing duties. Updating the same duty template replaces only its overlapping dates. Ending default official hours is blocked while recurring or dated duties would lose default coverage; attach a replacement or end those duties first. Saving the same dates and pattern again does not create another assignment. Official and duty schedules can reuse the same template.

The everyday duty flow is **Add duty → select an existing template from Settings → choose one day/date range/ongoing and working pattern → Save schedule**. Hours, segments and clock requirements are defined once in **Admin → Settings → Work Planning → Templates**; the staff assignment does not create or clone templates. Normal date-range duty, selected weekdays and rotating cycles reuse those templates. Saving an unchanged assignment is idempotent. A temporary change preserves and resumes the previous duty outside the changed dates. Ending a schedule uses **Last scheduled day**; the server removal date is the following day.

The current tenant and the existing `Schedule.Attach` role are checked on assignment and removal. Reads are scoped to the current tenant.

The automatic planner generates one daily `WorkPlans` row with separate `WorkAssignments`, `WorkAssignmentSegments` and `WorkPlanSegments` for the official default and every effective duty template, including overlapping hours. The older weekday-based `JobWorkTemplates` path remains available for staff without the newer assignments. Manual assignments can join an existing, unfinalized daily plan. Repeated generation does not duplicate a template on the same day.

Duty inside official hours is recorded separately but contributes no extra regular hours or OT. For official hours 08:00–14:00 and duty 09:00–11:00, the day remains six hours. Duty 13:00–16:00 has one hour within official hours and two hours potentially eligible for an OT request. Clock pairs are formed per template and overlapping worked intervals are merged for the daily total. Official attendance boundaries remain those of the official schedule. Overnight and split periods use interval overlap rather than subtracting aggregate durations.

Check-in requirements remain separate: when the official 08:00 check-in and duty 10:00 check-in both have `IsMandatory` and `RequiresAttendance` enabled, each needs its own clock event resolved to its segment. Duty clock requirements are evaluated separately from official daily attendance. Optional check-ins do not prevent completion. The staff schedule cards list the mandatory check-in times. This uses existing template-segment flags and requires no further schema change.

`AttendanceDailyEvaluationDto.PotentialOvertimeMinutes` is observed work outside official hours; it is separate from approved `OvertimeMinutes`. Scheduled duty outside official hours is available in `PlannedDutyOutsideOfficialMinutes`. Neither field creates an OT approval or payroll entry. The current OT screens remain placeholders.

Changes invalidate unused generated plans so they can be rebuilt. Finalized plans, manual plans, and plans associated with attendance are preserved. A newly attached template can be added to an existing unfinalized plan without deleting recorded attendance.

The same default-schedule prerequisite is enforced for manual duty generation and transfer/swap recipients. The workspace lists all current duties with individual end actions; Add duty opens a new attachment. No table or column changes are required for multiple duty templates.
