# DHR Attendance Connector — Enterprise Deployment Guide

## Architecture Overview

`DHR Attendance Connector` is a lightweight, cross-platform desktop and background service installed on an existing Windows, Linux, or macOS computer inside an organisation's office LAN.

- **No local SQL Server, IIS/web server, or organisation-hosted `AttendanceEdgeAgent` is required.**
- **Outbound-only HTTPS (TLS 1.2/1.3) with mTLS** to the centrally hosted DHR Attendance Gateway at the National Data Center.
- **Zero inbound ports or port forwarding** on the organisation's firewall.
- **Offline resilience**: All raw biometric attendance events are persisted in a local SQLite WAL transactional outbox before upload and retried automatically with exponential backoff + jitter.

## Supported Targets & OS Dependencies

| Target RID | OS Requirements | Packaging |
|---|---|---|
| `win-x64` | Windows 10 / 11 / Server 2019+ (x64) | PowerShell Windows Service installer / Inno Setup EXE |
| `linux-x64` | Debian 11+, Ubuntu 22.04+, RHEL 8+ (`libicu`, `libssl`, X11/Wayland for optional GUI) | DEB package + `systemd` service |
| `linux-arm64` | Debian/Ubuntu ARM64 (`libicu`, `libssl`) | DEB package + `systemd` service |
| `osx-arm64` / `osx-x64` | macOS 13+ | PKG + `launchd` daemon |

> **Self-contained publishing**: `.NET 8` runtime is bundled inside the published binaries; end users do not need to install .NET separately. Native OS libraries (`libicu`, OpenSSL on Linux) are still required.

## Building Distribution Packages

```powershell
# From repository root:
.\DHR.AttendanceConnector\installers\build-all.ps1 -Version 1.0.0 -Runtimes win-x64,linux-x64,linux-arm64,osx-arm64,osx-x64
```

This outputs self-contained archives and `SHA256SUMS.txt` under `DHR.AttendanceConnector\installers\dist\`.

### Code Signing (Production Requirement)

Packages generated without signing certificates are **unsigned** and must not be distributed to production organisations without signing:
- **Windows**: Sign `DHR.AttendanceConnector.Worker.exe`, `DHR.AttendanceConnector.App.exe`, and the Inno Setup installer using an EV/OV Authenticode certificate (`signtool sign /fd SHA256 /tr <timestamp-url>`).
- **Linux**: Sign DEB/RPM repository metadata with the DHR GPG release key.
- **macOS**: Set `DEVELOPER_ID_APP` and `DEVELOPER_ID_INSTALLER`, sign with hardened runtime, and notarise with `xcrun notarytool`.

## Installing on an Organisation Computer

### Windows
1. Download the signed Windows package from **DHR → Attendance → Connectors** (or `/admin/attendance/connectors`).
2. Verify the SHA-256 checksum against `SHA256SUMS.txt`.
3. Run the installer or PowerShell script as Administrator:
   ```powershell
   .\installers\windows\install-windows.ps1
   ```
4. The installer creates `%ProgramData%\DHR\AttendanceConnector` with restricted ACLs and registers the `DHRAttendanceConnector` Windows Service (Automatic startup + failure recovery).

### Linux (Debian/Ubuntu)
```bash
sudo dpkg -i dhr-attendance-connector_1.0.0_amd64.deb
sudo systemctl status dhr-attendance-connector
```

## Activating the Connector from Central DHR

1. In central DHR, an authorised administrator navigates to **Admin → Attendance → Device Connectors** (`/admin/attendance/connectors` or `/admin/settings/edge-agents`).
2. Open the **Activation Codes** tab, select the **Organisation**, enter the **Site Name**, optionally pre-select **Biometric Devices**, and click **Generate One-Time Code**.
3. Copy the single-use code (format: `DHR-ACT-XXXX-XXXX`, default 60-minute expiry).
4. On the organisation's computer, open **DHR Attendance Connector** (or run `DHR.AttendanceConnector.Worker activate https://dhr.gov.mv/ DHR-ACT-XXXX-XXXX`).
5. Enter the DHR HTTPS URL and activation code, then click **Activate Connector**.
6. The connector generates an RSA-2048 key pair locally (the private key never leaves the computer), submits a proof-of-possession CSR to `POST /api/attendance/connectors/enroll`, stores the issued mTLS client certificate in OS-protected storage, downloads its assigned biometric devices, and begins polling automatically.
