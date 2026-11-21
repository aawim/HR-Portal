param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string[]]$Runtimes = @("win-x64", "linux-x64", "linux-arm64", "osx-arm64", "osx-x64"),
    [string]$OutputRoot = "$PSScriptRoot\dist"
)

$ErrorActionPreference = "Stop"
$solutionRoot = Split-Path -Parent $PSScriptRoot
$workerProj = Join-Path $solutionRoot "DHR.AttendanceConnector.Worker\DHR.AttendanceConnector.Worker.csproj"
$appProj    = Join-Path $solutionRoot "DHR.AttendanceConnector.App\DHR.AttendanceConnector.App.csproj"

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
$checksumLines = @()

foreach ($rid in $Runtimes) {
    Write-Host "=== Publishing DHR Attendance Connector v$Version for $rid ===" -ForegroundColor Cyan
    $ridDir = Join-Path $OutputRoot $rid
    $workerOut = Join-Path $ridDir "worker"
    $appOut    = Join-Path $ridDir "app"

    dotnet publish $workerProj -c $Configuration -r $rid --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$Version -o $workerOut

    dotnet publish $appProj -c $Configuration -r $rid --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:Version=$Version -o $appOut

    $zipPath = Join-Path $OutputRoot "DHR-Attendance-Connector-$Version-$rid.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$ridDir\*" -DestinationPath $zipPath

    $hash = (Get-FileHash -Algorithm SHA256 -Path $zipPath).Hash
    $checksumLines += "$hash  $(Split-Path -Leaf $zipPath)"
    Write-Host "Created $zipPath (SHA256: $hash)" -ForegroundColor Green
}

$checksumFile = Join-Path $OutputRoot "SHA256SUMS.txt"
$checksumLines | Set-Content -Path $checksumFile -Encoding UTF8
Write-Host "Build complete. Checksums written to $checksumFile" -ForegroundColor Green
Write-Host "NOTE: Packages produced without a code-signing certificate are UNSIGNED and must be signed before production distribution." -ForegroundColor Yellow
