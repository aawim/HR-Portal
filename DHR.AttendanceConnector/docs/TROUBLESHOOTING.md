# DHR Attendance Connector — Troubleshooting & Recovery Guide

## 1. Connector Status States

| Status | Meaning | Recommended Action |
|---|---|---|
| **Connected to DHR** | mTLS authenticated, heartbeats succeeding, queue healthy. | None. |
| **Registration Required** | No local profile or mTLS certificate installed. | Generate a one-time activation code in DHR and activate. |
| **Offline** | Cannot reach DHR over HTTPS. | Verify outbound TCP 443 to DHR. Local LAN polling continues automatically; events are safely buffered in SQLite. |
| **Authentication Error** | Client certificate expired, revoked, or untrusted. | Click **Rotate mTLS Certificate** in Settings, or if revoked/expired, generate a new activation code and re-activate. |
| **Synchronisation Delayed** | High local backlog (500+ events) or quarantined items. | Check DHR connectivity in Diagnostics, then click **Retry Pending Records**. |

## 2. Biometric Device Offline or Timeout

1. Open **Device Connections** and click **Test Connectivity** on the affected device.
2. Verify the office computer can reach the device IP and TCP port (default `4370` for ZKTeco) on the local LAN.
3. Confirm the device IP/port matches the configuration assigned in **DHR → Attendance → Device Connectors**.
4. Device-log cleanup is disabled by default; no punches are ever deleted from biometric terminals by the connector.

## 3. Crash / Power-Loss Recovery

- The local SQLite database runs in `WAL` mode with `synchronous=FULL`.
- On startup, `AttendanceSyncService.RecoverStaleUploadingEventsAsync` automatically returns any events interrupted mid-upload (`Uploading`) back to `PendingUpload`.
- Central DHR ingestion (`EA_EventReceipts`) enforces idempotency on `(AttendanceDeviceId, DeviceEventId)` and SHA-256 `EventHash`, so retried batches never create duplicate attendance logs.

## 4. Exporting Sanitised Diagnostics

- In the desktop UI, go to **Diagnostics → Export Sanitised Diagnostic Package** (or run `DHR.AttendanceConnector.Worker diagnostics`).
- The generated ZIP archive contains connector status, sync metrics, device health, configuration history, and recent logs.
- **Private keys, full certificates, and activation codes are never included.**

## 5. Replacing a Damaged Computer

1. In central DHR (**Admin → Attendance → Device Connectors**), locate the damaged connector and click **Revoke** immediately.
2. Install DHR Attendance Connector on the replacement computer.
3. Generate a new one-time activation code in DHR, assign the same biometric devices, and activate the replacement connector.
