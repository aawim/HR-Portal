# Work planning flow

1. **Templates** define working hours and attendance rules. A template belongs to one tenant or is explicitly global. Global changes require the Developer user group.
2. **Segments** define ordered events relative to the template start: check-in, work, breaks, check-out. Sequence numbers must be unique, durations positive, and offsets/grace nonnegative. Saving an edit updates type, mandatory flag, attendance flags and timing.
3. **Staff Schedule** assigns normal Sunday–Thursday hours, fixed weekdays, or a repeating rotation. Extra duty is separate from official hours.
4. **Plans** use that assignment configuration to generate or return one valid daily plan per staff job. Official and duty assignments and their dated segments share the daily plan. Generating again does not replace it.
5. **Manual assignments** add one-day duty using an available tenant/global template. They do not require a recurring template attachment. A manual duty may use the same template as official hours without suppressing the official schedule. Overlapping duplicates with the same template and purpose are rejected.
6. **Department assignments** generate a separate daily plan and segments for each selected employee. The requested start shifts all template offsets. The end must match the template's configured duration. Results show individual failures/skips, including ineligible staff.
7. **Clock matching** uses dated check-in/out boundaries, including separate duty events and overnight checkout. Mandatory segments determine completeness; overlapping hours are counted once.

## Editing and history

- Template or segment changes invalidate only unused, generated current/future plans so they can rebuild. Manual, finalized, completed and clock-associated plans retain their snapshots.
- A template still used by a recurring or rotating assignment cannot be archived. End or replace those assignments first.
- Manual **View** shows details. **Edit** changes the title, description, priority and clock requirements on the existing assignment and segments. Changing date/template requires cancelling and adding a new assignment.
- Manual **Remove** records a cancellation reason and disables only that duty, its owners and matching segments. Official assignments remain active.
- Manual changes/cancellation are rejected once the plan has clock events or is finalized. Historical corrections use the attendance correction process.
- Scheduling writes require `Schedule.Attach` and access to the current tenant. Template, plan and staff lookup results are tenant scoped. A global template is reusable across tenants; a private template is not.

## Verification

`Tests/AttendanceRefresh/WorkPlanningLifecycleTests.cs` exercises template/segment tenant boundaries, persisted segment edits and sequence validation, future plan refresh, preserved used/finalized plans, repeated generation, manual extra duty and preview timing, same-template official plus duty, bulk per-employee plans, manual edits/cancellation and permission revocation.

`ScheduleFlowTests.cs` covers concurrent schedules, rotation work/rest days, required clock events, overnight checkout, form saves, overlapping hours and access checks. All tests use isolated in-memory data; they do not change production attendance.

These flow fixes do not add database columns. Rotation columns are documented separately in `RotatingStaffSchedules.md`.
