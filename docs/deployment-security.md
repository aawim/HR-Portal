# Deployment security — 28 October 2026

## Actions required before production

1. **Rotate the eFaas client secret at the provider.** Git history contains a non-placeholder client secret in `appsettings.json` (commit `1c90e46`; also present before `17eba80`). It matches the current development secret. Removing a secret from current files does not revoke it. Replace it in the development user-secret store and the production secret manager after provider rotation. Do not paste it into chat, commit it, or put it in shell command history. Production startup rejects the known exposed credential using a public SHA-256 fingerprint; the actual value is not stored in source or this report.
2. **Update the hosting runtime.** This workstation has .NET/ASP.NET Core 8.0.16. Both production hosts now require .NET and ASP.NET Core 8.0.33 or a newer .NET 8 servicing patch. Install the current Hosting Bundle/runtime and restart the applications during deployment. Developer builds/tests can still run on the existing runtime; no system runtime was installed or production process restarted during this audit.
3. Configure the real public UI origin, private API origin, eFaas registration, permitted hosts and protected key storage. The registered callback must exactly match the production public origin plus `/Online/Auth/Callback`.

Microsoft's [25 October security release](https://devblogs.microsoft.com/dotnet/dotnet-october-2026-oob-security-updates/) identifies 8.0.33 as the .NET 8 servicing update. Microsoft also documents the [unknown-proxy header hardening introduced in 8.0.17](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/8/forwarded-headers-unknown-proxies). A test reproduced the older behavior locally; HRM now explicitly strips forwarded headers from unknown addresses before framework processing.

## Configuration

Base appsettings files deliberately contain no deployment origins or credentials. Localhost addresses and the development eFaas registration are confined to `appsettings.Development.json` and development launch profiles. Application page/API paths are relative routes and do not embed the production hostname. Each host is deployed at its own origin root; a virtual-directory deployment is not supported by the current route design.

Set the following through the deployment environment or secret-manager integration. ASP.NET configuration uses `__` instead of `:` in environment variable names:

| Setting | API | UI |
| --- | --- | --- |
| `Application__PublicOrigin` | Public UI HTTPS origin | Same public UI HTTPS origin |
| `Backend__Origin` | — | Private API HTTPS origin |
| `AllowedHosts` | Public UI and private API hostnames, separated by semicolons | Public UI hostname |
| `Authentication__OpenIdConnect__Authority` | Registered eFaas HTTPS authority | — |
| `Authentication__OpenIdConnect__ClientId` | Registered application ID; not a secret | — |
| `Authentication__OpenIdConnect__ClientSecret` | Rotated provider credential, from secret manager | — |
| `ConnectionStrings__DefaultConnection` / `ConnectionStrings__AuditConnection` | Database connections; managed identity/integrated authentication or secret manager | — |
| `DataProtection__KeysPath` | Absolute persistent directory outside API application files | Separate absolute persistent directory outside UI application files |
| `DataProtection__CertificatePath` / `DataProtection__CertificatePassword` | Private encryption certificate outside application files; password from secret manager | Equivalent UI key protection |
| `ReverseProxy__KnownProxies__0`, etc. | UI proxy IP / actual ingress IP as appropriate | Actual load-balancer/ingress IP if applicable |

On Windows, omitting the encryption certificate uses DPAPI bound to the service identity. Keep the service identity stable and grant only it access to the key directory. For IIS, configure the application-pool profile/key permissions appropriately. On other platforms a valid private certificate is required for file-based key persistence. Keys and certificates must remain outside served/source directories, with restricted filesystem permissions. Configured origins require HTTPS; production cannot fall back to localhost, and wildcard `AllowedHosts` are rejected. The compatibility `UiProxy:KnownProxies` setting is still accepted.

Development user secrets remain outside the repository under the existing API secrets ID. They are a development convenience, not a production vault. The UI receives no eFaas client secret or database connection.

## Authentication and API boundary

- The API owns eFaas OpenID Connect authorization-code login with PKCE, HTTPS metadata, signed ID-token validation, issuer/audience/lifetime checks, nonce and correlation validation.
- Protocol callback/logout URLs come from the configured public origin, not untrusted request headers.
- The `__Host-HRM.Auth` cookie is Secure, HttpOnly, root-path and has no Domain attribute. It contains only a protected opaque reference. Claims and eFaas tokens are stored in protected server-side tickets with an eight-hour maximum lifetime.
- Logout revokes the ticket at the API. Replaying the previous browser cookie fails; a concurrent sliding renewal cannot resurrect a revoked session.
- API controllers and the fallback policy require authentication. Identity-only endpoints still require a valid eFaas-authenticated session even when no local employee exists. Business APIs additionally enforce employee status, effective permissions, tenant boundaries, ownership and workflow rules. Writes retain antiforgery validation.
- This is the existing server-side UI/session model. It does not accept arbitrary bearer tokens or a custom API key as a substitute for eFaas authentication. A separate OAuth resource-server integration would require an explicitly configured audience/scopes and JWT bearer scheme.
- UI HTTP handlers share neither cookie jars nor redirects, preserve per-user CSRF pairs, validate TLS certificates and restrict destinations to the configured API origin.

The API ticket store currently uses in-memory `IDistributedCache`; restarting the API signs users out. Before deploying multiple API instances, register a shared `IDistributedCache` implementation and share the protected API key ring/certificate. UI instances require Blazor circuit/session affinity. Do not disable proxy trust, HTTPS certificate validation or key encryption to make a deployment work.

## Audit scope and verification

The current tracked JSON configuration scan found no nonempty secrets or private-key files. Historical secret exposure was checked without printing the credential, and compared to this application's development store in memory only. NuGet advisory scanning reported no vulnerable packages in the seven solution projects at audit time; this does not cover installed shared runtimes, which were checked separately.

Tests use ephemeral protection keys, TestServer and isolated databases. They exercise configuration failures, explicit proxy trust, fixed OIDC callback destinations, authentication fallback, forged headers/tokens, protected server-side tickets, expiry/revocation, opaque cookies, CSRF and tenant/workflow checks. No production host, background worker or live database was started. Real eFaas sign-in/callback/logout and deployed certificate/secret-manager settings must be verified after production configuration and credential rotation.

Final checks: 61 API/security tests, 30 UI tests and all 48 business regression groups passed. Solution builds have zero warnings/errors. The standalone Application contract checks and current-configuration secret guard passed; dependency auditing reported no NuGet advisories in the seven solution projects. Provider credential rotation and the system runtime update remain external deployment requirements.
