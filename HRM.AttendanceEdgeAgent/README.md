# DHR Attendance Edge Agent

Cross-platform local agent for Windows, Linux, or containers. It reads devices only on the organisation LAN and pushes batches outbound to DHR. Implement one `IDeviceAdapter` per vendor protocol. Enrolment must provide a device-scoped mTLS certificate; never use a staff eFaas token or expose device IPs to DHR. The agent sends a certificate-authenticated heartbeat after each poll, including when no attendance events exist.
