using System.Globalization;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace DHR.AttendanceConnector.Infrastructure.Repositories;

public sealed class SqliteConnectorRepository(SqliteConnectorDatabase database) : IConnectorRepository
{
    public Task InitializeAsync(CancellationToken ct = default) => database.InitializeSchemaAsync(ct);

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = await database.OpenConnectionAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1;";
            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result) == 1;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ConnectorProfile?> GetProfileAsync(CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT ConnectorId, AgentIdentifier, OrganisationId, OrganisationName, SiteName, ApiBaseUrl,
                   CertificateThumbprint, CertificateNotAfterUtc, ConfigurationVersion,
                   DefaultPollingIntervalSeconds, SyncBatchSize, HeartbeatIntervalSeconds,
                   RetryBaseDelaySeconds, RetryMaxDelaySeconds, ActivatedAtUtc, LastConfigSyncAtUtc
            FROM ConnectorConfiguration WHERE Id = 1;
            """;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new ConnectorProfile(
            ConnectorId: r.GetInt32(0),
            AgentIdentifier: r.GetString(1),
            OrganisationId: r.GetInt32(2),
            OrganisationName: r.GetString(3),
            SiteName: r.IsDBNull(4) ? null : r.GetString(4),
            ApiBaseUrl: r.GetString(5),
            CertificateThumbprint: r.GetString(6),
            CertificateNotAfterUtc: ParseUtc(r.GetString(7)),
            ConfigurationVersion: r.GetInt32(8),
            DefaultPollingIntervalSeconds: r.GetInt32(9),
            SyncBatchSize: r.GetInt32(10),
            HeartbeatIntervalSeconds: r.GetInt32(11),
            RetryBaseDelaySeconds: r.GetInt32(12),
            RetryMaxDelaySeconds: r.GetInt32(13),
            ActivatedAtUtc: ParseUtc(r.GetString(14)),
            LastConfigSyncAtUtc: r.IsDBNull(15) ? null : ParseUtc(r.GetString(15)));
    }

    public async Task SaveProfileAsync(ConnectorProfile p, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ConnectorConfiguration (
                Id, ConnectorId, AgentIdentifier, OrganisationId, OrganisationName, SiteName, ApiBaseUrl,
                CertificateThumbprint, CertificateNotAfterUtc, ConfigurationVersion,
                DefaultPollingIntervalSeconds, SyncBatchSize, HeartbeatIntervalSeconds,
                RetryBaseDelaySeconds, RetryMaxDelaySeconds, ActivatedAtUtc, LastConfigSyncAtUtc
            ) VALUES (
                1, $cid, $aid, $oid, $oname, $site, $url,
                $thumb, $exp, $cver, $poll, $batch, $hb, $rbase, $rmax, $act, $csync
            )
            ON CONFLICT(Id) DO UPDATE SET
                ConnectorId = excluded.ConnectorId,
                AgentIdentifier = excluded.AgentIdentifier,
                OrganisationId = excluded.OrganisationId,
                OrganisationName = excluded.OrganisationName,
                SiteName = excluded.SiteName,
                ApiBaseUrl = excluded.ApiBaseUrl,
                CertificateThumbprint = excluded.CertificateThumbprint,
                CertificateNotAfterUtc = excluded.CertificateNotAfterUtc,
                ConfigurationVersion = excluded.ConfigurationVersion,
                DefaultPollingIntervalSeconds = excluded.DefaultPollingIntervalSeconds,
                SyncBatchSize = excluded.SyncBatchSize,
                HeartbeatIntervalSeconds = excluded.HeartbeatIntervalSeconds,
                RetryBaseDelaySeconds = excluded.RetryBaseDelaySeconds,
                RetryMaxDelaySeconds = excluded.RetryMaxDelaySeconds,
                LastConfigSyncAtUtc = excluded.LastConfigSyncAtUtc;
            """;
        cmd.Parameters.AddWithValue("$cid", p.ConnectorId);
        cmd.Parameters.AddWithValue("$aid", p.AgentIdentifier);
        cmd.Parameters.AddWithValue("$oid", p.OrganisationId);
        cmd.Parameters.AddWithValue("$oname", p.OrganisationName);
        cmd.Parameters.AddWithValue("$site", (object?)p.SiteName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$url", p.ApiBaseUrl);
        cmd.Parameters.AddWithValue("$thumb", p.CertificateThumbprint);
        cmd.Parameters.AddWithValue("$exp", FormatUtc(p.CertificateNotAfterUtc));
        cmd.Parameters.AddWithValue("$cver", p.ConfigurationVersion);
        cmd.Parameters.AddWithValue("$poll", p.DefaultPollingIntervalSeconds);
        cmd.Parameters.AddWithValue("$batch", p.SyncBatchSize);
        cmd.Parameters.AddWithValue("$hb", p.HeartbeatIntervalSeconds);
        cmd.Parameters.AddWithValue("$rbase", p.RetryBaseDelaySeconds);
        cmd.Parameters.AddWithValue("$rmax", p.RetryMaxDelaySeconds);
        cmd.Parameters.AddWithValue("$act", FormatUtc(p.ActivatedAtUtc));
        cmd.Parameters.AddWithValue("$csync", p.LastConfigSyncAtUtc.HasValue ? FormatUtc(p.LastConfigSyncAtUtc.Value) : DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearProfileAsync(CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM ConnectorConfiguration; DELETE FROM RegisteredDevices;";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<DeviceConfiguration>> GetDevicesAsync(CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT DeviceId, Name, IpAddress, Port, ProtocolCode, PollingIntervalSeconds, Enabled,
                   LastConnectionStatus, LastSuccessfulPollUtc, LastEventTimestampUtc, LastError
            FROM RegisteredDevices ORDER BY Name, DeviceId;
            """;
        var list = new List<DeviceConfiguration>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new DeviceConfiguration(
                r.GetInt32(0),
                r.GetString(1),
                r.GetString(2),
                r.GetInt32(3),
                r.GetString(4),
                r.GetInt32(5),
                r.GetInt32(6) != 0,
                r.IsDBNull(7) ? null : r.GetString(7),
                r.IsDBNull(8) ? null : ParseUtc(r.GetString(8)),
                r.IsDBNull(9) ? null : ParseUtc(r.GetString(9)),
                r.IsDBNull(10) ? null : r.GetString(10)));
        }
        return list;
    }

    public async Task ReplaceDevicesAsync(IReadOnlyList<DeviceConfiguration> devices, int configVersion, string configHash, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);

        var existingHealth = new Dictionary<int, (string? Status, string? Poll, string? Evt, string? Err)>();
        await using (var sel = conn.CreateCommand())
        {
            sel.Transaction = tx;
            sel.CommandText = "SELECT DeviceId, LastConnectionStatus, LastSuccessfulPollUtc, LastEventTimestampUtc, LastError FROM RegisteredDevices;";
            await using var r = await sel.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                existingHealth[r.GetInt32(0)] = (
                    r.IsDBNull(1) ? null : r.GetString(1),
                    r.IsDBNull(2) ? null : r.GetString(2),
                    r.IsDBNull(3) ? null : r.GetString(3),
                    r.IsDBNull(4) ? null : r.GetString(4));
            }
        }

        await using (var del = conn.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM RegisteredDevices;";
            await del.ExecuteNonQueryAsync(ct);
        }

        foreach (var d in devices)
        {
            existingHealth.TryGetValue(d.DeviceId, out var prev);
            await using var ins = conn.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText = """
                INSERT INTO RegisteredDevices (
                    DeviceId, Name, IpAddress, Port, ProtocolCode, PollingIntervalSeconds, Enabled,
                    LastConnectionStatus, LastSuccessfulPollUtc, LastEventTimestampUtc, LastError
                ) VALUES ($id, $name, $ip, $port, $proto, $poll, $en, $st, $lp, $le, $err);
                """;
            ins.Parameters.AddWithValue("$id", d.DeviceId);
            ins.Parameters.AddWithValue("$name", d.Name);
            ins.Parameters.AddWithValue("$ip", d.IpAddress);
            ins.Parameters.AddWithValue("$port", d.Port);
            ins.Parameters.AddWithValue("$proto", d.ProtocolCode);
            ins.Parameters.AddWithValue("$poll", d.PollingIntervalSeconds);
            ins.Parameters.AddWithValue("$en", d.Enabled ? 1 : 0);
            ins.Parameters.AddWithValue("$st", (object?)prev.Status ?? DBNull.Value);
            ins.Parameters.AddWithValue("$lp", (object?)prev.Poll ?? DBNull.Value);
            ins.Parameters.AddWithValue("$le", (object?)prev.Evt ?? DBNull.Value);
            ins.Parameters.AddWithValue("$err", (object?)prev.Err ?? DBNull.Value);
            await ins.ExecuteNonQueryAsync(ct);
        }

        await using (var ver = conn.CreateCommand())
        {
            ver.Transaction = tx;
            ver.CommandText = """
                INSERT INTO ConfigurationVersions (VersionNumber, ConfigHash, AppliedAtUtc, DeviceCount)
                VALUES ($v, $h, $at, $cnt);
                """;
            ver.Parameters.AddWithValue("$v", configVersion);
            ver.Parameters.AddWithValue("$h", configHash);
            ver.Parameters.AddWithValue("$at", FormatUtc(DateTime.UtcNow));
            ver.Parameters.AddWithValue("$cnt", devices.Count);
            await ver.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task UpdateDeviceHealthAsync(int deviceId, string status, DateTime? lastSuccessfulPollUtc, DateTime? lastEventTimestampUtc, string? lastError, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE RegisteredDevices
            SET LastConnectionStatus = $st,
                LastSuccessfulPollUtc = COALESCE($lp, LastSuccessfulPollUtc),
                LastEventTimestampUtc = COALESCE($le, LastEventTimestampUtc),
                LastError = $err
            WHERE DeviceId = $id;
            """;
        cmd.Parameters.AddWithValue("$id", deviceId);
        cmd.Parameters.AddWithValue("$st", status);
        cmd.Parameters.AddWithValue("$lp", lastSuccessfulPollUtc.HasValue ? FormatUtc(lastSuccessfulPollUtc.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("$le", lastEventTimestampUtc.HasValue ? FormatUtc(lastEventTimestampUtc.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("$err", (object?)lastError ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<DeviceSyncCheckpoint?> GetCheckpointAsync(int deviceId, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT DeviceId, LastEventId, LastEventTimestampUtc, TotalEventsCollected, UpdatedAtUtc FROM DeviceSyncCheckpoints WHERE DeviceId = $id;";
        cmd.Parameters.AddWithValue("$id", deviceId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new DeviceSyncCheckpoint(
            r.GetInt32(0),
            r.IsDBNull(1) ? null : r.GetString(1),
            r.IsDBNull(2) ? null : ParseUtc(r.GetString(2)),
            r.GetInt64(3),
            ParseUtc(r.GetString(4)));
    }

    public async Task SaveCheckpointAsync(DeviceSyncCheckpoint cp, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO DeviceSyncCheckpoints (DeviceId, LastEventId, LastEventTimestampUtc, TotalEventsCollected, UpdatedAtUtc)
            VALUES ($id, $eid, $ts, $tot, $upd)
            ON CONFLICT(DeviceId) DO UPDATE SET
                LastEventId = excluded.LastEventId,
                LastEventTimestampUtc = excluded.LastEventTimestampUtc,
                TotalEventsCollected = excluded.TotalEventsCollected,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        cmd.Parameters.AddWithValue("$id", cp.DeviceId);
        cmd.Parameters.AddWithValue("$eid", (object?)cp.LastEventId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ts", cp.LastEventTimestampUtc.HasValue ? FormatUtc(cp.LastEventTimestampUtc.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("$tot", cp.TotalEventsCollected);
        cmd.Parameters.AddWithValue("$upd", FormatUtc(cp.UpdatedAtUtc));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> PersistCollectedEventsAsync(IReadOnlyList<RawAttendanceEvent> events, CancellationToken ct = default)
    {
        if (events.Count == 0) return 0;

        await using var conn = await database.OpenConnectionAsync(ct);
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
        var inserted = 0;

        foreach (var e in events)
        {
            await using var insRaw = conn.CreateCommand();
            insRaw.Transaction = tx;
            insRaw.CommandText = """
                INSERT OR IGNORE INTO RawAttendanceEvents (
                    ConnectorId, DeviceId, DeviceUserId, DeviceTimestampUtc, DeviceEventId,
                    PunchStatus, SourceProtocol, RawMetadata, IngestedAtUtc, DeduplicationKey, State
                ) VALUES (
                    $cid, $did, $uid, $ts, $eid, $ps, $proto, $meta, $ing, $dedup, 'PendingUpload'
                );
                """;
            insRaw.Parameters.AddWithValue("$cid", e.ConnectorId);
            insRaw.Parameters.AddWithValue("$did", e.DeviceId);
            insRaw.Parameters.AddWithValue("$uid", e.DeviceUserId);
            insRaw.Parameters.AddWithValue("$ts", FormatUtc(e.DeviceTimestampUtc));
            insRaw.Parameters.AddWithValue("$eid", e.DeviceEventId);
            insRaw.Parameters.AddWithValue("$ps", (object?)e.PunchStatus ?? DBNull.Value);
            insRaw.Parameters.AddWithValue("$proto", e.SourceProtocol);
            insRaw.Parameters.AddWithValue("$meta", (object?)e.RawMetadata ?? DBNull.Value);
            insRaw.Parameters.AddWithValue("$ing", FormatUtc(e.IngestedAtUtc));
            insRaw.Parameters.AddWithValue("$dedup", e.DeduplicationKey);

            var rows = await insRaw.ExecuteNonQueryAsync(ct);
            if (rows > 0)
            {
                await using var lastIdCmd = conn.CreateCommand();
                lastIdCmd.Transaction = tx;
                lastIdCmd.CommandText = "SELECT last_insert_rowid();";
                var localId = Convert.ToInt64(await lastIdCmd.ExecuteScalarAsync(ct));

                await using var insOutbox = conn.CreateCommand();
                insOutbox.Transaction = tx;
                insOutbox.CommandText = """
                    INSERT INTO OutboundSyncQueue (RawEventLocalId, State, RetryCount)
                    VALUES ($lid, 'PendingUpload', 0);
                    """;
                insOutbox.Parameters.AddWithValue("$lid", localId);
                await insOutbox.ExecuteNonQueryAsync(ct);
                inserted++;
            }
        }

        await tx.CommitAsync(ct);
        return inserted;
    }

    public async Task<IReadOnlyList<RawAttendanceEvent>> ClaimPendingBatchAsync(int maxBatchSize, string batchId, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
        var now = FormatUtc(DateTime.UtcNow);

        var items = new List<RawAttendanceEvent>();
        await using (var sel = conn.CreateCommand())
        {
            sel.Transaction = tx;
            sel.CommandText = """
                SELECT r.LocalId, r.ConnectorId, r.DeviceId, r.DeviceUserId, r.DeviceTimestampUtc,
                       r.DeviceEventId, r.PunchStatus, r.SourceProtocol, r.RawMetadata,
                       r.IngestedAtUtc, r.DeduplicationKey, q.RetryCount, q.NextRetryAtUtc, q.LastError
                FROM OutboundSyncQueue q
                INNER JOIN RawAttendanceEvents r ON r.LocalId = q.RawEventLocalId
                WHERE q.State = 'PendingUpload'
                  AND (q.NextRetryAtUtc IS NULL OR q.NextRetryAtUtc <= $now)
                ORDER BY r.DeviceTimestampUtc ASC, r.LocalId ASC
                LIMIT $lim;
                """;
            sel.Parameters.AddWithValue("$now", now);
            sel.Parameters.AddWithValue("$lim", maxBatchSize);

            await using var r = await sel.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                items.Add(new RawAttendanceEvent(
                    r.GetInt64(0),
                    r.GetString(1),
                    r.GetInt32(2),
                    r.GetString(3),
                    ParseUtc(r.GetString(4)),
                    r.GetString(5),
                    r.IsDBNull(6) ? null : r.GetString(6),
                    r.GetString(7),
                    r.IsDBNull(8) ? null : r.GetString(8),
                    ParseUtc(r.GetString(9)),
                    r.GetString(10),
                    OutboxEventState.Uploading,
                    r.GetInt32(11),
                    r.IsDBNull(12) ? null : ParseUtc(r.GetString(12)),
                    r.IsDBNull(13) ? null : r.GetString(13)));
            }
        }

        if (items.Count > 0)
        {
            foreach (var item in items)
            {
                await using var updQ = conn.CreateCommand();
                updQ.Transaction = tx;
                updQ.CommandText = "UPDATE OutboundSyncQueue SET State = 'Uploading', BatchId = $bid, ClaimedAtUtc = $now WHERE RawEventLocalId = $lid;";
                updQ.Parameters.AddWithValue("$bid", batchId);
                updQ.Parameters.AddWithValue("$now", now);
                updQ.Parameters.AddWithValue("$lid", item.LocalId);
                await updQ.ExecuteNonQueryAsync(ct);

                await using var updR = conn.CreateCommand();
                updR.Transaction = tx;
                updR.CommandText = "UPDATE RawAttendanceEvents SET State = 'Uploading' WHERE LocalId = $lid;";
                updR.Parameters.AddWithValue("$lid", item.LocalId);
                await updR.ExecuteNonQueryAsync(ct);
            }
        }

        await tx.CommitAsync(ct);
        return items;
    }

    public async Task RecoverStaleUploadingEventsAsync(TimeSpan staleThreshold, CancellationToken ct = default)
    {
        var cutoff = FormatUtc(DateTime.UtcNow - staleThreshold);
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                UPDATE RawAttendanceEvents
                SET State = 'PendingUpload'
                WHERE LocalId IN (
                    SELECT RawEventLocalId FROM OutboundSyncQueue
                    WHERE State = 'Uploading' AND (ClaimedAtUtc IS NULL OR ClaimedAtUtc <= $cutoff)
                );
                UPDATE OutboundSyncQueue
                SET State = 'PendingUpload', BatchId = NULL, ClaimedAtUtc = NULL
                WHERE State = 'Uploading' AND (ClaimedAtUtc IS NULL OR ClaimedAtUtc <= $cutoff);
                """;
            cmd.Parameters.AddWithValue("$cutoff", cutoff);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task ApplyBatchAcknowledgementAsync(
        string batchId,
        IReadOnlyList<long> localIds,
        int accepted,
        int duplicate,
        int rejected,
        string? serverMessage,
        CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
        var now = FormatUtc(DateTime.UtcNow);
        var finalState = (rejected > 0 && accepted == 0 && duplicate == 0) ? "Rejected" : "Acknowledged";

        foreach (var lid in localIds)
        {
            await using var updR = conn.CreateCommand();
            updR.Transaction = tx;
            updR.CommandText = "UPDATE RawAttendanceEvents SET State = $st, LastError = NULL WHERE LocalId = $lid;";
            updR.Parameters.AddWithValue("$st", finalState);
            updR.Parameters.AddWithValue("$lid", lid);
            await updR.ExecuteNonQueryAsync(ct);

            await using var delQ = conn.CreateCommand();
            delQ.Transaction = tx;
            delQ.CommandText = "DELETE FROM OutboundSyncQueue WHERE RawEventLocalId = $lid;";
            delQ.Parameters.AddWithValue("$lid", lid);
            await delQ.ExecuteNonQueryAsync(ct);
        }

        await using (var ack = conn.CreateCommand())
        {
            ack.Transaction = tx;
            ack.CommandText = """
                INSERT INTO SyncAcknowledgements (BatchId, AcceptedCount, DuplicateCount, RejectedCount, AcknowledgedAtUtc, ServerMessage)
                VALUES ($bid, $acc, $dup, $rej, $at, $msg);
                """;
            ack.Parameters.AddWithValue("$bid", batchId);
            ack.Parameters.AddWithValue("$acc", accepted);
            ack.Parameters.AddWithValue("$dup", duplicate);
            ack.Parameters.AddWithValue("$rej", rejected);
            ack.Parameters.AddWithValue("$at", now);
            ack.Parameters.AddWithValue("$msg", (object?)serverMessage ?? DBNull.Value);
            await ack.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task RecordBatchFailureAsync(
        string batchId,
        IReadOnlyList<long> localIds,
        string errorMessage,
        DateTime nextRetryAtUtc,
        int maxRetriesBeforeQuarantine = 20,
        CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);
        var nextStr = FormatUtc(nextRetryAtUtc);
        var trimmedErr = errorMessage.Length > 1000 ? errorMessage[..1000] : errorMessage;

        foreach (var lid in localIds)
        {
            await using var updQ = conn.CreateCommand();
            updQ.Transaction = tx;
            updQ.CommandText = """
                UPDATE OutboundSyncQueue
                SET RetryCount = RetryCount + 1,
                    State = CASE WHEN RetryCount + 1 >= $maxR THEN 'Quarantined' ELSE 'PendingUpload' END,
                    NextRetryAtUtc = $next,
                    LastError = $err,
                    BatchId = NULL,
                    ClaimedAtUtc = NULL
                WHERE RawEventLocalId = $lid;
                """;
            updQ.Parameters.AddWithValue("$maxR", maxRetriesBeforeQuarantine);
            updQ.Parameters.AddWithValue("$next", nextStr);
            updQ.Parameters.AddWithValue("$err", trimmedErr);
            updQ.Parameters.AddWithValue("$lid", lid);
            await updQ.ExecuteNonQueryAsync(ct);

            await using var updR = conn.CreateCommand();
            updR.Transaction = tx;
            updR.CommandText = """
                UPDATE RawAttendanceEvents
                SET RetryCount = RetryCount + 1,
                    State = CASE WHEN RetryCount + 1 >= $maxR THEN 'Quarantined' ELSE 'PendingUpload' END,
                    NextRetryAtUtc = $next,
                    LastError = $err
                WHERE LocalId = $lid;
                """;
            updR.Parameters.AddWithValue("$maxR", maxRetriesBeforeQuarantine);
            updR.Parameters.AddWithValue("$next", nextStr);
            updR.Parameters.AddWithValue("$err", trimmedErr);
            updR.Parameters.AddWithValue("$lid", lid);
            await updR.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<int> RetryPendingAndQuarantinedAsync(CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE OutboundSyncQueue
            SET State = 'PendingUpload', NextRetryAtUtc = NULL, RetryCount = 0, BatchId = NULL, ClaimedAtUtc = NULL
            WHERE State IN ('PendingUpload', 'Quarantined');
            UPDATE RawAttendanceEvents
            SET State = 'PendingUpload', NextRetryAtUtc = NULL, RetryCount = 0
            WHERE State IN ('PendingUpload', 'Quarantined');
            """;
        var count = await cmd.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return count;
    }

    public async Task<SyncMetricsSnapshot> GetSyncMetricsAsync(CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        int pending = 0, uploading = 0, acknowledged = 0, rejected = 0, quarantined = 0;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT State, COUNT(*) FROM RawAttendanceEvents GROUP BY State;";
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var st = r.GetString(0);
                var cnt = r.GetInt32(1);
                switch (st)
                {
                    case "Collected":
                    case "PendingUpload": pending += cnt; break;
                    case "Uploading": uploading += cnt; break;
                    case "Acknowledged": acknowledged += cnt; break;
                    case "Rejected": rejected += cnt; break;
                    case "Quarantined": quarantined += cnt; break;
                }
            }
        }

        DateTime? lastSync = null;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT AcknowledgedAtUtc FROM SyncAcknowledgements ORDER BY Id DESC LIMIT 1;";
            var scalar = await cmd.ExecuteScalarAsync(ct);
            if (scalar is string s && !string.IsNullOrWhiteSpace(s))
                lastSync = ParseUtc(s);
        }

        return new SyncMetricsSnapshot(pending, uploading, acknowledged, rejected, quarantined, lastSync);
    }

    public async Task<IReadOnlyList<RawAttendanceEvent>> GetRecentEventsAsync(int limit = 100, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT LocalId, ConnectorId, DeviceId, DeviceUserId, DeviceTimestampUtc, DeviceEventId,
                   PunchStatus, SourceProtocol, RawMetadata, IngestedAtUtc, DeduplicationKey, State,
                   RetryCount, NextRetryAtUtc, LastError
            FROM RawAttendanceEvents ORDER BY LocalId DESC LIMIT $lim;
            """;
        cmd.Parameters.AddWithValue("$lim", limit);
        var list = new List<RawAttendanceEvent>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            Enum.TryParse<OutboxEventState>(r.GetString(11), true, out var state);
            list.Add(new RawAttendanceEvent(
                r.GetInt64(0), r.GetString(1), r.GetInt32(2), r.GetString(3), ParseUtc(r.GetString(4)),
                r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6), r.GetString(7),
                r.IsDBNull(8) ? null : r.GetString(8), ParseUtc(r.GetString(9)), r.GetString(10),
                state, r.GetInt32(12), r.IsDBNull(13) ? null : ParseUtc(r.GetString(13)),
                r.IsDBNull(14) ? null : r.GetString(14)));
        }
        return list;
    }

    public async Task SaveReceivedCommandAsync(AgentCommandRecord c, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO AgentCommands (CommandId, CommandType, PayloadJson, Status, ReceivedAtUtc, ExecutedAtUtc, ResultJson, ErrorMessage)
            VALUES ($id, $type, $pay, $st, $rec, $exe, $res, $err);
            """;
        cmd.Parameters.AddWithValue("$id", c.CommandId);
        cmd.Parameters.AddWithValue("$type", c.CommandType);
        cmd.Parameters.AddWithValue("$pay", (object?)c.PayloadJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$st", c.Status);
        cmd.Parameters.AddWithValue("$rec", FormatUtc(c.ReceivedAtUtc));
        cmd.Parameters.AddWithValue("$exe", c.ExecutedAtUtc.HasValue ? FormatUtc(c.ExecutedAtUtc.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("$res", (object?)c.ResultJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$err", (object?)c.ErrorMessage ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateCommandResultAsync(long commandId, string status, string? resultJson, string? errorMessage, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE AgentCommands
            SET Status = $st, ExecutedAtUtc = $exe, ResultJson = $res, ErrorMessage = $err
            WHERE CommandId = $id;
            """;
        cmd.Parameters.AddWithValue("$id", commandId);
        cmd.Parameters.AddWithValue("$st", status);
        cmd.Parameters.AddWithValue("$exe", FormatUtc(DateTime.UtcNow));
        cmd.Parameters.AddWithValue("$res", (object?)resultJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$err", (object?)errorMessage ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AgentCommandRecord>> GetRecentCommandsAsync(int limit = 50, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CommandId, CommandType, PayloadJson, Status, ReceivedAtUtc, ExecutedAtUtc, ResultJson, ErrorMessage FROM AgentCommands ORDER BY CommandId DESC LIMIT $lim;";
        cmd.Parameters.AddWithValue("$lim", limit);
        var list = new List<AgentCommandRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new AgentCommandRecord(
                r.GetInt64(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetString(3),
                ParseUtc(r.GetString(4)), r.IsDBNull(5) ? null : ParseUtc(r.GetString(5)),
                r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? null : r.GetString(7)));
        }
        return list;
    }

    public async Task WriteHealthLogAsync(string level, string component, string message, string? detail = null, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO ConnectorHealthLogs (TimestampUtc, Level, Component, Message, Detail) VALUES ($ts, $lvl, $comp, $msg, $det);";
        cmd.Parameters.AddWithValue("$ts", FormatUtc(DateTime.UtcNow));
        cmd.Parameters.AddWithValue("$lvl", level);
        cmd.Parameters.AddWithValue("$comp", component);
        cmd.Parameters.AddWithValue("$msg", message);
        cmd.Parameters.AddWithValue("$det", (object?)detail ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ConnectorHealthLogEntry>> GetHealthLogsAsync(int limit = 200, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, TimestampUtc, Level, Component, Message, Detail FROM ConnectorHealthLogs ORDER BY Id DESC LIMIT $lim;";
        cmd.Parameters.AddWithValue("$lim", limit);
        var list = new List<ConnectorHealthLogEntry>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new ConnectorHealthLogEntry(
                r.GetInt64(0), ParseUtc(r.GetString(1)), r.GetString(2), r.GetString(3), r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5)));
        }
        return list;
    }

    public async Task<IReadOnlyList<ConfigurationVersionRecord>> GetConfigurationHistoryAsync(int limit = 20, CancellationToken ct = default)
    {
        await using var conn = await database.OpenConnectionAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT VersionNumber, ConfigHash, AppliedAtUtc, DeviceCount FROM ConfigurationVersions ORDER BY Id DESC LIMIT $lim;";
        cmd.Parameters.AddWithValue("$lim", limit);
        var list = new List<ConfigurationVersionRecord>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new ConfigurationVersionRecord(r.GetInt32(0), r.GetString(1), ParseUtc(r.GetString(2)), r.GetInt32(3)));
        }
        return list;
    }

    private static string FormatUtc(DateTime dt) => dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    private static DateTime ParseUtc(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
}
