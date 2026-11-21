#Requires -RunAsAdministrator
param(
    [string]$InstallDir = "$env:ProgramFiles\DHR\AttendanceConnector",
    [switch]$PurgeLocalData
)

$ErrorActionPreference = "Stop"
$serviceName = "DHRAttendanceConnector"
$dataDir = Join-Path $env:ProgramData "DHR\AttendanceConnector"

$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existing) {
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName | Out-Null
    Start-Sleep -Seconds 2
}

if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force
}

if ($PurgeLocalData -and (Test-Path $dataDir)) {
    Remove-Item -Path $dataDir -Recurse -Force
    Write-Host "Local SQLite queue and secrets purged." -ForegroundColor Yellow
} else {
    Write-Host "Local data preserved at $dataDir (pass -PurgeLocalData to remove)." -ForegroundColor Cyan
}

Write-Host "DHR Attendance Connector uninstalled." -ForegroundColor Green
