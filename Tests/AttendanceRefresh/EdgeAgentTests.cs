using HRM.AttendanceEdgeAgent.Adapters;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Ingestion;
using HRM.AttendanceEdgeAgent.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public static class EdgeAgentTests
{
    public static async Task RunAsync()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "HRM_EdgeAgent_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var options = Options.Create(new AttendanceEdgeOptions
            {
                AgentId = "test-agent-01",
                OrganisationId = "10",
                DataDirectory = testDir,
                MaxBatchSize = 100
            });

            // 1. Test Device Cursor Store
            var cursorStore = new FileDeviceCursorStore(options, NullLogger<FileDeviceCursorStore>.Instance);
            var initial = await cursorStore.GetCursorAsync(101);
            if (initial != null) throw new Exception("Expected initial cursor to be null");

            var now = DateTime.UtcNow;
            await cursorStore.SaveCursorAsync(101, new DeviceCursor
            {
                DeviceId = 101,
                LastEventId = "evt-12345",
                LastEventTimestampUtc = now,
                TotalEventsIngested = 42
            });

            var loaded = await cursorStore.GetCursorAsync(101);
            if (loaded == null || loaded.LastEventId != "evt-12345" || loaded.TotalEventsIngested != 42)
                throw new Exception("Cursor failed to persist or reload correctly");

            // 2. Test Offline Outbox Event Buffer
            var buffer = new FileOfflineEventBuffer(options, NullLogger<FileOfflineEventBuffer>.Instance);
            var stats0 = await buffer.GetStatisticsAsync();
            if (stats0.PendingBatches != 0 || stats0.PendingEvents != 0)
                throw new Exception("Expected empty initial outbox buffer");

            // Enqueue 250 events (with MaxBatchSize = 100, should create 3 batches: 100, 100, 50)
            var events = Enumerable.Range(1, 250).Select(i => new AttendanceEnvelope(
                DeviceId: 101,
                LocalUserId: $"USR-{i:D4}",
                DeviceTimestamp: now.AddSeconds(i),
                DeviceEventId: $"EVT-{i}")).ToList();

            var enqueued = await buffer.EnqueueAsync(events);
            if (enqueued != 250) throw new Exception($"Expected 250 enqueued events, got {enqueued}");

            var stats1 = await buffer.GetStatisticsAsync();
            if (stats1.PendingBatches != 3 || stats1.PendingEvents != 250)
                throw new Exception($"Expected 3 pending batches and 250 pending events, got {stats1.PendingBatches} batches and {stats1.PendingEvents} events");

            var pending = await buffer.GetPendingBatchesAsync(maxBatches: 10);
            if (pending.Count != 3) throw new Exception($"Expected 3 pending batches, got {pending.Count}");

            // Record attempt on first batch
            await buffer.RecordAttemptAsync(pending[0].BatchId, "Simulated transient timeout");
            var reloaded = await buffer.GetPendingBatchesAsync(maxBatches: 1);
            if (reloaded[0].AttemptCount != 1 || reloaded[0].LastError != "Simulated transient timeout")
                throw new Exception("Failed to record batch transmission attempt");

            // Acknowledge first batch
            await buffer.AcknowledgeBatchAsync(pending[0].BatchId);
            var stats2 = await buffer.GetStatisticsAsync();
            if (stats2.PendingBatches != 2 || stats2.ArchivedBatches != 1)
                throw new Exception($"Expected 2 pending batches and 1 archived batch, got {stats2.PendingBatches} / {stats2.ArchivedBatches}");

            // 3. Test Generic File Drop Adapter
            var dropFolder = Path.Combine(testDir, "drop");
            Directory.CreateDirectory(dropFolder);

            var sampleCsv = Path.Combine(dropFolder, "terminal_export.csv");
            await File.WriteAllLinesAsync(sampleCsv, new[]
            {
                "EnrollmentNumber,Timestamp,EventId",
                "1001,2026-11-16 08:30:00,EVT-1001-A",
                "1002,2026-11-16 08:31:05,EVT-1002-B"
            });

            var adapter = new GenericFileDropAdapter(NullLogger<GenericFileDropAdapter>.Instance);
            var deviceOpt = new DeviceOptions
            {
                DeviceId = 50,
                Adapter = "generic-file",
                DropPath = dropFolder
            };

            var parsed = await adapter.ReadAsync(deviceOpt, null, CancellationToken.None);
            if (parsed.Count != 2) throw new Exception($"Expected 2 parsed drop events, got {parsed.Count}");
            if (parsed[0].LocalUserId != "1001" || parsed[1].LocalUserId != "1002")
                throw new Exception("Drop file user IDs did not match expected values");

            if (File.Exists(sampleCsv))
                throw new Exception("Processed drop file was not moved to .processed");

            var processedFiles = Directory.GetFiles(Path.Combine(dropFolder, ".processed"));
            if (processedFiles.Length != 1)
                throw new Exception("Processed file not found in .processed folder");

            Console.WriteLine("PASS Edge agent storage, offline store-and-forward outbox buffer, cursor tracking, and file-drop adapter");
        }
        finally
        {
            try { Directory.Delete(testDir, recursive: true); } catch { /* ignore temp cleanup */ }
        }
    }
}
