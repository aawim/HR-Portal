#Requires -RunAsAdministrator
param(
    [string]$InstallDir = "$env:ProgramFiles\DHR\AttendanceConnector",
    [string]$SourceDir = "$PSScriptRoot\..\dist\win-x64"
)

$ErrorActionPreference = "Stop"
$serviceName = "DHRAttendanceConnector"
$dataDir = Join-Path $env:ProgramData "DHR\AttendanceConnector"

Write-Host "Installing DHR Attendance Connector to $InstallDir ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null

# Lock down ProgramData directory ACLs (Administrators + SYSTEM + LocalService only)
icacls $dataDir /inheritance:r /grant:r "SYSTEM:(OI)(CI)F" "Administrators:(OI)(CI)F" "LOCAL SERVICE:(OI)(CI)M" | Out-Null

Copy-Item -Path "$SourceDir\worker\*" -Destination $InstallDir -Recurse -Force
Copy-Item -Path "$SourceDir\app\*" -Destination $InstallDir -Recurse -Force

$workerExe = Join-Path $InstallDir "DHR.AttendanceConnector.Worker.exe"
if (-not (Test-Path $workerExe)) {
    throw "Worker executable not found at $workerExe. Run installers/build-all.ps1 -Runtimes win-x64 first."
}

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing) {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

New-Service -Name $serviceName `
    -BinaryPathName "`"$workerExe`"" `
    -DisplayName "DHR Attendance Connector" `
    -Description "Collects local biometric attendance logs and securely synchronises them outbound to DHR." `
    -StartupType Automatic | Out-Null

sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Null
Start-Service -Name $serviceName

Write-Host "DHR Attendance Connector installed and background service started." -ForegroundColor Green
Write-Host "Launch '$InstallDir\DHR.AttendanceConnector.App.exe' to activate with your one-time DHR activation code." -ForegroundColor Cyan
