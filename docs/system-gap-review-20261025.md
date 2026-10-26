# System gap review — 25 October 2026

## Scope and limits

Source review of the current HRM checkout, concentrating on tenant permissions, request/position workflows, registration, attendance, overtime, navigation, deployment and regression coverage. Existing deployment failure evidence from this session was also inspected. No application code or database records were changed for this review. No tests, penetration tests, package advisory scans or production workflow actions were run. Findings identify verified code gaps and explicitly label dormant/conditional paths; this is not a complete security certification.

P1 = address before wider tenant rollout or relying on the affected feature. P2 = important integration/reliability work. P3 = incomplete features and cleanup.

## Recommended order

1. Close tenant/configuration permission gaps (G01–G02).
2. Correct position approval-to-attendance integration and registration (G03–G04).
3. Repair deployment baseline and emergency access revocation (G05–G06).
4. Consolidate workflow writers and lifecycle rules (G07–G08).
5. Complete OT enforcement, generic request authorisation, tests/CI and search reliability (G09–G12).
6. Inventory unfinished modules and remove inactive duplicates safely (G13).

## Findings

### G01 — P1: Legacy leave configuration can cross tenant boundaries

The active LeaveConfigGrid loads all organisations and all LeaveConfigurableValues. Save and delete fetch rows by ID without a tenant predicate or a management-permission check. Setting the current organisation as the default on a new form does not protect existing rows. An admitted admin-portal user can be shown and operate on configuration outside the current organisation.

Evidence: [Components/Leave/LeaveConfigGrid.razor:309](G:/NewEadmin/WebApp/HRM/Components/Leave/LeaveConfigGrid.razor:309), [Components/Leave/LeaveConfigGrid.razor:362](G:/NewEadmin/WebApp/HRM/Components/Leave/LeaveConfigGrid.razor:362), [Components/Leave/LeaveConfigGrid.razor:401](G:/NewEadmin/WebApp/HRM/Components/Leave/LeaveConfigGrid.razor:401).

**Completion condition:** Move these operations into a tenant-scoped service. Check the required permission on every read/write, validate organisation ownership of each ID and foreign key, and add cross-tenant denial cases.

### G02 — P1: Shared Job Type and Request Type writes lack a configuration permission gate

JobTypeConfigurationService.RequireOrganisationAsync checks identity and organisation access, but not permission to edit catalogues or approval workflows. SaveJobTypeAsync and SaveRequestTypeAsync then change shared catalogue records; the same service also saves workflows. The settings host only uses an authenticated AuthorizeView. Organisation access is insufficient authority to edit global configuration.

Evidence: [Services/JobTypeConfigurationService.cs:14](G:/NewEadmin/WebApp/HRM/Services/JobTypeConfigurationService.cs:14), [Services/JobTypeConfigurationService.cs:55](G:/NewEadmin/WebApp/HRM/Services/JobTypeConfigurationService.cs:55), [Services/JobTypeConfigurationService.cs:143](G:/NewEadmin/WebApp/HRM/Services/JobTypeConfigurationService.cs:143).

**Completion condition:** Require explicit server-side catalogue/workflow permissions. Separate global catalogue administration from tenant-specific policy configuration and audit catalogue mutations.

### G03 — P1: New position approvals are not included in attendance approval-date eligibility

AttendanceEligibilityCalendar discovers approval dates only through legacy JobPositionsRequests with RequestTypeId 32. The new staff position request callback inserts approved JobPositions without creating that legacy link or recording a resulting-position ID on the new request. An assignment approved after its requested FromDate therefore falls back to FromDate for attendance eligibility and can introduce absence before approval. This is a source-confirmed integration gap; a historical attendance recalculation was not performed in this review.

Evidence: [Helpers/AttendanceEligibilityCalendar.cs:29](G:/NewEadmin/WebApp/HRM/Helpers/AttendanceEligibilityCalendar.cs:29), [Helpers/AttendanceEligibilityCalendar.cs:49](G:/NewEadmin/WebApp/HRM/Helpers/AttendanceEligibilityCalendar.cs:49), [Services/StaffPositionManagementService.cs:167](G:/NewEadmin/WebApp/HRM/Services/StaffPositionManagementService.cs:167), [Models/StaffPositionChangeRequest.cs:5](G:/NewEadmin/WebApp/HRM/Models/StaffPositionChangeRequest.cs:5).

**Completion condition:** Persist the resulting assignment/request relationship and its approval timestamp. Resolve eligibility from all supported position approval paths. Verify a backdated assignment does not create pre-approval absences.

### G04 — P1: Manual individual registration is unfinished and DNR lookup returns sample identity data

AddNewIndividualGrid is reachable through HR → Individuals → Add New. Its Save Individual button has no click or submit handler. FetchDNR waits and then writes fixed example names, birth date and gender into the form; no DNR service is called. The UI currently presents functionality that does not register a person and can display fictional registry information.

Evidence: [Components/Admin/Hr/Individual/AddNewIndividualGrid.razor:332](G:/NewEadmin/WebApp/HRM/Components/Admin/Hr/Individual/AddNewIndividualGrid.razor:332), [Components/Admin/Hr/Individual/AddNewIndividualGrid.razor:391](G:/NewEadmin/WebApp/HRM/Components/Admin/Hr/Individual/AddNewIndividualGrid.razor:391).

**Completion condition:** Implement validated, duplicate-safe BusinessEntity/Individual registration through a permission-checked service. Connect the authorised DNR integration or disable lookup with an honest unavailable message. Do not display sample responses as registry results.

### G05 — P1: Deployment is blocked by legacy migration-history/schema mismatch

The normal EF database-update attempt earlier in this session tried to apply 20260910062632_AddJobWorkTemplates and failed because AdditionDeductionTypeDependencyTimePeriodAmounts already exists. Recent additive changes were installed using migration-specific SQL scripts. That makes local functionality available, but does not repair the standard deployment path. The failed attempt is recorded in overtime-database-update.log; migration application was not retried during this review.

Evidence: [Migrations/20260910062632_AddJobWorkTemplates.cs:15](G:/NewEadmin/WebApp/HRM/Migrations/20260910062632_AddJobWorkTemplates.cs:15).

**Completion condition:** Reconcile the deployed schema and migration history with an audited baseline procedure. Rehearse both an empty-database deployment and an upgrade against a restored database copy. Do not blindly mark migrations applied or drop existing tables.

### G06 — P1: Emergency admin-access revocation is blocked by a pending grant request

RevokeAsync returns a failure whenever the target account has an open Admin access grant request. An administrator must first cancel/reject that request, potentially depending on other workflow actors, before removing current access. A pending request should not prevent emergency revocation of existing access.

Evidence: [Services/AdminAccessGrantService.cs:309](G:/NewEadmin/WebApp/HRM/Services/AdminAccessGrantService.cs:309).

**Completion condition:** Provide an audited emergency revoke operation that disables access and invalidates sessions immediately, while cancelling or preventing later approval of outstanding grant requests.

### G07 — P2: Approval-flow configuration still has multiple writers

Request Approval Flows uses LeaveFlowConfigurationService and User Group stages with LeaveRequestKind = 0. Job Type Configuration and Request Type Configuration still expose their own workflow editor using JobTypeConfigurationService and named-staff stages with LeaveRequestKind = null. Both write WF definitions, but query and version different subsets. Administrators can save one editor while the runtime selects a different applicable flow.

Evidence: [Services/Leave/LeaveFlowConfigurationService.cs:99](G:/NewEadmin/WebApp/HRM/Services/Leave/LeaveFlowConfigurationService.cs:99), [Services/Leave/LeaveFlowConfigurationService.cs:132](G:/NewEadmin/WebApp/HRM/Services/Leave/LeaveFlowConfigurationService.cs:132), [Services/JobTypeConfigurationService.cs:130](G:/NewEadmin/WebApp/HRM/Services/JobTypeConfigurationService.cs:130), [Services/JobTypeConfigurationService.cs:170](G:/NewEadmin/WebApp/HRM/Services/JobTypeConfigurationService.cs:170), [Components/Admin/Settings/JobTypes/JobTypeConfiguration.razor:93](G:/NewEadmin/WebApp/HRM/Components/Admin/Settings/JobTypes/JobTypeConfiguration.razor:93).

**Completion condition:** Keep one approval-flow editor and writer, redirect other workflow controls there, define one precedence rule, and migrate legacy definitions without altering in-flight request instances.

### G08 — P2: Position end-date rules disagree between features

Position management and Admin Portal Access treat an assignment whose end date is today as no longer actionable/eligible. Position Overview and attendance still treat the same end date as active through today. Thus one person can be excluded from a selector while still counted as active and expected to attend elsewhere.

Evidence: [Services/StaffPositionManagementService.cs:138](G:/NewEadmin/WebApp/HRM/Services/StaffPositionManagementService.cs:138), [Services/AdminAccessGrantService.cs:151](G:/NewEadmin/WebApp/HRM/Services/AdminAccessGrantService.cs:151), [Services/PositionOverviewService.cs:33](G:/NewEadmin/WebApp/HRM/Services/PositionOverviewService.cs:33), [Helpers/AttendanceEligibilityCalendar.cs:53](G:/NewEadmin/WebApp/HRM/Helpers/AttendanceEligibilityCalendar.cs:53).

**Completion condition:** Define whether ToDate is inclusive or exclusive, then use a shared lifecycle rule for status, actions, searches, attendance and counts. Cover today, yesterday, future dates and midnight boundaries.

### G09 — P2: OT policy is configuration only

OT eligibility and daily/weekly caps are stored and edited by JobTypeOvertimeSettingsService. Runtime searches found no OT calculation, claim or approval consumer of those limits. Weekly reconciliation still reports potential overtime independently. This was disclosed when the configuration was added; it remains an incomplete business process.

Evidence: [Services/JobTypeOvertimeSettingsService.cs:23](G:/NewEadmin/WebApp/HRM/Services/JobTypeOvertimeSettingsService.cs:23), [Services/JobTypeOvertimeSettingsService.cs:33](G:/NewEadmin/WebApp/HRM/Services/JobTypeOvertimeSettingsService.cs:33), [Helpers/WeeklyWorkingHours.cs:111](G:/NewEadmin/WebApp/HRM/Helpers/WeeklyWorkingHours.cs:111).

**Completion condition:** Add OT submission/approval and enforce the tenant/Job Type policy against cumulative daily and weekly usage, including concurrent requests. Preserve raw worked time and distinguish potential, requested, approved and paid OT.

### G10 — P2: Generic request service does not enforce request ownership or assigned approvers

RequestService.GetByIdAsync fetches a request by ID without authorisation or tenant filtering. ChangeStateAsync requires a signed-in context and a permitted transition, but does not verify request organisation, ownership, stage assignment or self-approval; only the new position request family has an explicit bypass guard. No active component calls to this generic mutator were found, so this is a service-contract gap rather than a demonstrated public exploit.

Evidence: [Services/RequestService.cs:89](G:/NewEadmin/WebApp/HRM/Services/RequestService.cs:89), [Services/RequestService.cs:101](G:/NewEadmin/WebApp/HRM/Services/RequestService.cs:101).

**Completion condition:** Restrict or retire generic state mutation. Route every request family through its domain validation and shared workflow processor. Apply tenant and resource-authorisation checks to generic reads.

### G11 — P2: Recent features lack targeted regression coverage and CI wiring

There is a substantial console regression suite, including permissions, attendance, leave, schedules and onboarding, and tools/verify-project.ps1 runs it. Source searches found no tests referencing the new GroupsManagementService, StaffPositionChangeRequests, JobTypeOvertime policies, RequireActivePosition selector behaviour or shared ScopeScheduleAssignments. No CI workflow configuration was found in this checkout. This is not a claim that the existing suite fails; it was not run during this review.

Evidence: [Tests/AttendanceRefresh/Program.cs:1](G:/NewEadmin/WebApp/HRM/Tests/AttendanceRefresh/Program.cs:1), [tools/verify-project.ps1:1](G:/NewEadmin/WebApp/HRM/tools/verify-project.ps1:1).

**Completion condition:** Add focused domain/permission tests and SQL Server integration cases for the new features, plus route/selector tests. Run the existing verification script in CI using isolated test resources; no live database may be modified by tests.

### G12 — P2: Staff search can return stale results and miss full names

WorkspaceSearch starts an unawaited search for each keystroke without debounce, cancellation or request-version checking. An older query can finish after a newer one and replace its results. The full-name expression always inserts a space around the middle name; an absent middle name gives double spaces and a normal single-space full-name query may fail.

Evidence: [Components/Admin/My/Workspace/WorkspaceSearch.razor:158](G:/NewEadmin/WebApp/HRM/Components/Admin/My/Workspace/WorkspaceSearch.razor:158), [Components/Admin/My/Workspace/WorkspaceSearch.razor:268](G:/NewEadmin/WebApp/HRM/Components/Admin/My/Workspace/WorkspaceSearch.razor:268).

**Completion condition:** Use debounced/cancellable searches and discard responses for an older query or organisation. Normalise search tokens and names consistently, retaining tenant and active-position predicates.

### G13 — P3: Visible placeholders and unimplemented service contracts remain

HR exposes CSC fetch/classification/designation/office and Requests Overview entries with no component. Settings has an Attendance Clients placeholder. Registered BusinessAccessService job/payroll access methods and JobStore.GetJobID throw NotImplementedException; several other legacy leave/audit methods are also unimplemented. Some have no active caller and must be classified before removal. The older Shared/WorkspaceSearch is also unscoped but appears only in a commented settings block, so it is cleanup work, not a confirmed active tenant leak.

Evidence: [Components/Pages/HrCategoryPage.razor:184](G:/NewEadmin/WebApp/HRM/Components/Pages/HrCategoryPage.razor:184), [Components/Pages/HrCategoryPage.razor:193](G:/NewEadmin/WebApp/HRM/Components/Pages/HrCategoryPage.razor:193), [Components/Admin/Settings/SettingsCategoryPage.razor:274](G:/NewEadmin/WebApp/HRM/Components/Admin/Settings/SettingsCategoryPage.razor:274), [Services/BusinessAccessService.cs:523](G:/NewEadmin/WebApp/HRM/Services/BusinessAccessService.cs:523), [Services/BusinessAccessService.cs:767](G:/NewEadmin/WebApp/HRM/Services/BusinessAccessService.cs:767), [Services/JobStore.cs:11](G:/NewEadmin/WebApp/HRM/Services/JobStore.cs:11).

**Completion condition:** Create a feature inventory marking implemented, partial and unavailable features. Hide unsupported navigation or show an accurate unavailable state. Implement reachable contracts; remove only demonstrably unused compatibility code.

