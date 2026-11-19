using DHR.AttendanceConnector.Core.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.Infrastructure.Persistence;

public sealed class SqliteConnectorDatabase
{
    public string DatabasePath { get; }
    public string ConnectionString { get; }

    public SqliteConnectorDatabase(IOptions<ConnectorOptions> options)
    {
        var dataDir = options.Value.ResolveDataDirectory();
        Directory.CreateDirectory(dataDir);
        DatabasePath = Path.Combine(dataDir, "connector.db");
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new SqliteConnection(ConnectionString);
        await conn.OpenAsync(ct);

        await using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        await pragma.ExecuteNonQueryAsync(ct);
        return conn;
    }

    public async Task InitializeSchemaAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ConnectorConfiguration (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                ConnectorId INTEGER NOT NULL,
                AgentIdentifier TEXT NOT NULL,
                OrganisationId INTEGER NOT NULL,
                OrganisationName TEXT NOT NULL,
                SiteName TEXT NULL,
                ApiBaseUrl TEXT NOT NULL,
                CertificateThumbprint TEXT NOT NULL,
                CertificateNotAfterUtc TEXT NOT NULL,
                ConfigurationVersion INTEGER NOT NULL,
                DefaultPollingIntervalSeconds INTEGER NOT NULL,
                SyncBatchSize INTEGER NOT NULL,
                HeartbeatIntervalSeconds INTEGER NOT NULL,
                RetryBaseDelaySeconds INTEGER NOT NULL,
                RetryMaxDelaySeconds INTEGER NOT NULL,
                ActivatedAtUtc TEXT NOT NULL,
                LastConfigSyncAtUtc TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS RegisteredDevices (
                DeviceId INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                IpAddress TEXT NOT NULL,
                Port INTEGER NOT NULL,
                ProtocolCode TEXT NOT NULL,
                PollingIntervalSeconds INTEGER NOT NULL,
                Enabled INTEGER NOT NULL,
                LastConnectionStatus TEXT NULL,
                LastSuccessfulPollUtc TEXT NULL,
                LastEventTimestampUtc TEXT NULL,
                LastError TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS RawAttendanceEvents (
                LocalId INTEGER PRIMARY KEY AUTOINCREMENT,
                ConnectorId TEXT NOT NULL,
                DeviceId INTEGER NOT NULL,
                DeviceUserId TEXT NOT NULL,
                DeviceTimestampUtc TEXT NOT NULL,
                DeviceEventId TEXT NOT NULL,
                PunchStatus TEXT NULL,
                SourceProtocol TEXT NOT NULL,
                RawMetadata TEXT NULL,
                IngestedAtUtc TEXT NOT NULL,
                DeduplicationKey TEXT NOT NULL UNIQUE,
                State TEXT NOT NULL DEFAULT 'PendingUpload',
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT NULL,
                LastError TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_RawAttendanceEvents_State_NextRetry ON RawAttendanceEvents (State, NextRetryAtUtc);
            CREATE INDEX IF NOT EXISTS IX_RawAttendanceEvents_Device_Timestamp ON RawAttendanceEvents (DeviceId, DeviceTimestampUtc);

            CREATE TABLE IF NOT EXISTS DeviceSyncCheckpoints (
                DeviceId INTEGER PRIMARY KEY,
                LastEventId TEXT NULL,
                LastEventTimestampUtc TEXT NULL,
                TotalEventsCollected INTEGER NOT NULL DEFAULT 0,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS OutboundSyncQueue (
                QueueId INTEGER PRIMARY KEY AUTOINCREMENT,
                RawEventLocalId INTEGER NOT NULL UNIQUE,
                BatchId TEXT NULL,
                State TEXT NOT NULL DEFAULT 'PendingUpload',
                ClaimedAtUtc TEXT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT NULL,
                LastError TEXT NULL,
                FOREIGN KEY (RawEventLocalId) REFERENCES RawAttendanceEvents(LocalId)
            );
            CREATE INDEX IF NOT EXISTS IX_OutboundSyncQueue_State_NextRetry ON OutboundSyncQueue (State, NextRetryAtUtc);
            CREATE INDEX IF NOT EXISTS IX_OutboundSyncQueue_BatchId ON OutboundSyncQueue (BatchId);

            CREATE TABLE IF NOT EXISTS SyncAcknowledgements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                BatchId TEXT NOT NULL,
                AcceptedCount INTEGER NOT NULL,
                DuplicateCount INTEGER NOT NULL,
                RejectedCount INTEGER NOT NULL,
                AcknowledgedAtUtc TEXT NOT NULL,
                ServerMessage TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_SyncAcknowledgements_AckAt ON SyncAcknowledgements (AcknowledgedAtUtc DESC);

            CREATE TABLE IF NOT EXISTS AgentCommands (
                CommandId INTEGER PRIMARY KEY,
                CommandType TEXT NOT NULL,
                PayloadJson TEXT NULL,
                Status TEXT NOT NULL,
                ReceivedAtUtc TEXT NOT NULL,
                ExecutedAtUtc TEXT NULL,
                ResultJson TEXT NULL,
                ErrorMessage TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS ConnectorHealthLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                Level TEXT NOT NULL,
                Component TEXT NOT NULL,
                Message TEXT NOT NULL,
                Detail TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_ConnectorHealthLogs_Timestamp ON ConnectorHealthLogs (TimestampUtc DESC);

            CREATE TABLE IF NOT EXISTS ConfigurationVersions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VersionNumber INTEGER NOT NULL,
                ConfigHash TEXT NOT NULL,
                AppliedAtUtc TEXT NOT NULL,
                DeviceCount INTEGER NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(ct);

        // Restrict file permissions on Unix
        if (!OperatingSystem.IsWindows() && File.Exists(DatabasePath))
        {
            try { File.SetUnixFileMode(DatabasePath, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
            catch { /* ignore on filesystems without POSIX mode */ }
        }
    }
}
