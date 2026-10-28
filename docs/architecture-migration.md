# HRM application architecture

## Current state — 28 October 2026

The four-project structure is implemented. API and UI are separate .NET 8 applications. Earlier checkpoints below describe the migration history and are superseded by this section.

| Project | Responsibility | Project references |
| --- | --- | --- |
| HRM.Core | Entities, enums and constants | None |
| HRM.Application | DTOs, interfaces, pure rules and reviewed route catalog | Core |
| HRM.Api | Controllers, business services, workflows, EF, migrations and background processing | Core, Application |
| HRM.UI | Blazor pages, assets, state, exports and authenticated HTTP clients | Application |

UI components inject Application interfaces. `AddHrmApiClients` binds reviewed interfaces to `ApiContractProxy`, which uses `HttpApiOperationClient` and typed `/api/v1/...` endpoints. The API binds those interfaces to backend implementations. UI has no DbContext, Entity Framework or API assembly dependency. Local presentation adapters also obtain their data through HTTP.

Components/assets, including isolated styles and PDF fonts, now live under HRM.UI. Direct database access has been removed from Razor. Legacy Add New Job redirects to staff enrolment. Team creation is an audited, tenant-authorised backend operation. Daily organisation attendance uses canonical schedules and attendance results.

`HRM.csproj` is now a build-only compatibility entry point. The former root Program and 39 superseded client wrappers have been removed. The solution contains the four application projects and tests. Applied migrations, embedded SQL names, table mappings and live data are unchanged.

The API owns eFaas/OIDC. The UI proxies fixed authentication routes and verifies identity through the API. Each server-side user session owns its cookie jar and CSRF pair; pooled handlers share neither cookies nor redirects. Claims and eFaas tokens remain in protected API tickets; browser cookies contain an opaque reference. Logout revokes both the API ticket and browser circuit sessions. Writes are not retried. Independent card reads can run concurrently. API permission, tenant, ownership and workflow checks remain authoritative. Platform administrator context uses the explicit database-grant policy. See [production security requirements](deployment-security.md).

The bounded UI session store is per process and expires after eight hours. Multi-instance deployments require Blazor session affinity. See [startup instructions](separate-host-startup.md) and the API and UI Visual Studio startup profile.

Tests use in-memory HTTP hosts and isolated databases; production Programs, listeners and accrual workers are not started. Real eFaas sign-in/callback/sign-out remains a deployment validation step.

Verified on 28 October 2026: solution and compatibility builds passed with zero warnings/errors; 37 API tests, 22 UI transport/authentication tests and all 48 regression groups passed. Application checks passed for 75 contracts / 337 declared operations, and the relocated CSS build passed. The shared route manifest contains 335 supported application actions, plus the two session endpoints. No live database or schema changes were made.

## Historical migration checkpoints

Requested target: match RefProject with HRM.Core, HRM.Application, HRM.Api and HRM.UI as four projects. API and UI must be independently running applications, with the UI calling backend operations over HTTP. Keep .NET 8, current eFaas authentication, tenant permission ceilings, request approval/state rules and historical database mappings/migrations.

## In progress

- Establish pure entity/enum/constants Core and transport-contract Application projects.
- Extract EF DbContexts, entity mappings, migrations, database scripts and business implementations into Api.
- Move Razor components/assets/presentation state into UI.
- Replace the 35 direct-DbContext Razor components with authorised typed API operations.
- Supply HTTP clients for application service contracts; authenticate API calls using the signed-in staff identity, with server-side tenant/permission enforcement.
- Keep existing public auth callback/logout routes through the UI boundary.
- Update tools, CI, migration discovery, styles and tests for the new project paths.

Do not launch the web application while verifying this migration: its background accrual worker can alter real balances. Do not migrate or modify database schema as part of an assembly/folder refactor. Build and test with isolated databases only. Preserve all applied migration source and existing namespaces where needed for migration/type compatibility. RefProject is a read-only reference.

## Verified extraction checkpoint (27 October 2026)

Core and Application compile independently. The temporary root host compiles against both and explicitly includes the relocated backend sources while the HTTP boundary is completed. Applied migration sources and embedded SQL resource names are preserved.

- 39 backend feature services now implement shared asynchronous contracts, with generated HTTP client implementations compiled by the bridge build.
- Typed HTTP payloads preserve declared parameter types (including nulls), overload selection and server cancellation. Unknown methods and invalid argument counts are rejected by contract descriptors.
- Individual registry, workspace search, workspace identity and personal attendance support have moved out of Razor database queries into scoped backend services. The registry requires HR permission; caller staff IDs only narrow workspace searches; personal attendance derives staff/organisation from authenticated context.
- Login group labels use the verified user context. Sixteen unused database/workflow injections have been removed.
- A 46-group regression run passed after the search and attendance extraction. Additional focused checks cover the HTTP proxy and workspace tenant override denial.

The independent HRM.Api project now compiles with zero warnings/errors, references only Core/Application, retains the existing eFaas configuration/user-secrets identity and includes the backend's EF mappings/migrations. Its HTTP-context authentication provider replaces circuit identity; its configured-public-origin navigation adapter supports existing audit services without a UI dependency. Cookie failures on /api return 401/403. The API has not been started. Core/Application/Api are listed in the solution alongside the temporary root bridge.

Still required: remaining 12 database-coupled components, existing-interface client registrations, explicit authorised API operation routing, independent UI host/public auth-route proxy, final physical UI move, final solution/tooling cleanup and final cross-host verification. The root host is a migration bridge, not the requested final deployment. Neither host has been launched and no production database operation is part of this checkpoint.

## Application contract checkpoint

The reviewed remote catalog now contains 74 DTO-only asynchronous interfaces / 330 operations. Entity-bearing feature contracts have explicit backend projections; unused legacy entity-based account writes have been removed from the remote interfaces. Remaining database-bound pages have typed contract coverage, documented in `docs/application-contracts.md`; their query implementations and controller/client wiring remain part of the API/UI migration. Personal leave application and user-data adapters reuse existing canonical services. A standalone Application-only contract suite is included in normal verification.

Verification for the Application contract checkpoint: standalone boundary/JSON checks passed; all 48 regression groups passed; the root bridge and independent API builds passed with zero warnings/errors. No web host was started and no live database or migration was changed.

## API controller checkpoint

Implemented 74 typed contract controllers / 329 supported actions, plus identity and CSRF endpoints. Added backend implementations for the page-query contracts, explicit session/CSRF/error handling and effective permission gates. Backend registration can be validated in isolation with background workers disabled. The new `HRM.Api.UnitTests` xUnit project covers HTTP and backend boundaries; no production host or live database was used. See `docs/api-controllers.md` for routes, request shapes, security checks, tests and remaining separate-UI/authentication work.

Final API checkpoint verification (27 October 2026): 31 xUnit API tests passed, all 48 existing regression groups passed, and Application-only contract checks passed for 74 interfaces / 331 operations. Both the independent API and temporary root host compile without warnings or errors. Production applications/background workers were not started; live database data and migrations were not changed. UI HTTP-client and separate-host authentication wiring are still pending.
