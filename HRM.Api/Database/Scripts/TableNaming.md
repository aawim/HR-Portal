# Feature table naming

Pay framework configuration reuses the existing `PF_` tables in place (`PF_Frameworks`, `PF_JobMatrix`, `PF_JobMatrixSteps`, `PF_JobMatrixTitles`, `PF_JobMatrixBenefits`, `PF_JobTitles`, `PF_PositionMatrix`, `PF_PositionMatrixHistory`, `PF_JobStepHistory`). The additive `PayFrameworkMatrixManagement` migration adds framework/currency links without replacement tables or reclassification of historical rows. Existing physical names, including established singular matrix table names, are preserved. See `docs/pay-framework-configuration.md`.

The reviewed 29 October cleanup retains active registry tables as `BE_BusinessEntityDocuments`, `BE_BusinessEntityCalendars`, `BE_BusinessEntityLocations`, `BE_BusinessEntityLocationTypes`, `BE_BusinessEntitySchedules`, `BE_BusinessEntityTypes`, `BE_BusinessEntityContacts`. Legacy dependent catalogues use `BT_BreakTimes`, `BG_BudgetItems`, `PY_AdditionOrDeductionTypes`. `SC_TableRetirementAudits` preserves schema and row evidence for explicitly retired tables. See `docs/legacy-table-usage-audit.md` for all eighteen decisions and recovery details.

`AL_AllowanceAssessments` stores append-only HR allowance amount decisions. Existing `Positions.PositionFrameworkID` / `PositionMatrixID` and the `PF_` framework/matrix catalogue are reused in place; no replacement position tables are created. See `docs/position-management.md`.

Allowances use `AL_AllowanceTypes`, `AL_PositionAllowanceRules`, `AL_StaffAllowances`. Deductions use `DA_DeductionPolicies`, `DA_DeductionAgreements`, `DA_Instalments`, `DA_Payments`, `DA_PaymentAllocations`. Their shared approval/document/payroll integration uses `AD_FinanceRequests`, `AD_RequestDocuments`, `AD_PayrollBatches`, `AD_PayrollEntries`. See `docs/staff-allowances-deductions.md` for columns and deployment.

New physical tables must use an uppercase feature-initial prefix followed by an underscore and a descriptive PascalCase plural name. Example: `LC_LateCheckInRequests`. Related tables share a prefix, as the existing workflow tables share `WF_`.

`IM_` is the item management feature prefix. `IM_ItemCategories` stores organisation-scoped catalogue groups, `IM_Items` stores asset and consumable catalogue entries, `IM_ItemUnits` stores individually tracked asset units, `IM_ItemStaffRequests` stores requisition and direct issue headers, and `IM_ItemStaffRequestLines` stores the separately requested and approved item names and quantities.

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

JobMatrixProfilesAndEmployeeSteps reuses PF_JobMatrixQualifications and PF_JobMatrixDuties and adds profile columns to PF_JobMatrix. Employee step selections are stored on the existing SP_StaffPositionChangeRequests and SO_StaffOnboardingRequests; approved steps remain in PF_JobStepHistory by JobID. No replacement tables or destructive step-history conversion are used.

PF_Professions stores versioned profession/occupation references linked to existing PF_JobFamilies. PF_JobMatrix.ProfessionID links dropdown selections without replacing position/job history. Existing family and title tables are reused.

PF_StudyFieldSuggestions stores active, family-linked educational-discipline suggestions with source metadata. Existing matrix FieldsOfStudy snapshots are preserved.

PF_ExperienceAreaSuggestions contains sector-grouped experience examples with an optional job-family scope, description and source metadata. A null family denotes general suggestions, not automatic eligibility. Existing Job Matrix ExperienceAreas text is preserved.

PF_CredentialRequirements, PF_QualificationPathways and PF_PathwayQualifications store structured versioned Job Matrix licensing and AND/OR qualification requirements. Removed draft requirements are soft-retired; existing free-text and legacy qualification records are preserved.

PF_ProfessionRequirementTemplates stores profession/rank-specific source-linked requirement presets. Values are copied into matrix snapshots only when selected; existing employment and financial records are not modified.

`ST_` is the internal staff transfer management feature prefix. `ST_StaffTransfers` stores transfer request headers, versioned employee snapshots, source/destination structures and lifecycle status; `ST_StaffTransferJobDetails` stores current and proposed job, `PF_PositionMatrix` and appointment mode details; `ST_StaffTransferWorkDetails` stores schedule, template, attendance planning provider and location arrangements; `ST_StaffTransferAccessChanges` stores application/organisational permission comparisons; `ST_StaffTransferDoorAccess` stores physical security and door-access provisioning states; `ST_StaffTransferReportingDetails` stores reporting and handover arrangements; `ST_StaffTransferComments` stores non-blocking supervisor/HOD and participant comments; `ST_StaffTransferConfirmations` stores versioned employee confirmations; `ST_StaffTransferActivationLogs` stores idempotent activation execution steps; `ST_StaffTransferDocuments` stores versioned A4 PDF snapshots and hashes; and `ST_JobOrganisationHistories` stores effective-dated job organisation structure placement history.

`AM_` is the staff asset assignment, acknowledgement and custody management feature prefix. `AM_AssetCategories` stores serialized asset category policies; `AM_AssetConditions` stores the asset condition dictionary; `AM_AssetItems` stores individually serialized physical inventory assets; `AM_AssetIssuances` stores multi-item staff issuance request headers and employee/job snapshots; `AM_AssetIssuanceItems` stores distinct physical issuance lines, reservations, handover timestamps and versioned item snapshots; `AM_AssetAcknowledgements` stores append-only per-item employee accept/reject decisions; `AM_AssetHrDecisions` stores append-only HR review decisions with idempotency keys; `AM_AssetCustodies` stores effective-dated active and historical custody records; `AM_AssetReturns` and `AM_AssetReturnItems` store return, damage, loss and clearance headers and inspected item dispositions; `AM_AssetTransferDispositions` stores internal transfer asset retain/return/department-change/replace decisions; `AM_AssetEvents` stores the immutable audit timeline; `AM_AssetAttachments` stores controlled evidence file metadata and hashes; and `AM_AssetDocuments` stores immutable A4 handover and acknowledgement PDF snapshots.

`AM_` is extended for asset service, maintenance, upgrade and replacement lifecycle management: `AM_AssetServiceRequests` stores service, upgrade and replacement tickets; `AM_AssetServiceRequestItems` stores individual line items under service; `AM_AssetMaintenanceJobs` stores diagnostic work orders, bench test results, vendor tracking and repair costs; `AM_AssetMaintenanceActions` stores technician action timestamps and hours; `AM_AssetComponents` stores modular hardware components (RAM, SSD, battery, GPU, motherboard); `AM_AssetComponentTransactions` stores hardware upgrade/swap/removal audit history; `AM_AssetServiceParts` stores spare parts inventory and reorder thresholds; `AM_AssetReplacementLinks` stores dual-asset links between original and replacement equipment; `AM_AssetTemporaryHandovers` stores temporary loaner equipment tracking; `AM_AssetInspections` stores physical and technical condition inspection checklists; `AM_AssetServiceDocuments` stores immutable service reports, upgrade certificates, replacement certificates and clearance records; and `AM_AssetConfigurationSnapshots` stores point-in-time hardware specification snapshots.
