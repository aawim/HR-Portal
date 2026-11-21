using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using X509CertificateRequest = System.Security.Cryptography.X509Certificates.CertificateRequest;
using HRM.AttendanceEdgeAgent.Adapters;
using HRM.AttendanceEdgeAgent.Configuration;
using HRM.AttendanceEdgeAgent.Ingestion;
using HRM.AttendanceEdgeAgent.Storage;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.EdgeAttendance;
using HRM.DTOs.EdgeAttendance;
using HRM.Services.Attendance.Abstraction;
using HRM.Services.Attendance.Processing;
using HRM.Services.EdgeAttendance;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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

            // 4. Test Server-side mTLS Ingestion & Trust Pipeline (End-to-End)
            var dbOptions = new DbContextOptionsBuilder<HrmTeContext>()
                .UseInMemoryDatabase("HRM_EdgeIngestion_Test_" + Guid.NewGuid().ToString("N"), x => x.EnableNullChecks(false))
                .Options;
            var dbFactory = new EdgeAgentTestDbContextFactory(dbOptions);

            // Generate Root CA
            using var caRsa = RSA.Create(2048);
            var caReq = new X509CertificateRequest(
                "CN=DHR Edge Authority Root CA, O=HRM Attendance Edge",
                caRsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            caReq.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            caReq.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));
            using var caCert = caReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-10), DateTimeOffset.UtcNow.AddYears(10));

            // Generate Agent Client Certificate signed by Root CA
            using var clientRsa = RSA.Create(2048);
            var clientReq = new X509CertificateRequest(
                "CN=test-agent-01, O=HRM Attendance Edge",
                clientRsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            clientReq.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: false));
            clientReq.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
            clientReq.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.2") }, critical: false));

            var serial = new byte[8];
            RandomNumberGenerator.Fill(serial);
            using var clientCert = clientReq.Create(caCert, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(2), serial);

            // Seed DB with Trusted CA, Agent, Device, Staff Enrollment, and Active Job
            await using (var db = dbFactory.CreateDbContext())
            {
                db.EdgeAttendanceTrustedIssuers.Add(new EdgeAttendanceTrustedIssuer
                {
                    EdgeAttendanceTrustedIssuerId = 1,
                    Subject = caCert.Subject,
                    Thumbprint = caCert.Thumbprint,
                    CertificateRawData = caCert.RawData,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });

                db.EdgeAttendanceAgents.Add(new EdgeAttendanceAgent
                {
                    EdgeAttendanceAgentId = 1,
                    AgentIdentifier = "test-agent-01",
                    CertificateThumbprint = clientCert.Thumbprint,
                    CertificateIssuer = caCert.Subject,
                    CertificateNotAfterUtc = clientCert.NotAfter.ToUniversalTime(),
                    IsActive = true,
                    OrganisationId = 10,
                    CreatedAtUtc = DateTime.UtcNow
                });

                db.AttendanceDevices.Add(new AttendanceDevice
                {
                    AttendanceDeviceId = 101,
                    OrganisationId = 10,
                    EdgeAttendanceAgentId = 1,
                    IsEdgeEnabled = true,
                    Ipaddress = "192.168.1.50",
                    Port = 4370
                });

                db.EdgeAttendanceAgentDevices.Add(new EdgeAttendanceAgentDevice
                {
                    EdgeAttendanceAgentDeviceId = 1,
                    EdgeAttendanceAgentId = 1,
                    AttendanceDeviceId = 101,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });

                db.StaffEnrollmentNumbers.Add(new StaffEnrollmentNumber
                {
                    StaffEnrollmentNumberId = 1,
                    OrganisationId = 10,
                    IndividualId = 999,
                    EnrollmentNumber = "USR-0042"
                });

                db.Jobs.Add(new Job
                {
                    JobId = 500,
                    IndividualID = 999,
                    OrganisationID = 10,
                    JobStateId = 4,
                    JoinedDate = DateTime.UtcNow.AddYears(-1).Date,
                    OrganisationStructureId = 12
                });

                await db.SaveChangesAsync();
            }

            // Test Certificate Trust Validator
            var trustValidator = new DatabaseCertificateTrustValidator(dbFactory);
            await trustValidator.ValidateAsync(clientCert);

            // Test Untrusted Certificate Rejection
            using var rogueRsa = RSA.Create(2048);
            var rogueReq = new X509CertificateRequest("CN=rogue-agent", rogueRsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var rogueCert = rogueReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(1));
            bool rogueRejected = false;
            try
            {
                await trustValidator.ValidateAsync(rogueCert);
            }
            catch (UnauthorizedAccessException)
            {
                rogueRejected = true;
            }
            if (!rogueRejected) throw new Exception("Expected rogue certificate to be rejected by trust validator");

            // Test Ingestion Service with valid batch
            var testLogProcessor = new TestAttendanceLogProcessor();
            var ingestionService = new EdgeAttendanceIngestionService(dbFactory, trustValidator, testLogProcessor);

            var eventTimestamp = DateTime.UtcNow.AddMinutes(-5);
            var batchInput = new EdgeAttendanceBatchInput(
                AgentId: "test-agent-01",
                Events: new[]
                {
                    new EdgeAttendanceEventInput(
                        DeviceId: 101,
                        LocalUserId: "USR-0042",
                        DeviceTimestamp: eventTimestamp,
                        DeviceEventId: "EVT-INGEST-001",
                        RawMetadata: null)
                });

            var result = await ingestionService.AcceptAsync(clientCert, batchInput);
            if (result.Accepted != 1 || result.Duplicate != 0 || result.Rejected != 0)
                throw new Exception($"Expected 1 accepted event, got {result.Accepted} accepted, {result.Duplicate} dup, {result.Rejected} rej");

            // Verify Database Receipt and AttendanceLog
            await using (var db = dbFactory.CreateDbContext())
            {
                var receipt = await db.EdgeAttendanceEventReceipts.SingleOrDefaultAsync(x => x.DeviceEventId == "EVT-INGEST-001");
                if (receipt == null || receipt.ProcessingStatus != "Recorded" || !receipt.AttendanceLogId.HasValue)
                    throw new Exception("EdgeAttendanceEventReceipt was not properly recorded");

                var log = await db.AttendanceLogs.SingleOrDefaultAsync(x => x.AttendanceLogId == receipt.AttendanceLogId.Value);
                if (log == null || log.IndividualId != 999 || log.AttendanceDeviceId != 101 || log.OrganisationId != 10 || log.OrganisationStructureId != 12)
                    throw new Exception("AttendanceLog was not correctly mapped and created");

                if (!testLogProcessor.ProcessedIds.Contains(log.AttendanceLogId))
                    throw new Exception("IAttendanceLogProcessor was not invoked for the ingested log");
            }

            // Test Deduplication (same event sent again)
            var duplicateResult = await ingestionService.AcceptAsync(clientCert, batchInput);
            if (duplicateResult.Accepted != 0 || duplicateResult.Duplicate != 1 || duplicateResult.Rejected != 0)
                throw new Exception($"Expected duplicate event detection, got {duplicateResult.Accepted} accepted, {duplicateResult.Duplicate} dup, {duplicateResult.Rejected} rej");

            // Test Unbound Device Rejection
            var unboundBatch = new EdgeAttendanceBatchInput(
                AgentId: "test-agent-01",
                Events: new[]
                {
                    new EdgeAttendanceEventInput(
                        DeviceId: 999,
                        LocalUserId: "USR-0042",
                        DeviceTimestamp: eventTimestamp,
                        DeviceEventId: "EVT-UNBOUND-001",
                        RawMetadata: null)
                });

            var unboundResult = await ingestionService.AcceptAsync(clientCert, unboundBatch);
            if (unboundResult.Rejected != 1)
                throw new Exception($"Expected unbound device rejection, got {unboundResult.Rejected} rejected");

            // Test Unmatched Enrollment (local user with no staff record)
            var unmatchedBatch = new EdgeAttendanceBatchInput(
                AgentId: "test-agent-01",
                Events: new[]
                {
                    new EdgeAttendanceEventInput(
                        DeviceId: 101,
                        LocalUserId: "USR-UNKNOWN-99",
                        DeviceTimestamp: eventTimestamp,
                        DeviceEventId: "EVT-UNMATCHED-001",
                        RawMetadata: null)
                });

            var unmatchedResult = await ingestionService.AcceptAsync(clientCert, unmatchedBatch);
            if (unmatchedResult.Accepted != 1)
                throw new Exception("Expected unmatched event to be accepted with PendingStaffMapping status");

            await using (var db = dbFactory.CreateDbContext())
            {
                var unmatchedReceipt = await db.EdgeAttendanceEventReceipts.SingleOrDefaultAsync(x => x.DeviceEventId == "EVT-UNMATCHED-001");
                if (unmatchedReceipt == null || unmatchedReceipt.ProcessingStatus != "PendingStaffMapping" || unmatchedReceipt.AttendanceLogId.HasValue)
                    throw new Exception("Unmatched event did not record PendingStaffMapping status");
            }

            // 5. Test Re-evaluate Pending Receipts (Option 1)
            // Seed staff #888 with an active job
            await using (var db = dbFactory.CreateDbContext())
            {
                db.Jobs.Add(new Job
                {
                    JobId = 501,
                    IndividualID = 888,
                    OrganisationID = 10,
                    JobStateId = 4,
                    JoinedDate = DateTime.UtcNow.AddYears(-1).Date,
                    OrganisationStructureId = 12
                });
                // Assign enrollment number USR-UNKNOWN-99 to staff #888
                db.StaffEnrollmentNumbers.Add(new StaffEnrollmentNumber
                {
                    StaffEnrollmentNumberId = 2,
                    OrganisationId = 10,
                    IndividualId = 888,
                    EnrollmentNumber = "USR-UNKNOWN-99"
                });
                await db.SaveChangesAsync();
            }

            // Run re-evaluation
            var reevalResult = await ingestionService.ReevaluatePendingReceiptsAsync(organisationId: 10);
            if (reevalResult.Evaluated < 1 || reevalResult.Resolved != 1 || reevalResult.RemainingPending != 0)
                throw new Exception($"Expected 1 resolved receipt from re-evaluation, got {reevalResult.Resolved} resolved, {reevalResult.RemainingPending} remaining");

            await using (var db = dbFactory.CreateDbContext())
            {
                var receipt = await db.EdgeAttendanceEventReceipts.SingleAsync(x => x.DeviceEventId == "EVT-UNMATCHED-001");
                if (receipt.ProcessingStatus != "Recorded" || !receipt.AttendanceLogId.HasValue)
                    throw new Exception("Re-evaluated receipt status was not updated to Recorded");

                var log = await db.AttendanceLogs.SingleAsync(x => x.AttendanceLogId == receipt.AttendanceLogId.Value);
                if (log.IndividualId != 888 || log.AttendanceDeviceId != 101)
                    throw new Exception("Re-evaluated attendance log not mapped to expected employee");

                if (!testLogProcessor.ProcessedIds.Contains(log.AttendanceLogId))
                    throw new Exception("AttendanceLogProcessor was not triggered for re-evaluated log");
            }

            // 6. Test EdgeAgentManagementService: ListPendingReceiptsAsync, SearchStaffAsync, ResolvePendingReceiptAsync
            var adminAccess = Proxy.For<IUserAccessService>((m, a) => m switch
            {
                "RequireContextAsync" => Task.FromResult(new HRM.DTOs.UserContext.UserContextDto { UserId = 1, IndividualId = 100, IsSuperAdministrator = true }),
                _ => throw new Exception(m)
            });

            var edgeMgmtService = new EdgeAgentManagementService(dbFactory, adminAccess, ingestionService);

            // Ingest a new pending event for manual resolution
            var manualBatch = new EdgeAttendanceBatchInput(
                AgentId: "test-agent-01",
                Events: new[]
                {
                    new EdgeAttendanceEventInput(
                        DeviceId: 101,
                        LocalUserId: "USR-NEW-BADGE-77",
                        DeviceTimestamp: eventTimestamp,
                        DeviceEventId: "EVT-MANUAL-001",
                        RawMetadata: null)
                });

            var manualIngestResult = await ingestionService.AcceptAsync(clientCert, manualBatch);
            if (manualIngestResult.Accepted != 1)
                throw new Exception("Expected manual batch event to be accepted");

            // Verify ListPendingReceiptsAsync returns the pending receipt
            var pendingList = await edgeMgmtService.ListPendingReceiptsAsync(new EdgeReconciliationFilterInput { OrganisationId = 10 });
            var pendingItem = pendingList.FirstOrDefault(p => p.DeviceEventId == "EVT-MANUAL-001");
            if (pendingItem is null || pendingItem.ProcessingStatus != "PendingStaffMapping")
                throw new Exception("ListPendingReceiptsAsync did not return the pending manual receipt");

            // Test SearchStaffAsync
            // Seed Staff record for Individual #888
            await using (var db = dbFactory.CreateDbContext())
            {
                db.Staffs.Add(new Staff
                {
                    IndividualId = 888,
                    Individual = new Individual { BusinessEntityId = 888, FirstNameEnglish = "Ahmed", LastNameEnglish = "Ali" }
                });
                await db.SaveChangesAsync();
            }

            var staffResults = await edgeMgmtService.SearchStaffAsync(new EdgeStaffSearchInput { OrganisationId = 10, Query = "Ahmed" });
            if (staffResults.Count == 0 || staffResults[0].IndividualId != 888)
                throw new Exception("SearchStaffAsync failed to find matching staff member");

            // Test ResolvePendingReceiptAsync with permanent badge assignment
            var resolveResult = await edgeMgmtService.ResolvePendingReceiptAsync(new ResolvePendingReceiptInput
            {
                ReceiptId = pendingItem.ReceiptId,
                IndividualId = 888,
                AssignEnrollmentNumberToStaff = true
            });

            if (!resolveResult.Success)
                throw new Exception($"ResolvePendingReceiptAsync failed: {resolveResult.Message}");

            // Verify receipt and enrollment number
            await using (var db = dbFactory.CreateDbContext())
            {
                var resolvedReceipt = await db.EdgeAttendanceEventReceipts.SingleAsync(x => x.EdgeAttendanceEventReceiptId == pendingItem.ReceiptId);
                if (resolvedReceipt.ProcessingStatus != "Recorded" || !resolvedReceipt.AttendanceLogId.HasValue)
                    throw new Exception("Manually resolved receipt was not marked Recorded with AttendanceLogId");

                var newEnrollment = await db.StaffEnrollmentNumbers.FirstOrDefaultAsync(x => x.OrganisationId == 10 && x.EnrollmentNumber == "USR-NEW-BADGE-77");
                if (newEnrollment is null || newEnrollment.IndividualId != 888)
                    throw new Exception("ResolvePendingReceiptAsync did not persist permanent StaffEnrollmentNumber");
            }

            // Test Agent Heartbeat
            await ingestionService.HeartbeatAsync(clientCert, new EdgeAttendanceHeartbeatInput("test-agent-01", "Online"));
            await using (var db = dbFactory.CreateDbContext())
            {
                var agent = await db.EdgeAttendanceAgents.SingleAsync(x => x.AgentIdentifier == "test-agent-01");
                if (agent.LastHealthStatus != "Online" || !agent.LastSeenAtUtc.HasValue)
                    throw new Exception("Heartbeat failed to update agent health status and last seen timestamp");
            }

            Console.WriteLine("PASS Edge agent storage, offline store-and-forward outbox buffer, cursor tracking, file-drop adapter, end-to-end mTLS ingestion pipeline, and staff reconciliation");
        }
        finally
        {
            try { Directory.Delete(testDir, recursive: true); } catch { /* ignore temp cleanup */ }
        }
    }
}

sealed class EdgeAgentTestDbContext(DbContextOptions<HrmTeContext> options) : HrmTeContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
}

sealed class EdgeAgentTestDbContextFactory(DbContextOptions<HrmTeContext> options) : IDbContextFactory<HrmTeContext>
{
    public HrmTeContext CreateDbContext() => new EdgeAgentTestDbContext(options);
    public Task<HrmTeContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult<HrmTeContext>(new EdgeAgentTestDbContext(options));
}

sealed class TestAttendanceLogProcessor : IAttendanceLogProcessor
{
    public List<int> ProcessedIds { get; } = new();

    public Task<AttendanceProcessingResult> ProcessAsync(int attendanceLogId, CancellationToken cancellationToken = default)
    {
        ProcessedIds.Add(attendanceLogId);
        return Task.FromResult(new AttendanceProcessingResult { Success = true, IsSuccess = true, Message = "Processed" });
    }
}
