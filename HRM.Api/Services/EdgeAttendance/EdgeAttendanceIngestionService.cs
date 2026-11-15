using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HRM.Models;
using HRM.Models.EdgeAttendance;
using HRM.Services.Attendance.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.EdgeAttendance;

public sealed record EdgeAttendanceEventInput(int DeviceId, string LocalUserId, DateTime DeviceTimestamp, string DeviceEventId, string? RawMetadata);
public sealed record EdgeAttendanceBatchInput(string AgentId, IReadOnlyList<EdgeAttendanceEventInput> Events);
public sealed record EdgeAttendanceIngestionResult(int Accepted, int Duplicate, int Rejected);
public sealed record EdgeAttendanceHeartbeatInput(string AgentId, string HealthStatus);

public sealed class EdgeAttendanceIngestionService(IDbContextFactory<HrmTeContext> factory, IEdgeCertificateTrustValidator certificateTrust, IAttendanceLogProcessor attendanceLogProcessor)
{
    public async Task<EdgeAttendanceIngestionResult> AcceptAsync(X509Certificate2 certificate, EdgeAttendanceBatchInput batch, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(batch.AgentId) || batch.Events is null || batch.Events.Count is < 1 or > 500)
            throw new InvalidOperationException("A batch must contain between one and 500 events.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var agent = await AuthenticateAgentAsync(db, certificate, batch.AgentId, ct);

        var accepted = 0;
        var duplicate = 0;
        var rejected = 0;
        var logsToProcess = new List<(EdgeAttendanceEventReceipt Receipt, AttendanceLog Log)>();
        foreach (var item in batch.Events)
        {
            int? failureDeviceId = item.DeviceId > 0 ? item.DeviceId : null;
            if (!IsValid(item, out var timestamp))
            {
                AddFailure(db, agent.EdgeAttendanceAgentId, failureDeviceId, "InvalidEvent", "The event did not meet ingestion validation.");
                rejected++;
                continue;
            }

            var deviceIsBound = await db.EdgeAttendanceAgentDevices.AnyAsync(x => x.EdgeAttendanceAgentId == agent.EdgeAttendanceAgentId && x.AttendanceDeviceId == item.DeviceId && x.IsActive, ct);
            var deviceIsConfigured = await db.AttendanceDevices.AnyAsync(x => x.AttendanceDeviceId == item.DeviceId && x.OrganisationId == agent.OrganisationId && x.EdgeAttendanceAgentId == agent.EdgeAttendanceAgentId && x.IsEdgeEnabled, ct);
            if (!deviceIsBound || !deviceIsConfigured)
            {
                AddFailure(db, agent.EdgeAttendanceAgentId, item.DeviceId, "UnboundDevice", "The device is not actively bound to this agent.");
                rejected++;
                continue;
            }

            var hash = Hash(agent.EdgeAttendanceAgentId, item.DeviceId, item.LocalUserId, timestamp, item.DeviceEventId);
            var exists = await db.EdgeAttendanceEventReceipts.AnyAsync(x => x.EventHash == hash || (x.AttendanceDeviceId == item.DeviceId && x.DeviceEventId == item.DeviceEventId), ct);
            if (exists) { duplicate++; continue; }
            var receipt = new EdgeAttendanceEventReceipt { EdgeAttendanceAgentId = agent.EdgeAttendanceAgentId, AttendanceDeviceId = item.DeviceId, DeviceEventId = item.DeviceEventId.Trim(), EventHash = hash, LocalUserId = item.LocalUserId.Trim(), DeviceTimestampUtc = timestamp, ReceivedAtUtc = DateTime.UtcNow, ProcessingStatus = "PendingStaffMapping" };
            db.EdgeAttendanceEventReceipts.Add(receipt);
            var enrollments = await db.StaffEnrollmentNumbers.AsNoTracking().Where(x => x.OrganisationId == agent.OrganisationId && x.EnrollmentNumber == item.LocalUserId.Trim()).Select(x => x.IndividualId).Take(2).ToListAsync(ct);
            if (enrollments.Count != 1)
            {
                receipt.ProcessingStatus = "PendingStaffMapping";
                AddFailure(db, agent.EdgeAttendanceAgentId, item.DeviceId, "UnmatchedEnrollment", "No unique staff enrolment matched the device-local user ID.");
                accepted++;
                continue;
            }
            var job = await db.Jobs.AsNoTracking().Where(x => x.IndividualID == enrollments[0] && x.OrganisationID == agent.OrganisationId && x.JobStateId == 4 && x.JoinedDate <= timestamp.Date && (!x.TerminatedDate.HasValue || x.TerminatedDate.Value > timestamp.Date)).OrderByDescending(x => x.JobId).FirstOrDefaultAsync(ct);
            if (job is null)
            {
                receipt.ProcessingStatus = "PendingActiveJob";
                AddFailure(db, agent.EdgeAttendanceAgentId, item.DeviceId, "NoActiveJob", "The matched staff member has no active job in this organisation.");
                accepted++;
                continue;
            }
            var log = new AttendanceLog { AttendanceDeviceId = item.DeviceId, IndividualId = enrollments[0], OrganisationId = agent.OrganisationId, OrganisationStructureId = job.OrganisationStructureId, InOutModeId = 3, ActualInOutMode = 3, Date = timestamp, Year = timestamp.Year, Month = timestamp.Month, Day = timestamp.Day, Hour = timestamp.Hour, Minute = timestamp.Minute, Second = timestamp.Second, Uidstamp = hash, AttendanceLogStateId = 1, AttendanceLogModeId = 2 };
            db.AttendanceLogs.Add(log);
            receipt.ProcessingStatus = "Recorded";
            logsToProcess.Add((receipt, log));
            accepted++;
        }
        agent.LastSeenAtUtc = DateTime.UtcNow;
        agent.LastHealthStatus = rejected == 0 ? "Online" : "OnlineWithRejectedEvents";
        await db.SaveChangesAsync(ct);
        foreach (var (receipt, log) in logsToProcess) receipt.AttendanceLogId = log.AttendanceLogId;
        if (logsToProcess.Count > 0) await db.SaveChangesAsync(ct);
        foreach (var (_, log) in logsToProcess)
        {
            try { await attendanceLogProcessor.ProcessAsync(log.AttendanceLogId, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch
            {
                await using var failureDb = await factory.CreateDbContextAsync(ct);
                var receipt = await failureDb.EdgeAttendanceEventReceipts.SingleAsync(x => x.AttendanceLogId == log.AttendanceLogId, ct);
                receipt.ProcessingStatus = "RecordedPendingProcessing";
                AddFailure(failureDb, agent.EdgeAttendanceAgentId, receipt.AttendanceDeviceId, "ProcessingFailed", "The attendance record was saved but downstream processing must be retried.");
                await failureDb.SaveChangesAsync(ct);
            }
        }
        return new EdgeAttendanceIngestionResult(accepted, duplicate, rejected);
    }

    public async Task HeartbeatAsync(X509Certificate2 certificate, EdgeAttendanceHeartbeatInput heartbeat, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(heartbeat.HealthStatus) || (heartbeat.HealthStatus != "Online" && heartbeat.HealthStatus != "Degraded")) throw new InvalidOperationException("Unsupported health status.");
        await using var db = await factory.CreateDbContextAsync(ct);
        var agent = await AuthenticateAgentAsync(db, certificate, heartbeat.AgentId, ct);
        agent.LastSeenAtUtc = DateTime.UtcNow;
        agent.LastHealthStatus = heartbeat.HealthStatus;
        await db.SaveChangesAsync(ct);
    }

    private async Task<EdgeAttendanceAgent> AuthenticateAgentAsync(HrmTeContext db, X509Certificate2 certificate, string agentId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new UnauthorizedAccessException("Agent identity is required.");
        await certificateTrust.ValidateAsync(certificate, ct);
        var thumbprint = certificate.Thumbprint?.Trim();
        if (string.IsNullOrWhiteSpace(thumbprint)) throw new UnauthorizedAccessException("Client certificate has no thumbprint.");
        return await db.EdgeAttendanceAgents.SingleOrDefaultAsync(x => x.AgentIdentifier == agentId.Trim() && x.CertificateThumbprint == thumbprint && x.IsActive && x.RevokedAtUtc == null && x.CertificateNotAfterUtc > DateTime.UtcNow, ct) ?? throw new UnauthorizedAccessException("Edge agent is not enrolled or is revoked.");
    }

    private static bool IsValid(EdgeAttendanceEventInput item, out DateTime timestamp)
    {
        timestamp = default;
        if (item.DeviceId <= 0 || string.IsNullOrWhiteSpace(item.LocalUserId) || item.LocalUserId.Length > 100 || string.IsNullOrWhiteSpace(item.DeviceEventId) || item.DeviceEventId.Length > 200 || item.DeviceTimestamp.Kind == DateTimeKind.Unspecified) return false;
        timestamp = item.DeviceTimestamp.ToUniversalTime();
        return timestamp >= DateTime.UtcNow.AddDays(-90) && timestamp <= DateTime.UtcNow.AddMinutes(5);
    }
    private static string Hash(int agentId, int deviceId, string user, DateTime timestamp, string eventId) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{agentId}|{deviceId}|{user.Trim()}|{timestamp.Ticks}|{eventId.Trim()}")));
    private static void AddFailure(HrmTeContext db, int agentId, int? deviceId, string code, string detail) => db.EdgeAttendanceIngestionFailures.Add(new EdgeAttendanceIngestionFailure { EdgeAttendanceAgentId = agentId, AttendanceDeviceId = deviceId, FailureCode = code, Detail = detail, OccurredAtUtc = DateTime.UtcNow });
}
