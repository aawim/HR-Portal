# Project cleanup and security audit — 25 October 2026

## Scope and findings

Reviewed the application source inventory, routes/controllers, authentication setup,
database configuration, EF model discovery, service access checks, raw SQL usage,
browser script loading, and direct/transitive package advisories. Reference scanning
is a conservative heuristic, not proof that every possible reflection-driven use is absent.
EF-mapped models, reflected `IEntityTypeConfiguration` classes, and migration history
were retained. No production data or database schema was changed.

The [removal manifest](cleanup-removed-files.md) records 78 obsolete files removed.
Legacy leave URLs now redirect to their canonical administration/workspace pages.

### Fixed

- Removed the duplicate unauthenticated leave-type CRUD API, its duplicate editor,
  demo profile endpoint/model/view, unrestricted resolver pages, and unused test services.
- Corrected the inverted super-admin check in both the leave-type UI and service.
  Writes require authorised HR/admin access; tenant records cannot be edited by changing an ID.
- Added shared owner/current-tenant checks to legacy attendance, leave-balance,
  job-history and position-history reads. Removed synchronous `.Result` access in position lookup.
- Employee clock writes verify the authenticated employee and active approved job.
  Timestamps come from the server in Maldives time; clock events receive a real audit record.
- Custom clock times require **all** of Development mode, `Security:AllowTestClock=true`,
  and Developer User Group membership. The setting defaults to disabled.
- Logout POST validates antiforgery tokens. Legacy GET sign-out links only display a
  confirmation form and do not log users out. Organisation API endpoints require authentication
  and check the required permission/organisation scope.
- Detailed circuit errors are restricted to Development; common unexpected UI failures
  show generic messages instead of database/internal exception text.
- Authentication cookies require HTTPS and HttpOnly; OIDC explicitly requires PKCE
  and HTTPS metadata. Added frame, content-type and referrer response protections.
- Removed unused session middleware and duplicate service registrations.
- Removed the DbContext's hard-coded connection override. Configured providers are
  preserved, including isolated test providers; unconfigured contexts fail safely.
- Development database connections and the eFaas client secret were moved to local
  .NET user-secrets. Tracked settings contain empty values. Production database
  configuration must encrypt traffic and validate the SQL Server certificate.
- Pinned `System.Security.Cryptography.Pkcs` 8.0.2 to fix the two advisories found in
  transitive version 8.0.1. Package lock files are checked in; NuGet advisory warnings fail restore.
- Replaced the remote Tailwind JavaScript runtime with generated local CSS and a
  locked CSS toolchain. The CSS parser is pinned to patched 7.1.6 through an override.

## Verification

- A fresh application rebuild with security analyzers enabled succeeded with **zero
  compiler/Razor/analyzer warnings and zero errors**. Removed unused fields and fixed
  null handling, unawaited form refreshes and the missing profile section content contract.
  The verification script treats application build warnings as errors.
- All **26 regression groups** passed using isolated in-memory/SQLite databases.
  New security coverage checks retired routes, logout antiforgery, provider isolation,
  clock ownership and diagnostic restrictions, leave-type privilege denial, and tenant boundaries.
- Updated older attendance fixtures for separate duty attendance, explicit request tabs,
  canonical attendance DTOs and current export dependencies; retained the security assertions.
- NuGet direct/transitive advisory audit: **zero reported vulnerabilities** after patching.
- npm audit, including development tools: **zero reported vulnerabilities** after patching.
- Reference inventory: **zero remaining review candidates** after cleanup.
- Generated local Tailwind CSS successfully. Browser visual regression testing and a
  production penetration test are not included in these checks.

## Required deployment actions

1. **Rotate the eFaas client secret.** It previously existed in tracked configuration;
   removing it from the working tree does not revoke it or erase Git history. No history
   rewrite or identity-provider credential rotation was performed.
2. **Update the hosting machine's .NET/ASP.NET Core runtime to the latest serviced
   .NET 8 release** before deployment. The inspected host has 8.0.16 installed. Package
   audits do not assess or patch the shared runtime, OS, IIS, SQL Server or certificates.
3. Configure deployment secrets through the host's secret store/environment:
   `Authentication__OpenIdConnect__ClientSecret`, `ConnectionStrings__DefaultConnection`,
   and `ConnectionStrings__AuditConnection`. Use encrypted SQL connections with
   `TrustServerCertificate=False` and a trusted server certificate. Set `AllowedHosts`
   to the actual deployment hostname(s), and keep Production mode enabled.
4. Development launch profiles continue to use Development mode and the preserved local
   user-secrets. EF tools use `ASPNETCORE_ENVIRONMENT=Development` to load those local connections.
   Other developers must supply their own local configuration; secrets are not shared through Git.
5. After changing Razor/CSS sources, run `npm ci --ignore-scripts` and `npm run build:css`
   and include `wwwroot/css/tailwind.css` in the reviewed change.

These results establish the fixes and test outcomes above; they are not a guarantee
that the application or its hosting environment contains no security vulnerabilities.

## Reproduce

```powershell
./tools/verify-project.ps1
```

The verification script does not run one-off operations or modify production data.

## References

- [Microsoft PKCS advisory: CVE-2026-84008](https://github.com/advisories/GHSA-846f-f764-76mx)
- [Microsoft PKCS advisory: CVE-2026-70162](https://github.com/advisories/GHSA-2fmv-rv2c-59x7)
- [CSS parser advisory](https://github.com/advisories/GHSA-rj75-hqrm-r3gf)
- [ASP.NET Core Blazor security](https://learn.microsoft.com/aspnet/core/blazor/security/?view=aspnetcore-8.0)
