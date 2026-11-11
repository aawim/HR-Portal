# Feature table naming

Pay framework configuration reuses the existing `PF_` tables in place (`PF_Frameworks`, `PF_JobMatrix`, `PF_JobMatrixSteps`, `PF_JobMatrixTitles`, `PF_JobMatrixBenefits`, `PF_JobTitles`, `PF_PositionMatrix`, `PF_PositionMatrixHistory`, `PF_JobStepHistory`). The additive `PayFrameworkMatrixManagement` migration adds framework/currency links without replacement tables or reclassification of historical rows. Existing physical names, including established singular matrix table names, are preserved. See `docs/pay-framework-configuration.md`.

The reviewed 29 October cleanup retains active registry tables as `BE_BusinessEntityDocuments`, `BE_BusinessEntityCalendars`, `BE_BusinessEntityLocations`, `BE_BusinessEntityLocationTypes`, `BE_BusinessEntitySchedules`, `BE_BusinessEntityTypes`, `BE_BusinessEntityContacts`. Legacy dependent catalogues use `BT_BreakTimes`, `BG_BudgetItems`, `PY_AdditionOrDeductionTypes`. `SC_TableRetirementAudits` preserves schema and row evidence for explicitly retired tables. See `docs/legacy-table-usage-audit.md` for all eighteen decisions and recovery details.

`AL_AllowanceAssessments` stores append-only HR allowance amount decisions. Existing `Positions.PositionFrameworkID` / `PositionMatrixID` and the `PF_` framework/matrix catalogue are reused in place; no replacement position tables are created. See `docs/position-management.md`.

Allowances use `AL_AllowanceTypes`, `AL_PositionAllowanceRules`, `AL_StaffAllowances`. Deductions use `DA_DeductionPolicies`, `DA_DeductionAgreements`, `DA_Instalments`, `DA_Payments`, `DA_PaymentAllocations`. Their shared approval/document/payroll integration uses `AD_FinanceRequests`, `AD_RequestDocuments`, `AD_PayrollBatches`, `AD_PayrollEntries`. See `docs/staff-allowances-deductions.md` for columns and deployment.

New physical tables must use an uppercase feature-initial prefix followed by an underscore and a descriptive PascalCase plural name. Example: `LC_LateCheckInRequests`. Related tables share a prefix, as the existing workflow tables share `WF_`.

`AC_` is the attribute-access-control prefix. `AC_StaffAccessPolicies` marks position classifications (for example Political) as restricted resources; `AC_StaffAccessScopes` applies explicit-only visibility to selected users or role groups; and `AC_StaffAccessGrants` stores audited allow/deny grants for a classification or a specific staff member.

`BG_` is the Public Finance Budget Management & Payroll Coupling feature prefix, compliant with Maldives Public Finance Act (Law No. 3/2006) and Ministry of Finance Chart of Accounts (COA). `BG_FiscalYears` stores annual budget cycles, approved total allocations, and personal emolument envelopes; `BG_BudgetCategories` stores MoF standard economic chapters (e.g. Chapter 21 Compensation of Employees); `BG_BudgetItems` stores standardized economic codes (e.g. 211001 Basic Salary, 211002 Overtime, 212003 Technical Allowance, 213006 Pension 7%); `BG_BudgetTransactionTypes` stores statutory allocation, transfer, commitment, and expenditure transaction classifications; `BG_BudgetAllocations` tracks organisation-scoped approved, vired, committed, and expended funds; `BG_BudgetVirements` stores formal transfers between budget votes with legal justification and MoF sanction references; `BG_BudgetCommitments` tracks pre-payroll batch encumbrances and procurement reservations; `BG_BudgetTransactions` stores immutable double-entry ledger postings with Bandeyri / SAP authority references; `BG_PayrollBudgetMappings` links configurable payroll earnings and allowance components to MoF economic codes; and `BG_BudgetAuditEvents` provides immutable, append-only financial audit trails.

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


`AR_` is the attendance reconciliation feature prefix. `AR_AttendanceObligations` stores independent daily schedule obligations (`Official`, `Duty`, `OfficialBreak`) with their resolved state (`NotStarted`, `ClockedIn`, `OnBreak`, `ClockedOut`, `MissingClockIn`, `MissingClockOut`, `PartiallyResolved`, `Conflict`, `Completed`) and planned/actual/credited durations; and `AR_AttendanceManualOverrides` stores versioned HR manual classification overrides and missing-event acknowledgements. Existing `AttendanceLogResolutions` gains `IsPrimaryClassification`, `SchedulePurpose`, `LogicalEventType`, `MatchMethod`, `MatchReason`, `ConfidenceState`, `PolicyVersion`, `ReconciliationRunID`, `AttendanceObligationID` and manual override metadata, with `UX_AttendanceLogResolutions_ActivePrimaryPerLog` and `UX_AttendanceLogResolutions_ActiveSegmentPerLog` allowing one physical `AttendanceLogs` record to satisfy both a primary official classification and policy-permitted secondary duty/break associations. See `docs/attendance-reconciliation-architecture.md`.

`DMS_` is the Enterprise Document & Records Management System (DHR-DMS) feature prefix. `DMS_Categories` stores configurable staff, organisation and workflow document categories; `DMS_DocumentTypes` stores document type policies, permitted formats, classification defaults and approval/retention rules; `DMS_RetentionPolicies` stores organisation-approved retention schedules and archive/disposal rules; `DMS_StoredObjects` stores immutable private binary objects, SHA-256 content hashes and malware scan evidence; `DMS_Documents` stores the central document registry, version pointers, checkout state and official-record locks; `DMS_DocumentVersions` stores preserved version history and snapshots; `DMS_DocumentLinks` stores many-to-many links between central documents and HRM business records without duplicating physical binaries; `DMS_DocumentPermissions` stores explicit document-level access control entries; `DMS_DocumentEvents` stores the immutable document audit trail; `DMS_DocumentTemplates` stores versioned official document templates with safe verified HRM placeholders; `DMS_DocumentRequests` stores employee and HR official document requests linked to `dbo.Requests`; `DMS_LegalHolds` stores legal holds blocking disposition or deletion; and `DMS_DocumentSignatures` stores digital approval and signature digests. Existing legacy `Documents`, `DocumentTypes` and `BE_BusinessEntityDocuments` are preserved and synchronised.

`SEP_` is the enterprise staff separation, offboarding and clearance management feature prefix. `SEP_SeparationTypes` stores configurable separation categories and notice requirements; `SEP_ClearanceTemplates` stores tenant and role-specific clearance templates; `SEP_ClearanceTemplateTasks` stores checklist task definitions; `SEP_StaffSeparations` stores separation requests, employee snapshots, notice dates and execution status; `SEP_ClearanceTasks` stores per-instance cross-departmental clearance items; `SEP_WorkHandovers` stores supervisor/HOD handover sign-offs; `SEP_WorkHandoverItems` stores granular project, duty and workflow reassignments; `SEP_AssetClearances` reconciles staff asset return and damage assessments; `SEP_FinanceItems` stores final settlements, leave encashment, arrears and debt recoveries; `SEP_AccessRevocations` tracks application roles, eFaas org unbinding, door cards and office key revocations; `SEP_SeparationDocuments` stores generated official PDF clearance certificates; and `SEP_SeparationEvents` stores immutable separation audit events. See `docs/staff-separation-offboarding-database.md`.

`PAY_` is the Enterprise Payroll & Compensation Management feature prefix. `PAY_PayrollPeriods` stores organisation-scoped payroll periods, cycle months, and cutoff dates; `PAY_SalaryComponents` stores configurable earnings, allowances, deductions, and employer contributions; `PAY_SalaryStructures` stores salary grade baselines; `PAY_EmployeeCompensation` stores effective-dated basic salary, grade, step, and bank account details; `PAY_CalculationRules` stores pension, tax, and proration formulas; `PAY_PayrollRuns` stores regular, supplementary, off-cycle, and settlement runs; `PAY_PayrollRunBatches` tracks bounded employee batch executions; `PAY_PayrollEmployees` stores employee-level gross, deduction, employer contribution, and net pay results; `PAY_PayrollLines` stores itemized breakdown lines; `PAY_PayrollInputSnapshots` stores frozen, hashed input snapshots for deterministic reproduction; `PAY_PayrollExceptions` stores blocking errors and warning issues; `PAY_PayrollAdjustments` stores approved manual corrections; `PAY_PaymentBatches` stores bank export instructions; `PAY_PaymentTransactions` stores employee-level transfer instructions; `PAY_PayrollAuditEvents` stores immutable state transition audit logs; and `PAY_BackgroundJobs` stores durable distributed queue jobs with lease ownership, fencing tokens, heartbeats, and exponential retry.

`TM_` is the team hierarchy operational prefix. `TM_TeamScheduleAdjustments` stores audited, dated check-in/check-out overlays for sub-team members without rewriting schedule templates or attendance punches.


OC_ is the organisation calendar feature prefix. OC_OrganisationEvents (with OC_OrganisationEventAudiences, OC_OrganisationEventRecurrences, OC_OrganisationEventOccurrenceExceptions) stores events; OC_CalendarExceptions and OC_CalendarExceptionSegments store approved working-day overrides; OC_CalendarImpactAssessments stores leave/plan/schedule impact reviews; OC_EventNotifications deduplicates notifications delivered through SN_StaffNotifications; OC_SupervisorTasks and OC_SupervisorTaskAssignees store supervisor calendar tasks. Script: 20261111_OrganisationCalendar.sql.
