# Duty roster administration tabs

## Location

Open **Admin → Work planning → Duty roster** (`/admin/work-planning/duty-roster`). The page hosts four tabs and the selected tab is kept in the URL:

- `roster` – dated duties with transfers, swaps and duty requests (`/admin/work-planning/duty-roster?tab=roster`).
- `schedule` – shared schedule assignments for the organisation, approved structures and staff groups (`?tab=schedule`). The former Settings → Work planning → Schedule assignments entry now opens this tab. `scope` and `groupId` query values still preselect a group.
- `manual` – the manual work assignment form for one staff member and one date (`?tab=manual`), also reachable at `/admin/work-planning/manual-assignment`.
- `create` – create a dated duty roster for several staff members over a date range (`?tab=create`).

The employee roster remains available at `/online/duty-roster`.

## Create a roster

1. Select an approved department; the tab lists its active staff with a job.
2. Select the staff members who receive the duty.
3. Choose an active duty template effective on the start date, the date range, the weekdays and an assignment title.
4. Optionally change the check-in/check-out requirements and add remarks.
5. Review the preview (staff × selected days) and choose **Create roster**.

Each staff member and day becomes a manual work assignment on that staff member's daily plan, using the existing manual assignment generation. Staff need official hours for the duty dates; missing schedules, invalid positions and duplicate duty on the same template and day are skipped and reported instead of aborting the batch. A single run is limited to 400 assignments, and the batch can be stopped between items.

## Permissions

Every read and write uses the current authorised organisation and the existing Work Planning access checks. The roster builder adds no tenant or cross-organisation bypass.
