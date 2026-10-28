# Application contracts

The reviewed HTTP contract surface is `HRM.Application/Contracts/ApplicationContractCatalog.cs`: 75 interfaces and 337 operations. UI resolves these interfaces to typed-route HTTP proxies. The catalog describes supported schemas; it does not register endpoints or grant access. API controllers must enforce authentication, resource permissions, current-tenant scope, workflow transitions and concurrency for every operation.

`HRM.Application` references only `HRM.Core`. It has no ASP.NET, EF, API or UI assembly dependency. Remote contracts use DTOs, explicit scalars and asynchronous methods. Domain entities, DbContexts, arbitrary object graphs, query expressions, request-supplied CLR types and server audit/cache operations are excluded from the remote surface.

## Contract groups

- The 39 feature-service operation interfaces preserve current feature names and rules. Entity-bearing responses/commands are projected through explicit DTO adapters in `HRM.Api/Services/Transport`; existing internal business services remain the canonical writers.
- Existing DTO-based contracts for attendance, jobs, profiles, leave policies, leave entitlements, receipts, work planning and staff schedules are included directly in the catalog.
- `ILeaveApplications` is the DTO boundary for the existing leave service. `LeaveApplicationsService` delegates all submission/cancellation actions to that service and maps balances/history into DTOs. It adds no second approval or balance calculation path.
- `IUserAppDataService` returns the existing `JobLeaveTypeDto`, preserving assignment IDs, type names, fractional balances and effective periods. `LeaveStore` consumes those DTOs directly. Both active hosts use the same scoped registration.
- Former-staff exports use typed scalar cells (`ExportReportDto`), preserving numeric values across JSON without exposing arbitrary report objects. The existing in-process export implementation remains available to current pages.

## Migrated page contract coverage

These pages now use the implemented HTTP clients and API backends. UI registration lives in `HRM.UI/Services/Transport/ApiClientRegistration.cs`; see [separate-host startup](separate-host-startup.md).

| Current UI | Contract for controller/UI migration |
| --- | --- |
| Employee/admin attendance request grids | `IAttendanceRequestQueries`; existing `IAttendanceRequestService` handles submission |
| Recent leave requests: states, reference, handover and stage metadata | `ILeaveRequestWorkflowQueries`; existing leave workflow contracts handle actions |
| Employee/admin organisation daily attendance | `IOrganisationAttendanceQueries`; existing attendance contracts provide detailed reports |
| Organisation structure/team lists and group/team detail panels | `IOrganisationDirectoryQueries`; canonical organisation/group/scope-schedule services handle mutations |
| Employee/admin out-of-office request grids | `IOutOfOfficeRequests` |
| Legacy Add New Job route | Existing `StaffOnboardingServiceContracts.IOperations` and `IIndividualRegistryService`; redirect to canonical onboarding rather than adding another writer |
| Current session and address/geography lookups | `ICurrentEmployeeSession`, `IEmployeeAddresses` |

The page-query contracts now have backend implementations and typed controllers; see `docs/api-controllers.md`. Their UI/client wiring remains part of the independent UI migration.

## Ownership, scope and concurrency

Personal operations derive identity from the verified session. Optional organisation or applicant-job selectors are filters/subjects, never permission grants; only authorised users may select another subject. `ExpectedStateId`, policy `Version` and existing audit concurrency identifiers are preserved. Server-generated audit identities, approvers and resulting balances continue to be determined by the business services.

Backend-only interfaces such as workflow engines, repositories, audit writers, context builders and arbitrary-user context/cache lookup remain internal service contracts. Their presence in Application is not permission to expose them through controllers. UI calendar/presentation state also remains outside the remote catalog.

## Verification

Run:

```powershell
dotnet run --project Tests/ApplicationContracts/ApplicationContracts.csproj
dotnet run --project Tests/AttendanceRefresh/AttendanceRefresh.csproj -p:WarningLevel=0 -- --application-contracts-only
dotnet run --project tools/ContractAudit/ContractAudit.csproj
```

The standalone contract tests reference Application only and validate catalog completeness, asynchronous signatures, recursively DTO-only/bounded payloads, assembly boundaries, local dates/times, fractional balances, versions and typed export cells. Regression tests exercise real DTO adapters and the OT policy contract against an isolated SQLite database, including stale writes and tenant-switch denial. Normal project verification runs these checks. Do not start either application or apply database migrations to verify contract changes.
