$ErrorActionPreference = 'Stop'
$taskRepository = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $taskRepository
try {
    python tools/check_deployment_configuration.py
    if ($LASTEXITCODE -ne 0) { throw 'Deployment configuration security checks failed.' }
    npm ci --ignore-scripts --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'CSS dependency restore failed.' }
    npm run build:css
    if ($LASTEXITCODE -ne 0) { throw 'CSS build failed.' }
    npm audit --audit-level=low
    if ($LASTEXITCODE -ne 0) { throw 'npm reported a vulnerability or could not complete its audit.' }
    dotnet restore HRM.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Locked .NET restore failed.' }
    $taskNugetOutput = dotnet list HRM.sln package --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) { throw 'NuGet advisory audit could not complete.' }
    $taskNugetAudit = $taskNugetOutput | ConvertFrom-Json
    foreach ($taskProject in $taskNugetAudit.projects) {
        foreach ($taskFramework in $taskProject.frameworks) {
            foreach ($taskPackage in @($taskFramework.topLevelPackages) + @($taskFramework.transitivePackages)) {
                if ($taskPackage -and $taskPackage.vulnerabilities.Count -gt 0) { throw ('NuGet advisory found for ' + $taskPackage.id) }
            }
        }
    }
    dotnet build HRM.sln --no-restore -p:EnableNETAnalyzers=true -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Application build or analyzer checks failed.' }
    dotnet run --project Tests/ApplicationContracts/ApplicationContracts.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Application contract boundary checks failed.' }
    dotnet restore Tests/HRM.Api.UnitTests/HRM.Api.UnitTests.csproj --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'API unit-test dependency restore failed.' }
    dotnet test Tests/HRM.Api.UnitTests/HRM.Api.UnitTests.csproj --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'API unit and endpoint checks failed.' }
    dotnet test Tests/HRM.UI.UnitTests/HRM.UI.UnitTests.csproj --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'UI transport and authentication checks failed.' }
    dotnet run --project Tests/AttendanceRefresh/AttendanceRefresh.csproj -p:WarningLevel=0 --
    if ($LASTEXITCODE -ne 0) { throw 'Regression checks failed.' }
    python tools/audit_project.py
    if ($LASTEXITCODE -ne 0) { throw 'Reference inventory failed.' }
    Write-Output 'Dependency audits, regression checks and source inventory completed successfully.'
}
finally { Pop-Location }
