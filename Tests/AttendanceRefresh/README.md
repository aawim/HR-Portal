# Attendance refresh regression tests

User Group coverage verifies a Developer permission holder can create groups,
assign permissions they hold, and view them in Roles settings. It exercises real
audit creation without an active job and verifies UserOrganisation IDs are used
instead of organisation business IDs. Membership grants/revocations are checked
against current data even with a stale user context. Unheld Admin permissions
cannot be granted through the group editor.

Request-transition tests exercise the shared type/from/to guard and real request
state changes. They check missing or removed transitions block writes, other
Request Types do not grant access, final states stay closed, rejected and
cancelled requests follow exact allowed transitions, blocked cancellation does
not refund or audit, allowed cancellation refunds once, and administrators can
add/remove transitions through the actual settings page. Workflow saves must
not recreate transitions removed by an administrator.

Workspace leave-change tests render the actual pending card with controlled
service responses. They verify early returns and extensions, own pending
requests, deduplication, final-state exclusion, return-date display, approval
dispatch through ExecuteActionAsync, removal after approval, failure messaging,
and mounting the card in WorkspaceContentPage. Unrelated workspace widgets are
stubbed; interactive render-mode annotations are resolved by the test activator.

Request approval flow coverage exercises the configuration service and settings
component with isolated EF data. It checks organisation, Job Type, Leave Type
and request-kind selection using Request Type rather than Leave Type; sharing
flows across Leave Types; importing an existing leave-specific flow as a draft;
HR-only early returns; Supervisor → HR extensions;
new-leave flow configuration; stale-save protection; immutable workflow
versions; active supervisor and HR role resolution; applicant exclusion;
missing approvers; and HR/admin access to settings. These checks do not execute
the SQL Server workflow-start statements against production data.

Leave receipt coverage uses isolated EF data and the actual receipt page.
It verifies staff ownership (including previous jobs), approved-only access,
organisation letterhead, staff name, dates, duration, current balance, recorded
approver, legacy missing approval metadata, and the next scheduled reporting
day excluding other approved leave. Missing schedules and ambiguous balances
are not guessed. The page's print action and the history receipt link are
checked through component rendering; browser print layout is not automated.

Run from the HRM directory:

```powershell
dotnet run --project Tests/AttendanceRefresh/AttendanceRefresh.csproj
```

The runner returns a nonzero exit code if any assertion or Blazor render fails.
The application reference builds into `bin/AttendanceRefresh` so the checks can
run while a development instance holds the normal executable open.
It dispatches real button events in the actual Index page and both clock
components. The real AttendanceLogService saves to an isolated EF in-memory
database; a fake processor and asynchronously completed attendance queries
provide updated display data. No production database is accessed.

Each clock is clicked repeatedly. Assertions cover persisted event counts,
rendered weekly summary times, yearly overview data, rendered Clock Events,
daily attendance status, one daily reload per click, and preservation of the
selected overview year. Unrelated dashboard widgets and initial page loading
are replaced so the checks need no external services or login.

Leave coverage exercises the real yearly attendance query against seeded
in-memory leave records and checks the actual overview's blue day squares.
It covers inclusive date ranges, a range crossing a year boundary, overlapping
ranges, previous jobs, other employees, excluded leave states, and leave dates
that also contain clock events. Approved leave is excluded from absence,
presence and unresolved totals.

The real My Attendance table is also rendered and searched against the seeded
database. Checks cover Leave status, Annual Leave and Sick Leave type labels,
preserved clock times, exclusion from late records, and the leave chart total.

Attendance request coverage uses an isolated SQLite database for real
transactions and rollback checks. It covers the panel's current-date default,
uneven-event prefill, future early-return preview, past/current leave guards,
event ownership, duplicate requests, immutable original events, and exclusion
of pending events from attendance. The actual early-return submission and
approval application logic is exercised to verify exact partial and full
refunds to JobLeaveTypes; workflow identity/assignment and request numbering
are replaced. SQL Server production data is never accessed.

The request panel's Leave change flow is exercised through select and button
events for both extensions and early returns, including balance previews and
blocking leave with pending changes. Transactional service checks verify an
approved extension deducts the correct balance and insufficient balance blocks
an extension.

Past leave correction tests require a valid check-in for that employee on the
same day. Both legacy check-ins and effective resolved automatic check-ins are
covered. Check-outs, other employees' events, pending proposed check-ins and
superseded check-in resolutions do not qualify. The exception applies only to
corrections and does not change leave dates or balances.

Rendered correction actions are checked to ensure blocked leave dates have no
correction button and are excluded from the event dropdown, while dates with
qualifying check-ins keep both actions available.

Leave history checks cover approved records (including previous employment and
records without workflow links), newest-10 ordering and limiting, personal
ownership, inclusive overlap dates, combined status/date searches, invalid
date ranges and filter reset. The actual recent card and full-history form are
rendered, including legacy detail viewing and consistent status labels.
