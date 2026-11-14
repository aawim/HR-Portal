# DHR Workforce Scheduling Phase 0 Assessment

## Current reusable foundation

The existing Work Planning implementation is the attendance-plan authority. `PlanningProviderResolver` resolves the effective organisation provider and defaults to `WORK_PLANNING`; `WorkPlanGenerator` only generates plans when that provider is active. It uses a process gate and SQL Server application lock per organisation, job and work date, preserves finalized plans, and already supports more than one schedule purpose on a plan.

`WorkPlan`, `WorkAssignment`, `WorkAssignmentSegment` and `WorkPlanSegment` are the existing planning model. The generator resolves official and duty schedule attachments through `EffectiveStaffSchedules`, creates dated segments, and supports overnight template end times. It must remain the target for roster-derived attendance segments; a second attendance-plan table or a profession-specific provider is not appropriate.

The existing duty-roster service is a narrow operational feature. It reads generated `WorkAssignment` rows with `SchedulePurpose == "Duty"`, supports future transfer/swap workflow requests, applies organisation/department access checks, and prevents a recipient from accepting a conflicting duty or approved leave. It does not model a separately published roster, a staffing requirement, an independently assignable duty occurrence, coverage, qualifications, rest validation, duty policies, or immutable roster revisions.

Attendance processing is currently segment/assignment based. `AttendanceWorkPlanResolver`, `AttendanceLogProcessor`, and `AttendanceSegmentEvaluator` are the integration points for a later session-evidence model. The new implementation must retain the original evidence and add deterministic match/evaluation metadata; it must not infer missing checkouts or use the latest active segment as a universal fallback.

Existing organisation access, request workflows, operation logs, notifications, leave, overtime, payroll and EF Core migrations are reusable integration points. Blazor pages must continue calling application/API contracts rather than querying the database.

## Gaps against the workforce specification

1. No generic duty-type registry or effective-dated workforce policy resolver exists.
2. There is no published roster aggregate, versioned occurrence, coverage slot, or assignment independent from a generated work assignment.
3. Work-plan segments do not retain a duty occurrence/assignment source, a resolved policy version, a time-zone identifier, or a stable session identity.
4. Attendance resolution has no persisted evidence-to-session match reason, ambiguity state, or session evaluation version.
5. There is no work-time ledger separating actual time, credited time, standby, and compensable time.
6. The current swap flow can be reused as a workflow integration, but it does not yet cover replacements, callouts, extensions, or handovers.

## Additive database migration design

The implementation will use the `WS_` workforce-scheduling prefix, avoiding the established `WF_` workflow prefix, and will not rename or replace existing `WorkPlans`, `WorkAssignments`, attendance, leave, payroll, or request data.

Phase 1 tables:

| Table | Purpose |
| --- | --- |
| `WS_DutyTypes` | Organisation-scoped, active duty-type registry with code, name, category and default attendance strategy. |
| `WS_WorkforcePolicies` | Effective-dated, versioned policy definitions with typed rule JSON and audit reference. |
| `WS_WorkforcePolicyAssignments` | Organisation, job, individual, duty-type and future assignment-scoped policy attachments with precedence. |

Later phases add `WS_Rosters`, `WS_RosterVersions`, `WS_DutyOccurrences`, `WS_DutyAssignments`, `WS_DutyCoverageRequirements`, `WS_DutyOperations`, `WS_AttendanceSessionEvaluations`, and `WS_WorkTimeLedgerEntries`. These tables will use foreign keys, organisation-scoped indexes, optimistic concurrency, immutable version links, and idempotency keys where generation or processing can be retried.

Work-plan extensions are nullable and additive: duty occurrence ID, duty assignment ID, resolved policy version ID, session key and time-zone ID. Existing plans remain valid when these values are null. Payroll receives ledger exports only after a finalized result version is available.

## Affected code map

| Area | Existing component | Planned change |
| --- | --- | --- |
| Provider selection | `HRM.Api/WorkPlanning/Services/PlanningProviderResolver.cs` | Keep provider resolution unchanged; roster-derived segments remain under `WORK_PLANNING`. |
| Plan generation | `HRM.Api/WorkPlanning/WorkPlanGenerator.cs` | Add published-duty selections after roster management is available; preserve locking/finalization behaviour. |
| Duty operations | `HRM.Api/WorkPlanning/Services/DutyRosterService*.cs` | Reuse current transfer/swap workflow where compatible; move new roster lifecycle logic into dedicated services. |
| Attendance | `AttendanceWorkPlanResolver`, `AttendanceLogProcessor`, `AttendanceSegmentEvaluator` | Add session matching/evaluation incrementally behind a feature flag. |
| API/UI | Duty roster contracts/controller and `HRM.UI` work-planning components | Add scoped contracts/endpoints/pages only after application services are complete. |
| Persistence | `HrmTeContext`, entity configurations, migrations, table-naming documentation | Register additive entities/configurations, indexes and data-preserving migrations. |

## Ordered implementation backlog

The administrative UI will expose one central **Schedule Manager**. Roster planning, duty assignments, coverage, policies, swaps and exceptions are sections of that manager, not separate schedule-management products. Employee and team workspaces consume the same data and expose only the actions appropriate to their role.

1. Workforce policy registry, policy assignment, effective-date validation and deterministic resolver.
2. Roster aggregate, versioned publication lifecycle, duty occurrences and separate staff assignments.
3. Work-plan adapter that materializes published roster assignments as source-linked duty segments.
4. Session evidence matching/evaluation with persisted reasons and ambiguous-review outcomes.
5. Coverage, qualification, leave/availability and rest-rule validation with hard-block, override-required and warning severities.
6. Replacements, swaps, callouts, extensions and handovers using existing request/workflow infrastructure.
7. Work-time ledger, entitlement export and payroll-finalization adjustment boundaries.
8. Workforce dashboard, roster planner, coverage, policy, exception and employee/team workspace views.
9. Feature flags, shadow calculation, organisation pilot and regression/deployment verification.

## Phase 1 acceptance criteria

The first implementation slice is complete only when an authorised organisation can manage versioned generic duty types and effective-dated policies; the resolver deterministically returns historical policy versions at organisation, job and duty-type scope; no job-title-specific logic is introduced; and existing plan generation and attendance tests remain unchanged.
