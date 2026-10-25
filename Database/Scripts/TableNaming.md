# Feature table naming

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
