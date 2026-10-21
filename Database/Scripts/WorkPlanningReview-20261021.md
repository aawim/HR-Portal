# Work planning review — 21 October 2026

## Application fixes

- The Plans screen now uses the runtime generator for scheduled plans and the shared assignment generator for manual duty. It no longer replaces an existing daily plan.
- Template/segment/plan/staff lookup operations enforce tenant boundaries. Scheduling writes require Schedule.Attach. Global template changes require the Developer user group.
- Segment edits save the selected event type, mandatory flag, sequence, offsets and grace correctly. Segment listing is restricted to the requested template.
- Unused generated future plans are invalidated after template/segment edits; manual, finalized and clock-associated snapshots are preserved.
- Manual previews use actual template hours. Manual flags propagate to dated plan segments. Same-template official hours and separate manual duty can coexist.
- Bulk department assignments create each employee's own plan, assignment, owners and clock segments, with individual skip/failure results. They do not attach several employees to one employee's plan.
- Manual View, Edit and cancellation are connected. Edit updates the existing assignment instead of creating a new one. Date/template changes use cancellation and a new assignment. Clock-associated/finalized plans reject these changes.
- Plan generation errors are shown to the user.

## Existing database findings

The read-only check found these pre-existing records:

| Finding | Count | Action |
| --- | ---: | --- |
| Active assignments missing assignment segments | 42 repaired | User approved the matching-segment repair. Restored 168 dated segments; operation log 246571. |
| Active plans with no valid assignments | 8 | Historical plan IDs 55, 81, 92, 96, 99, 101, 110, 140. Review separately; plan 110 already has attendance resolutions. |
| Active template with no segments | 1 | Template 5, Remote Work. Complete its segments before using it. |
| Owners differing from original plan staff | 3 | All three have OwnershipType Transfer. They are recorded transfers and were not treated as erroneous bulk assignments. |
| Active segments with invalid basic timing | 0 | No nonpositive sequence/duration or negative offset found. |

The repaired assignments include today's older official assignments. Attendance matching uses plan segments, while the assignment resolver needs assignment segments. Both structures now have matching dated segments for these 42 assignments.

`20261021_WorkPlanningSegmentRepair.sql` defaults to preview mode and copies existing dated plan segment snapshots only. It does not recompute historical schedules or alter clock logs, resolutions, owners or leave records. The approved execution ran inside a transaction and compared the full tenant clock-log and resolution rows before committing: unchanged. Readback verified 168 matching segments with zero timing/flag mismatches and zero remaining active assignments without segments in tenant 3. The eight segment-only plans need a separate decision because creating assignments from current templates could change historical attendance expectations.

## Tests

The isolated regression suite includes WorkPlanningLifecycleTests for the full template → segment → scheduled/manual plan path, tenant denial, sequence validation, snapshot preservation, bulk assignment, rule updates, cancellation and form edit wiring. ScheduleFlowTests cover normal, fixed and rotating schedules, clock matching, overnight duties, duplicate prevention and hour overlap.

Historical clock logs, resolutions and existing plan snapshots were preserved. The only historical repair was the explicitly approved insertion of 168 missing assignment segments, recorded under operation log 246571.
