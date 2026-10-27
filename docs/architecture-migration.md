# HRM architecture migration

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

The reviewed remote catalog now contains 74 DTO-only asynchronous interfaces / 329 operations. Entity-bearing feature contracts have explicit backend projections; unused legacy entity-based account writes have been removed from the remote interfaces. Remaining database-bound pages have typed contract coverage, documented in `docs/application-contracts.md`; their query implementations and controller/client wiring remain part of the API/UI migration. Personal leave application and user-data adapters reuse existing canonical services. A standalone Application-only contract suite is included in normal verification.

Verification for the Application contract checkpoint: standalone boundary/JSON checks passed; all 48 regression groups passed; the root bridge and independent API builds passed with zero warnings/errors. No web host was started and no live database or migration was changed.
