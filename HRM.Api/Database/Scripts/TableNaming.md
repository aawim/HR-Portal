# Feature table naming

`AL_AllowanceAssessments` stores append-only HR allowance amount decisions. Existing `Positions.PositionFrameworkID` / `PositionMatrixID` and the `PF_` framework/matrix catalogue are reused in place; no replacement position tables are created. See `docs/position-management.md`.

Allowances use `AL_AllowanceTypes`, `AL_PositionAllowanceRules`, `AL_StaffAllowances`. Deductions use `DA_DeductionPolicies`, `DA_DeductionAgreements`, `DA_Instalments`, `DA_Payments`, `DA_PaymentAllocations`. Their shared approval/document/payroll integration uses `AD_FinanceRequests`, `AD_RequestDocuments`, `AD_PayrollBatches`, `AD_PayrollEntries`. See `docs/staff-allowances-deductions.md` for columns and deployment.

New physical tables must use an uppercase feature-initial prefix followed by an underscore and a descriptive PascalCase plural name. Example: `LC_LateCheckInRequests`. Related tables share a prefix, as the existing workflow tables share `WF_`.

| Previous name | Current name |
| --- | --- |
| `LateCheckInRequests` | `LC_LateCheckInRequests` |
| `DutyChangeRequests` | `DC_DutyChangeRequests` |
| `AdminAccessGrantRequests` | `AA_AdminAccessGrantRequests` |
| `StaffOnboardingRequests` | `SO_StaffOnboardingRequests` |
| `StaffNotifications` | `SN_StaffNotifications` |
| `RequestNotifications` | `RN_RequestNotifications` |
| `LeaveCarryForwards` | `LCF_LeaveCarryForwards` |
| `JobTypeLeaveAccrualRules` | `LA_JobTypeLeaveAccrualRules` |
| `LeaveAccrualPostings` | `LA_LeaveAccrualPostings` |
| `LeaveHandoverReviews` | `LH_LeaveHandoverReviews` |
| `LeaveHandoverSupervisors` | `LH_LeaveHandoverSupervisors` |
| `UserTodos` | `UT_UserTodos` |
| New working-hours rules | `WH_JobTypeWorkingHourRules` |
| New weekly exception requests | `WH_WeeklyHourExceptionRequests` |
| New dated organisation schedule amendments | `SA_OrganisationScheduleAmendments` |

Existing installations: apply `20261022_PrefixFeatureTables.sql` before starting the rebuilt application. The EF migration `20261022160000_PrefixFeatureTables` runs the same embedded script and is safe after manual application. New-install scripts use the prefixed names. Applied historical migrations retain their original names and the new migration performs the upgrade.

The rename preserves table object IDs, row counts, foreign keys, indexes and constraints. Index and constraint names are retained. The script refuses a collision or an outstanding SQL-module dependency. For rollback, stop the new application, set `@Revert = 1`, run the script, then deploy the matching previous application version.

Entity class and DbSet names remain descriptive C# names. Physical names are set explicitly in EF mappings; handwritten SQL must use those same physical names. Existing unrelated legacy tables are outside this rename. Future table creation scripts, migrations and documentation must follow this convention.

- `WH_OrganisationWorkingHourRequirements`: tenant and optional Job Type temporary daily working-hour increases; `FromDate`, `ToDate`, `RequiredDailyMinutes`, `Reason`, `IsActive`, `Version`, `OperationLogId`.

- `WP_` is the feature prefix for new shared work-planning configuration. `WP_ScopeScheduleAssignments` stores organisation, approved structure and staff-group template assignments. Existing legacy scheduling tables retain their physical names.

- `OT_` is the overtime policy feature prefix. `OT_JobTypeOvertimeRules` stores tenant-specific Job Type eligibility and daily/weekly OT caps.

- `SP_StaffPositionChangeRequests`: staff position assignment, change, date and removal requests. Stores proposed changes; approved JobPositions change only at final workflow approval.

- `OT_OvertimeRequests`: tenant-scoped overtime claims, immutable approval policy snapshots and payroll payment references.

Supervisor additions/removals use `SV_SupervisorChangeRequests`; shared workflow history remains in the `WF_` tables.

`LS_JobTypeLeaveSets` stores current-tenant Job Type to leave-set assignments. Existing `LeaveSets` and `LeaveSetLeaveTypes` remain in place; no historical tables are replaced.

`LT_` is the unified leave-type feature prefix. `LT_LeaveTypes` is the in-place rename/consolidation of the existing catalogue; `LT_TypeMigrationAudits` preserves retired definition/mapping/configuration snapshots. See `docs/unified-leave-types.md`.

Related leave type policy tables use `LT_LeavePolicies` and `LT_LeavePolicyAccrualRules`. Migration `20261026061841_PrefixLeavePolicyTables` performs in-place, data-preserving renames. Existing constraint and index names remain unchanged. Applied historical migrations retain the names used when they were deployed.

`LH_LeaveHandoverRules` and `LH_LeaveHandoverRuleTypes` store scoped, effective-dated work-handover requirements and selected leave types. They share the established `LH_` handover prefix. Existing `Leaves` receives nullable `HandoverRuleSnapshot`; historical rows are not rewritten.

`OT_WorkRequests` stores dated OT preapproval and missing-clock requests; `OT_WorkTasks` stores their task list. `OT_OvertimeRequests` retains claims and gains saved clock event references, timestamp snapshots, claim source and an optional approved task reference.

`CH_Conversations` and `CH_Messages` store private tenant-scoped direct staff chats, read cursors and optional authorised request references. User and request foreign keys are restrictive; history is not cascade-deleted.

`CH_Memberships` retains each join/leave interval, manager role, message visibility boundary and read cursor. `CH_Events` retains group creation, conversion, membership and manager-change events. Existing `CH_Conversations` gains `IsGroup` and `Title`; existing direct conversations and message records are preserved.

`OT_PayrollBatches` stores tenant payroll month, reference and processing audit. `OT_PayrollItems` snapshots approved claims and prevents a claim being processed twice. `TR_OT_PayrollStateLock` protects processed (and legacy paid) claim statuses on `Requests`.

`BE_Individuals` gains nullable `IsForeigner`, `ImageData` and `ImageContentType` through an additive migration. Existing individuals are preserved without guessed classification. No new physical table is created.

The business entity registry uses `BE_` consistently: `Individuals` → `BE_Individuals`, `BusinessEntities` → `BE_BusinessEntities`, `VerifiedStates` → `BE_VerifiedStates`, and `BusinessEntityStates` → `BE_BusinessEntityStates`. `PrefixBusinessEntityTables` and `20261027_PrefixBusinessEntityTables.sql` rename existing objects in place. Original primary keys, foreign keys, indexes and state IDs are retained; the reviewed legacy `UserGetInfo` SQL module is updated without changing its interface or permissions. Historical applied migrations/scripts retain the names used when originally deployed. Current EF mappings and runtime SQL use the prefixed tables.
