# Run HRM API and UI

Start both projects in separate terminals, or select the **API and UI** profile in Visual Studio:

```powershell
dotnet run --project HRM.Api/HRM.Api.csproj
dotnet run --project HRM.UI/HRM.UI.csproj
```

Open **https://localhost:62839/**. The API listens on **https://localhost:62840/**. `HRM.csproj` builds the hosts for compatibility but is not executable.

Existing development user secrets are retained by the API through the existing secrets ID. The API requires the main/audit connection strings and eFaas client secret. Do not put secrets in the UI or commit them to appsettings. The UI needs only `Backend:Origin` and `Application:PublicOrigin`.

Keep `Application:PublicOrigin` identical in both hosts. The registered eFaas callback remains `https://localhost:62839/Online/Auth/Callback`. Login, callback and protected logout requests are forwarded through the UI to the API. Existing root-host authentication cookies may require a fresh sign-in after the host change.

Use trusted HTTPS certificates for both hosts. For deployment, set the public and private origins explicitly, keep the API on the private network, and configure `UiProxy:KnownProxies` with the UI proxy IP when it is not loopback. Only the configured public host and known proxies are trusted; never clear proxy trust restrictions. The per-process UI session store requires Blazor session affinity for multiple instances.

## Checks without starting production

```powershell
dotnet restore HRM.sln --locked-mode
dotnet build HRM.sln --no-restore -warnaserror
dotnet test Tests/HRM.Api.UnitTests/HRM.Api.UnitTests.csproj --no-restore
dotnet test Tests/HRM.UI.UnitTests/HRM.UI.UnitTests.csproj --no-restore
dotnet run --project Tests/ApplicationContracts/ApplicationContracts.csproj
dotnet run --project Tests/AttendanceRefresh/AttendanceRefresh.csproj -p:WarningLevel=0
npm run build:css
```

These tests do not invoke production Programs, connect to the live database or start the accrual worker. TestServer checks cover API-verified UI identity, cookie/CSRF isolation, logout forwarding, forged identities, typed routes, DI registrations, backend scopes and workflow regressions. Perform a real eFaas sign-in/callback/logout check when deploying; the external identity provider is not mocked as a complete OIDC server.
