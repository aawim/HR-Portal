using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HRM.DTOs;
using HRM.DTOs.EdgeAttendance;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.EdgeAttendance;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.EdgeAttendance;

public sealed class EdgeAgentManagementService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    EdgeAttendanceIngestionService ingestionService) : EdgeAgentManagementContracts.IOperations
{
    private async Task<int?> Guard()
    {
        var a = await access.RequireContextAsync();
        if (!a.IsSuperAdministrator && !await access.HasPermissionAsync("Edge.Agent.Manage") && !await access.HasUserGroupAsync("Developer"))
            throw new UnauthorizedAccessException("MDS Superadmin, Developer, or Edge.Agent.Manage is required.");
        return a.IndividualId;
    }

    public async Task<IReadOnlyList<HRM.DTOs.Organisation.OrganisationSettingsOption>> ListOrganisationsAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Organisations.AsNoTracking()
            .OrderBy(o => o.OrganisationName)
            .Select(o => new HRM.DTOs.Organisation.OrganisationSettingsOption(o.BusinessEntityID, o.OrganisationName))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EdgeAgentSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        var agents = await db.EdgeAttendanceAgents.AsNoTracking().OrderBy(x => x.AgentIdentifier).Select(x => new EdgeAgentSummaryDto(
            x.EdgeAttendanceAgentId,
            x.AgentIdentifier,
            x.OrganisationId,
            x.CertificateThumbprint,
            x.CertificateIssuer,
            x.CertificateNotAfterUtc,
            x.IsActive,
            x.RevokedAtUtc,
            x.LastSeenAtUtc,
            x.LastHealthStatus,
            db.EdgeAttendanceAgentDevices.Count(d => d.EdgeAttendanceAgentId == x.EdgeAttendanceAgentId && d.IsActive),
            db.EdgeAttendanceIngestionFailures.Count(f => f.EdgeAttendanceAgentId == x.EdgeAttendanceAgentId),
            x.SiteName,
            x.ConnectorVersion,
            x.OperatingSystem,
            x.ConfigurationVersion,
            x.LastSyncAtUtc,
            x.PendingEventCount)).ToListAsync(ct);

        var staleBefore = DateTime.UtcNow.AddMinutes(-10);
        for (var index = 0; index < agents.Count; index++)
        {
            if (agents[index].IsActive && (agents[index].LastSeenUtc is null || agents[index].LastSeenUtc < staleBefore))
            {
                agents[index] = agents[index] with { HealthStatus = "Offline" };
            }
        }
        return agents;
    }

    public async Task<IReadOnlyList<EdgeAttendanceDeviceDto>> ListDevicesAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.AttendanceDevices.AsNoTracking().OrderBy(x => x.OrganisationId).ThenBy(x => x.Location).Select(x => new EdgeAttendanceDeviceDto(
            x.AttendanceDeviceId,
            x.Ipaddress,
            x.Port,
            x.Location,
            x.OrganisationId,
            x.AdapterType,
            x.EdgeAttendanceAgentId,
            x.IsEdgeEnabled,
            30)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EdgeAttendanceIngestionFailureDto>> ListFailuresAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.EdgeAttendanceIngestionFailures.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(100).Select(x => new EdgeAttendanceIngestionFailureDto(
            x.EdgeAttendanceIngestionFailureId,
            x.EdgeAttendanceAgentId,
            x.AttendanceDeviceId,
            x.FailureCode,
            x.Detail,
            x.OccurredAtUtc)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EdgeTrustedIssuerDto>> ListTrustedIssuersAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.EdgeAttendanceTrustedIssuers.AsNoTracking().OrderBy(x => x.Subject).Select(x => new EdgeTrustedIssuerDto(
            x.EdgeAttendanceTrustedIssuerId,
            x.Subject,
            x.Thumbprint,
            x.IsActive,
            x.CreatedAtUtc)).ToListAsync(ct);
    }

    public async Task<ServiceResult> AddTrustedIssuerAsync(EdgeTrustedIssuerInput i, CancellationToken ct = default)
    {
        await Guard();
        try
        {
            var raw = Convert.FromBase64String(i.CertificateBase64.Trim());
            using var certificate = new X509Certificate2(raw);
            var basic = certificate.Extensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();
            if (basic?.CertificateAuthority != true || certificate.Subject != certificate.Issuer)
                return ServiceResult.Fail("Only a self-signed CA root certificate can be trusted.");

            await using var db = await factory.CreateDbContextAsync(ct);
            if (await db.EdgeAttendanceTrustedIssuers.AnyAsync(x => x.Thumbprint == certificate.Thumbprint, ct))
                return ServiceResult.Fail("This CA root is already registered.");

            db.EdgeAttendanceTrustedIssuers.Add(new EdgeAttendanceTrustedIssuer
            {
                Subject = certificate.Subject,
                Thumbprint = certificate.Thumbprint,
                CertificateRawData = raw,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            return ServiceResult.Ok("Trusted CA root added.");
        }
        catch
        {
            return ServiceResult.Fail("The supplied value is not a valid Base64-encoded CA certificate.");
        }
    }

    public async Task<ServiceResult> RevokeTrustedIssuerAsync(int id, CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        var issuer = await db.EdgeAttendanceTrustedIssuers.SingleOrDefaultAsync(x => x.EdgeAttendanceTrustedIssuerId == id, ct);
        if (issuer == null) return ServiceResult.Fail("Trusted issuer not found.");
        issuer.IsActive = false;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Trusted issuer revoked immediately.");
    }

    public async Task<ServiceResult> SaveDeviceConfigurationAsync(EdgeAttendanceDeviceConfigurationInput i, CancellationToken ct = default)
    {
        await Guard();
        if (i.OrganisationId <= 0)
            return ServiceResult.Fail("An organisation is required.");
        if (i.AttendanceDeviceId <= 0 && string.IsNullOrWhiteSpace(i.IpAddress))
            return ServiceResult.Fail("Enter an IP address / host for the new device.");
        if (i.AdapterType?.Length > 80)
            return ServiceResult.Fail("Adapter type cannot exceed 80 characters.");

        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Organisations.AnyAsync(x => x.BusinessEntityID == i.OrganisationId, ct))
            return ServiceResult.Fail("Organisation not found.");

        if (i.EdgeAttendanceAgentId.HasValue && !await db.EdgeAttendanceAgents.AnyAsync(x => x.EdgeAttendanceAgentId == i.EdgeAttendanceAgentId && x.OrganisationId == i.OrganisationId && x.IsActive && x.RevokedAtUtc == null, ct))
            return ServiceResult.Fail("The selected active agent does not belong to this organisation.");

        AttendanceDevice? device;
        if (i.AttendanceDeviceId == 0)
        {
            var sample = await db.AttendanceDevices.AsNoTracking()
                .Select(d => new { d.AttendanceClientId, d.AttendanceDeviceStateId, d.AttendanceDeviceInOutTypeId })
                .FirstOrDefaultAsync(ct);

            device = new AttendanceDevice
            {
                Ipaddress = i.IpAddress!.Trim(),
                Port = i.Port is > 0 ? i.Port.Value : 4370,
                Location = string.IsNullOrWhiteSpace(i.Location) ? "Biometric Device" : i.Location.Trim(),
                IsPrimary = false,
                AttendanceDeviceStateId = sample?.AttendanceDeviceStateId ?? 1,
                AttendanceClientId = sample?.AttendanceClientId ?? 1,
                AttendanceDeviceInOutTypeId = sample?.AttendanceDeviceInOutTypeId ?? 1,
                OrganisationId = i.OrganisationId,
                AdapterType = string.IsNullOrWhiteSpace(i.AdapterType) ? "zkteco" : i.AdapterType.Trim(),
                EdgeAttendanceAgentId = i.EdgeAttendanceAgentId,
                IsEdgeEnabled = i.IsEdgeEnabled
            };
            db.AttendanceDevices.Add(device);
            await db.SaveChangesAsync(ct);
            i.AttendanceDeviceId = device.AttendanceDeviceId;
        }
        else
        {
            device = await db.AttendanceDevices.SingleOrDefaultAsync(x => x.AttendanceDeviceId == i.AttendanceDeviceId, ct);
            if (device == null) return ServiceResult.Fail("Attendance device not found.");
        }

        var bindings = await db.EdgeAttendanceAgentDevices.Where(x => x.AttendanceDeviceId == i.AttendanceDeviceId).ToListAsync(ct);
        foreach (var binding in bindings) binding.IsActive = false;

        if (i.EdgeAttendanceAgentId.HasValue)
        {
            var pollSec = Math.Clamp(i.PollingIntervalSeconds, 5, 3600);
            var binding = bindings.SingleOrDefault(x => x.EdgeAttendanceAgentId == i.EdgeAttendanceAgentId.Value);
            if (binding is null)
                db.EdgeAttendanceAgentDevices.Add(new EdgeAttendanceAgentDevice { EdgeAttendanceAgentId = i.EdgeAttendanceAgentId.Value, AttendanceDeviceId = i.AttendanceDeviceId, IsActive = true, CreatedAtUtc = DateTime.UtcNow, PollingIntervalSeconds = pollSec });
            else
            {
                binding.IsActive = true;
                binding.PollingIntervalSeconds = pollSec;
            }

            var targetAgent = await db.EdgeAttendanceAgents.SingleOrDefaultAsync(a => a.EdgeAttendanceAgentId == i.EdgeAttendanceAgentId.Value, ct);
            if (targetAgent is not null)
                targetAgent.ConfigurationVersion++;
        }

        device.OrganisationId = i.OrganisationId;
        device.EdgeAttendanceAgentId = i.EdgeAttendanceAgentId;
        device.AdapterType = string.IsNullOrWhiteSpace(i.AdapterType) ? null : i.AdapterType.Trim();
        device.IsEdgeEnabled = i.IsEdgeEnabled;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Device configuration saved.");
    }

    public async Task<ServiceResult> RegisterAsync(EdgeAgentRegistrationInput i, CancellationToken ct = default)
    {
        await Guard();
        if (string.IsNullOrWhiteSpace(i.AgentIdentifier) || string.IsNullOrWhiteSpace(i.CertificateThumbprint) || i.OrganisationId <= 0 || i.CertificateNotAfterUtc <= DateTime.UtcNow || i.AttendanceDeviceIds.Count == 0)
            return ServiceResult.Fail("Agent, organisation, certificate and at least one device are required.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var ids = i.AttendanceDeviceIds.Distinct().ToArray();
        if (!await db.Organisations.AnyAsync(x => x.BusinessEntityID == i.OrganisationId, ct))
            return ServiceResult.Fail("Organisation not found.");

        if (await db.EdgeAttendanceAgents.AnyAsync(x => x.AgentIdentifier == i.AgentIdentifier.Trim() || x.CertificateThumbprint == i.CertificateThumbprint.Trim(), ct))
            return ServiceResult.Fail("Agent identifier or certificate is already enrolled.");

        var devices = await db.AttendanceDevices.Where(x => ids.Contains(x.AttendanceDeviceId)).ToListAsync(ct);
        if (devices.Count != ids.Length || devices.Any(x => x.OrganisationId != i.OrganisationId || x.EdgeAttendanceAgentId.HasValue))
            return ServiceResult.Fail("Every selected device must be configured for this organisation and not already bound to another agent.");

        var a = new EdgeAttendanceAgent
        {
            AgentIdentifier = i.AgentIdentifier.Trim(),
            OrganisationId = i.OrganisationId,
            CertificateThumbprint = i.CertificateThumbprint.Trim(),
            CertificateIssuer = i.CertificateIssuer.Trim(),
            CertificateNotAfterUtc = i.CertificateNotAfterUtc,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.EdgeAttendanceAgents.Add(a);
        await db.SaveChangesAsync(ct);

        foreach (var device in devices)
        {
            device.EdgeAttendanceAgentId = a.EdgeAttendanceAgentId;
            device.IsEdgeEnabled = true;
        }

        db.EdgeAttendanceAgentDevices.AddRange(ids.Select(id => new EdgeAttendanceAgentDevice
        {
            EdgeAttendanceAgentId = a.EdgeAttendanceAgentId,
            AttendanceDeviceId = id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        }));
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Agent enrolled.");
    }

    public async Task<ServiceResult> RevokeAsync(int id, CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        var a = await db.EdgeAttendanceAgents.SingleOrDefaultAsync(x => x.EdgeAttendanceAgentId == id, ct);
        if (a == null) return ServiceResult.Fail("Agent not found.");

        a.IsActive = false;
        a.RevokedAtUtc = DateTime.UtcNow;
        var bindings = await db.EdgeAttendanceAgentDevices.Where(x => x.EdgeAttendanceAgentId == id && x.IsActive).ToListAsync(ct);
        foreach (var binding in bindings) binding.IsActive = false;

        var devices = await db.AttendanceDevices.Where(x => x.EdgeAttendanceAgentId == id).ToListAsync(ct);
        foreach (var device in devices)
        {
            device.EdgeAttendanceAgentId = null;
            device.IsEdgeEnabled = false;
        }
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Agent and its device bindings were revoked immediately.");
    }

    public async Task<EdgeReconciliationResultDto> ReevaluatePendingReceiptsAsync(EdgeReconciliationFilterInput input, CancellationToken ct = default)
    {
        await Guard();
        return await ingestionService.ReevaluatePendingReceiptsAsync(input.OrganisationId, 500, ct);
    }

    public async Task<IReadOnlyList<EdgePendingReceiptDto>> ListPendingReceiptsAsync(EdgeReconciliationFilterInput input, CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        var query = db.EdgeAttendanceEventReceipts
            .AsNoTracking()
            .Where(x => (x.ProcessingStatus == "PendingStaffMapping" || x.ProcessingStatus == "PendingActiveJob") && x.AttendanceLogId == null);

        if (input.OrganisationId.HasValue)
        {
            var agentIds = await db.EdgeAttendanceAgents
                .Where(a => a.OrganisationId == input.OrganisationId.Value)
                .Select(a => a.EdgeAttendanceAgentId)
                .ToListAsync(ct);
            query = query.Where(x => agentIds.Contains(x.EdgeAttendanceAgentId));
        }

        var list = await (from r in query
                          join a in db.EdgeAttendanceAgents on r.EdgeAttendanceAgentId equals a.EdgeAttendanceAgentId
                          orderby r.ReceivedAtUtc descending
                          select new EdgePendingReceiptDto(
                              r.EdgeAttendanceEventReceiptId,
                              r.EdgeAttendanceAgentId,
                              a.AgentIdentifier,
                              r.AttendanceDeviceId,
                              r.DeviceEventId,
                              r.LocalUserId,
                              r.DeviceTimestampUtc,
                              r.ReceivedAtUtc,
                              r.ProcessingStatus,
                              a.OrganisationId
                          )).Take(100).ToListAsync(ct);
        return list;
    }

    public async Task<IReadOnlyList<EdgeStaffOptionDto>> SearchStaffAsync(EdgeStaffSearchInput input, CancellationToken ct = default)
    {
        await Guard();
        if (input.OrganisationId <= 0) return [];
        await using var db = await factory.CreateDbContextAsync(ct);
        var q = input.Query?.Trim() ?? "";

        var staffQuery = db.Staffs.AsNoTracking()
            .Where(s => db.Jobs.Any(j => j.IndividualID == s.IndividualId && j.OrganisationID == input.OrganisationId && j.JobStateId == 4));

        if (!string.IsNullOrWhiteSpace(q))
        {
            if (int.TryParse(q, out var staffId))
            {
                staffQuery = staffQuery.Where(s => s.IndividualId == staffId || s.Individual.FirstNameEnglish.Contains(q) || (s.Individual.LastNameEnglish != null && s.Individual.LastNameEnglish.Contains(q)));
            }
            else
            {
                staffQuery = staffQuery.Where(s => s.Individual.FirstNameEnglish.Contains(q) || (s.Individual.LastNameEnglish != null && s.Individual.LastNameEnglish.Contains(q)));
            }
        }

        var list = await (from s in staffQuery
                          let j = db.Jobs.Where(j => j.IndividualID == s.IndividualId && j.OrganisationID == input.OrganisationId && j.JobStateId == 4).OrderByDescending(j => j.JobId).FirstOrDefault()
                          let en = db.StaffEnrollmentNumbers.Where(e => e.OrganisationId == input.OrganisationId && e.IndividualId == s.IndividualId).Select(e => e.EnrollmentNumber).FirstOrDefault()
                          select new EdgeStaffOptionDto(
                              s.IndividualId,
                              (s.Individual.FirstNameEnglish + " " + (s.Individual.LastNameEnglish ?? "")).Trim(),
                              j != null ? j.Sapnumber : null,
                              en
                          )).Take(20).ToListAsync(ct);

        return list;
    }

    public async Task<ServiceResult> ResolvePendingReceiptAsync(ResolvePendingReceiptInput input, CancellationToken ct = default)
    {
        await Guard();
        if (input.ReceiptId <= 0 || input.IndividualId <= 0)
            return ServiceResult.Fail("Receipt ID and valid Staff member are required.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var receipt = await db.EdgeAttendanceEventReceipts.SingleOrDefaultAsync(x => x.EdgeAttendanceEventReceiptId == input.ReceiptId, ct);
        if (receipt is null) return ServiceResult.Fail("Pending receipt not found.");
        if (receipt.AttendanceLogId.HasValue || receipt.ProcessingStatus == "Recorded")
            return ServiceResult.Fail("This receipt has already been processed into an attendance log.");

        var agent = await db.EdgeAttendanceAgents.SingleOrDefaultAsync(a => a.EdgeAttendanceAgentId == receipt.EdgeAttendanceAgentId, ct);
        if (agent is null) return ServiceResult.Fail("Edge agent not found.");

        var timestamp = receipt.DeviceTimestampUtc;
        var job = await db.Jobs.AsNoTracking().Where(x => x.IndividualID == input.IndividualId && x.OrganisationID == agent.OrganisationId && x.JobStateId == 4 && x.JoinedDate <= timestamp.Date && (!x.TerminatedDate.HasValue || x.TerminatedDate.Value > timestamp.Date)).OrderByDescending(x => x.JobId).FirstOrDefaultAsync(ct);
        if (job is null)
            return ServiceResult.Fail("The selected staff member has no active job in this organisation covering the event timestamp.");

        if (input.AssignEnrollmentNumberToStaff)
        {
            var existing = await db.StaffEnrollmentNumbers.FirstOrDefaultAsync(x => x.OrganisationId == agent.OrganisationId && x.EnrollmentNumber == receipt.LocalUserId.Trim(), ct);
            if (existing is null)
            {
                db.StaffEnrollmentNumbers.Add(new StaffEnrollmentNumber
                {
                    OrganisationId = agent.OrganisationId,
                    IndividualId = input.IndividualId,
                    EnrollmentNumber = receipt.LocalUserId.Trim()
                });
                await db.SaveChangesAsync(ct);
            }
            else if (existing.IndividualId != input.IndividualId)
            {
                return ServiceResult.Fail($"Enrollment number '{receipt.LocalUserId}' is already assigned to another staff member (#{existing.IndividualId}).");
            }
        }

        var log = new AttendanceLog
        {
            AttendanceDeviceId = receipt.AttendanceDeviceId,
            IndividualId = input.IndividualId,
            OrganisationId = agent.OrganisationId,
            OrganisationStructureId = job.OrganisationStructureId,
            InOutModeId = 3,
            ActualInOutMode = 3,
            Date = timestamp,
            Year = timestamp.Year,
            Month = timestamp.Month,
            Day = timestamp.Day,
            Hour = timestamp.Hour,
            Minute = timestamp.Minute,
            Second = timestamp.Second,
            Uidstamp = receipt.EventHash,
            AttendanceLogStateId = 1,
            AttendanceLogModeId = 2
        };
        db.AttendanceLogs.Add(log);
        receipt.ProcessingStatus = "Recorded";
        await db.SaveChangesAsync(ct);
        receipt.AttendanceLogId = log.AttendanceLogId;
        await db.SaveChangesAsync(ct);

        var reevaluated = await ingestionService.ReevaluatePendingReceiptsAsync(agent.OrganisationId, 500, ct);

        var extraMsg = reevaluated.Resolved > 0 ? $" and resolved {reevaluated.Resolved} other pending event(s)" : "";
        return ServiceResult.Ok($"Successfully mapped badge {receipt.LocalUserId} to staff #{input.IndividualId}{extraMsg}.");
    }

    public async Task<CreatedActivationCodeDto> CreateActivationCodeAsync(CreateActivationCodeInput input, CancellationToken ct = default)
    {
        var createdByIndividualId = await Guard();
        if (input.OrganisationId <= 0)
            throw new InvalidOperationException("Organisation is required.");

        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Organisations.AnyAsync(o => o.BusinessEntityID == input.OrganisationId, ct))
            throw new InvalidOperationException("Organisation not found.");

        var expiresMinutes = Math.Clamp(input.ExpiresInMinutes, 5, 1440);
        var partA = RandomNumberGenerator.GetHexString(4, lowercase: false);
        var partB = RandomNumberGenerator.GetHexString(4, lowercase: false);
        var plaintext = $"DHR-ACT-{partA}-{partB}";
        var codeHash = AttendanceConnectorGatewayService.HashActivationCode(plaintext);
        var now = DateTime.UtcNow;

        var entity = new EdgeAttendanceActivationCode
        {
            CodeHash = codeHash,
            CodePrefix = $"DHR-ACT-{partA}-****",
            OrganisationId = input.OrganisationId,
            SiteName = string.IsNullOrWhiteSpace(input.SiteName) ? null : input.SiteName.Trim(),
            SuggestedConnectorName = string.IsNullOrWhiteSpace(input.SuggestedConnectorName) ? null : input.SuggestedConnectorName.Trim(),
            DeviceIdsJson = input.AttendanceDeviceIds.Count > 0 ? JsonSerializer.Serialize(input.AttendanceDeviceIds.Distinct().ToList()) : null,
            CreatedByIndividualId = createdByIndividualId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(expiresMinutes),
            IsRevoked = false
        };

        db.EdgeAttendanceActivationCodes.Add(entity);
        await db.SaveChangesAsync(ct);

        return new CreatedActivationCodeDto(
            entity.EdgeAttendanceActivationCodeId,
            plaintext,
            entity.CodePrefix,
            entity.OrganisationId,
            entity.SiteName,
            entity.SuggestedConnectorName,
            entity.ExpiresAtUtc);
    }

    public async Task<IReadOnlyList<EdgeActivationCodeDto>> ListActivationCodesAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.EdgeAttendanceActivationCodes.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => new EdgeActivationCodeDto(
                x.EdgeAttendanceActivationCodeId,
                x.CodePrefix,
                x.OrganisationId,
                x.SiteName,
                x.SuggestedConnectorName,
                x.CreatedAtUtc,
                x.ExpiresAtUtc,
                x.UsedAtUtc,
                x.UsedByEdgeAttendanceAgentId,
                x.IsRevoked))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult> RevokeActivationCodeAsync(int activationCodeId, CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        var code = await db.EdgeAttendanceActivationCodes.SingleOrDefaultAsync(x => x.EdgeAttendanceActivationCodeId == activationCodeId, ct);
        if (code is null) return ServiceResult.Fail("Activation code not found.");
        if (code.UsedAtUtc.HasValue) return ServiceResult.Fail("Activation code has already been redeemed.");
        code.IsRevoked = true;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Activation code revoked.");
    }

    public async Task<ServiceResult> QueueCommandAsync(QueueConnectorCommandInput input, CancellationToken ct = default)
    {
        var createdByIndividualId = await Guard();
        if (input.EdgeAttendanceAgentId <= 0)
            return ServiceResult.Fail("Connector ID is required.");
        if (!AttendanceConnectorGatewayService.AllowedCommandTypes.Contains(input.CommandType?.Trim() ?? ""))
            return ServiceResult.Fail("Unsupported command type. Allowed: SyncNow, TestDevice, RefreshConfig, ExportDiagnostics.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var agent = await db.EdgeAttendanceAgents.SingleOrDefaultAsync(a => a.EdgeAttendanceAgentId == input.EdgeAttendanceAgentId && a.IsActive && a.RevokedAtUtc == null, ct);
        if (agent is null)
            return ServiceResult.Fail("Active connector not found.");

        db.EdgeAttendanceConnectorCommands.Add(new EdgeAttendanceConnectorCommand
        {
            EdgeAttendanceAgentId = agent.EdgeAttendanceAgentId,
            CommandType = input.CommandType!.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(input.PayloadJson) ? null : input.PayloadJson.Trim(),
            Status = "Pending",
            CreatedByIndividualId = createdByIndividualId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok($"Command '{input.CommandType}' queued for {agent.AgentIdentifier}.");
    }

    public async Task<IReadOnlyList<EdgeConnectorCommandDto>> ListCommandsAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        return await (from c in db.EdgeAttendanceConnectorCommands.AsNoTracking()
                      join a in db.EdgeAttendanceAgents.AsNoTracking() on c.EdgeAttendanceAgentId equals a.EdgeAttendanceAgentId
                      orderby c.CreatedAtUtc descending
                      select new EdgeConnectorCommandDto(
                          c.EdgeAttendanceConnectorCommandId,
                          c.EdgeAttendanceAgentId,
                          a.AgentIdentifier,
                          c.CommandType,
                          c.Status,
                          c.CreatedAtUtc,
                          c.DispatchedAtUtc,
                          c.CompletedAtUtc,
                          c.ResultJson,
                          c.ErrorMessage))
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EdgeConnectorAlertDto>> ListAlertsAsync(CancellationToken ct = default)
    {
        await Guard();
        await using var db = await factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var alerts = new List<EdgeConnectorAlertDto>();

        var activeAgents = await db.EdgeAttendanceAgents.AsNoTracking()
            .Where(a => a.IsActive && a.RevokedAtUtc == null)
            .ToListAsync(ct);

        var offlineThreshold = now.AddMinutes(-10);
        var certExpiryWarning = now.AddDays(30);

        foreach (var a in activeAgents)
        {
            if (a.LastSeenAtUtc is null || a.LastSeenAtUtc < offlineThreshold)
            {
                alerts.Add(new EdgeConnectorAlertDto(
                    "Critical",
                    "ConnectorOffline",
                    a.EdgeAttendanceAgentId,
                    a.AgentIdentifier,
                    a.OrganisationId,
                    $"Connector '{a.AgentIdentifier}' is offline",
                    a.LastSeenAtUtc.HasValue
                        ? $"Last heartbeat received at {a.LastSeenAtUtc:yyyy-MM-dd HH:mm} UTC."
                        : "Connector has never reported a heartbeat since registration.",
                    now));
            }

            if (a.CertificateNotAfterUtc <= certExpiryWarning)
            {
                var daysLeft = Math.Max(0, (int)(a.CertificateNotAfterUtc - now).TotalDays);
                alerts.Add(new EdgeConnectorAlertDto(
                    daysLeft <= 7 ? "Critical" : "Warning",
                    "CertificateExpiring",
                    a.EdgeAttendanceAgentId,
                    a.AgentIdentifier,
                    a.OrganisationId,
                    $"Certificate expiring in {daysLeft} day(s)",
                    $"Connector '{a.AgentIdentifier}' client certificate expires on {a.CertificateNotAfterUtc:yyyy-MM-dd}.",
                    now));
            }

            if (a.PendingEventCount >= 500)
            {
                alerts.Add(new EdgeConnectorAlertDto(
                    "Warning",
                    "SyncBacklogHigh",
                    a.EdgeAttendanceAgentId,
                    a.AgentIdentifier,
                    a.OrganisationId,
                    $"High local sync backlog ({a.PendingEventCount} events)",
                    $"Connector '{a.AgentIdentifier}' has {a.PendingEventCount} unsynchronised attendance events queued locally.",
                    now));
            }
        }

        var recentFailuresCutoff = now.AddHours(-24);
        var unauthorizedSubmissions = await db.EdgeAttendanceIngestionFailures.AsNoTracking()
            .Where(f => f.OccurredAtUtc >= recentFailuresCutoff && f.FailureCode == "UnboundDevice")
            .OrderByDescending(f => f.OccurredAtUtc)
            .Take(20)
            .ToListAsync(ct);

        foreach (var f in unauthorizedSubmissions)
        {
            var agent = activeAgents.FirstOrDefault(a => a.EdgeAttendanceAgentId == f.EdgeAttendanceAgentId);
            alerts.Add(new EdgeConnectorAlertDto(
                "Critical",
                "UnauthorisedDeviceSubmission",
                f.EdgeAttendanceAgentId,
                agent?.AgentIdentifier,
                agent?.OrganisationId,
                $"Unauthorised device submission (Device #{f.AttendanceDeviceId})",
                f.Detail,
                f.OccurredAtUtc));
        }

        return alerts.OrderByDescending(a => a.Severity == "Critical").ThenByDescending(a => a.DetectedAtUtc).ToList();
    }
}
