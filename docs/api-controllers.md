# API controllers and backend services

## Implemented surface

Versioned controllers live in `HRM.Api/Controllers/V1`. There are 74 contract controllers with 329 supported application actions, plus the session controller's identity and antiforgery endpoints (331 versioned HTTP endpoints). Two obsolete account/clock aliases are deliberately not exposed: callers must use the canonical account settings and clock-event operations.

The route inventory is `docs/api-endpoints.json` (contract actions). Controllers are strongly typed and delegate to the canonical services; they do not execute arbitrary CLR methods, raw client-supplied queries or SQL. Read operations with scalar arguments use GET; reads requiring a structured filter use POST under `query/`; mutations use POST. POST bodies wrap the named contract arguments (for example `{ "model": { ... }, "applicantJobId": null }`).

Example endpoints:

- `GET /api/v1/session/identity`: authenticated identity without tokens/cookies; works for eFaas users without a local employee record.
- `GET /api/v1/current-employee-session/get`: verified local employee context, if available.
- `GET /api/v1/session/antiforgery`: paired CSRF token and cookie.
- `GET /api/v1/attendance-request/get-date-info?date=2026-10-27`.
- `POST /api/v1/attendance-request/submit` with `{ "model": { "date": "2026-10-27", "time": "08:30:00", "inOutModeId": 1, "comments": "Correction reason" } }`.
- `POST /api/v1/attendance-request-queries/query/get-mine` with `{ "filter": { "from": "2026-10-01", "to": "2026-10-31" } }`.
- `POST /api/v1/leave-applications/submit-leave` delegates to the existing leave workflow.

## Security and scope

All versioned controllers require the cookie authentication scheme explicitly. Anonymous API calls receive 401 rather than an eFaas HTML redirect. Non-session actions also require a verified local user. Unsafe methods require the CSRF token in `X-CSRF-TOKEN` together with its matching cookie and authenticated identity. Responses are not cached. Error responses contain safe Problem Details and a trace identifier rather than exception details.

Tenant selectors are interpreted by backend scope checks, not caller headers or group labels. Ordinary staff stay in their current organisation; only the server-verified Super Administrator may select another. Personal requests are bound to the current employee. Received-request queries require a current workflow assignment or the relevant effective viewing permission. Administrative lookup endpoints and legacy group-only configuration gates also enforce effective permissions; settings without a dedicated fine-grained gate require `Admin`. Existing business services still enforce their own organisation ceilings, authorisation, workflow transitions, concurrency and final-state rules.

Organisation/user/profile/staff lookup services now enforce backend scope rather than relying on their calling page. User context construction reads its own trusted organisation assignment directly, avoiding a dependency cycle between context construction and the authorised organisation service.

## Newly implemented queries

- Current employee session and address/geography DTOs.
- Personal and received attendance requests, including uneven-log corrections, status and date filters.
- Personal leave reference/status/handover/workflow-stage summaries, with separate leave-record and workflow-state lookups so filter IDs cannot be conflated.
- Organisation structure, teams, group/team membership history, schedules and leave sets.
- Organisation daily attendance, using canonical schedules and eligibility rather than fixed start/end hours.
- Out-of-office options, scoped lists, editable legacy drafts, workflow submission and approve/reject/cancel actions.

Out-of-office categories are separate from the central workflow request type: the submitted category is validated against `OutOfficeRequestTypes`, while central requests use the established type 50. Subject employment is recorded in the existing RequestJobs link table. New requests start the configured sequential workflow; actions use the shared workflow processor and allowed state transitions. Applicants cannot approve/reject their own requests, including Super Administrators. Completed workflows cannot be overwritten. Legacy drafts are editable only while Incomplete and before a workflow exists; active or ambiguous legacy workflows need reconciliation rather than inferred employment links.

## Registration and testing

`AddHrmBackendServices` is shared by API startup and dependency-resolution tests. `includeBackgroundWorkers: false` is reserved for isolated tests; production retains the existing accrual worker. `AddHrmApiEndpoints` registers endpoint filters, safe transport serialization, typed contract bindings and new query services. The temporary root host also registers the endpoints without replacing its interactive authentication provider.

The new xUnit project is `Tests/HRM.Api.UnitTests/HRM.Api.UnitTests.csproj`. Run:

```powershell
dotnet test Tests/HRM.Api.UnitTests/HRM.Api.UnitTests.csproj
```

Tests use an isolated TestServer, ephemeral data-protection keys and in-memory databases. They never execute the production API Program, open a listener, run accrual workers or connect to the live database. They cover all contract/controller registrations and unique routes, anonymous/missing sessions, paired CSRF validation, cross-identity token rejection, malformed input, safe errors, effective permission denial, tenant boundaries, request ownership, missing-schedule eligibility and out-of-office workflow delegation/self-approval denial. The mocked workflow submission test does not claim to verify SQL Server transaction rollback; real workflow execution still uses the existing transactional SQL implementation.

The new project is included in the solution and normal verification script. Verified on 27 October 2026: 31 xUnit tests, all 48 existing regression groups and Application-only contract checks passed. The temporary Blazor host also seeds each MVC request’s scoped authentication state from the verified HTTP principal without modifying any circuit scope.

## Remaining architecture work

The existing pages still call their current services. The generated HTTP clients still use the earlier transport abstraction and must be connected to these typed routes while completing `HRM.UI`. Separate-host eFaas callback/cookie forwarding, public URL handling and final API/UI end-to-end verification remain part of that UI/authentication migration. Do not start both production hosts simply to test the controller layer, because the accrual worker can modify live balances. No schema migration is required for this change.
