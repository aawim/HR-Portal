using System.Text.Json;
using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using HRM.ActivityLogs.Persistence;
using HRM.ActivityLogs.Processing;
using HRM.ActivityLogs.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.ActivityLogs.Services;

public sealed class ActivityLogService(
    IDbContextFactory<ActivityDbContext> dbFactory,
    IActivityContextAccessor contextAccessor,
    IActivityClock clock,
    ActivityQueueChannel queue,
    IOptions<ActivityLogOptions> optionsAccessor,
    ILogger<ActivityLogService> logger) : IActivityLogService
{
    private readonly ActivityLogOptions _options = optionsAccessor.Value;

    public async Task RecordAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_options.DisabledCategories.Contains(request.Category.ToString()))
            return;

        var actor = await contextAccessor.GetCurrentContextAsync(cancellationToken);
        var entry = BuildEntity(request, actor);

        if (request.RequireGuaranteedDelivery)
        {
            await PersistDirectlyWithIdempotencyAsync(entry, cancellationToken);
            return;
        }

        if (_options.EnableAsyncQueue && queue.TryEnqueue(entry))
            return;

        await PersistDirectlyWithIdempotencyAsync(entry, cancellationToken);
    }

    public async Task EnqueueAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_options.DisabledCategories.Contains(request.Category.ToString()))
            return;

        var actor = await contextAccessor.GetCurrentContextAsync(cancellationToken);
        var entry = BuildEntity(request, actor);
        await queue.EnqueueAsync(entry, cancellationToken);
    }

    public async Task RecordInOutboxAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_options.DisabledCategories.Contains(request.Category.ToString()))
            return;

        var actor = await contextAccessor.GetCurrentContextAsync(cancellationToken);
        var entry = BuildEntity(request, actor);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.ActivityOutbox.AsNoTracking().AnyAsync(x => x.EventId == entry.EventId, cancellationToken))
            return;

        db.ActivityOutbox.Add(new ActivityOutboxMessage
        {
            EventId = entry.EventId,
            PayloadJson = JsonSerializer.Serialize(entry),
            AttemptCount = 0,
            NextAttemptAtUtc = clock.UtcNow,
            CreatedAtUtc = clock.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task RecordAuthenticationAsync(
        string activityType, string description, int? userId = null, int? organisationId = null,
        CancellationToken cancellationToken = default)
        => RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Authentication,
            ActivityType = activityType,
            Module = "Authentication",
            Action = activityType,
            Description = description,
            UserId = userId,
            OrganisationId = organisationId,
            RequireGuaranteedDelivery = true
        }, cancellationToken);

    public Task RecordOrganisationSwitchAsync(
        int targetOrganisationId, string organisationName, CancellationToken cancellationToken = default)
        => RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Organisation,
            ActivityType = ActivityTypes.UserSwitchedOrganisation,
            Module = "Organisation",
            Action = "SWITCH_ORGANISATION",
            Description = $"Switched active organisation to {ActivitySanitizer.SanitizeDisplayName(organisationName) ?? $"Org #{targetOrganisationId}"}.",
            OrganisationId = targetOrganisationId,
            TargetEntityType = "Organisation",
            TargetEntityId = targetOrganisationId.ToString(),
            TargetDisplayName = organisationName,
            RequireGuaranteedDelivery = true
        }, cancellationToken);

    public Task RecordTeamSwitchAsync(
        int teamId, string teamName, int? organisationId = null, CancellationToken cancellationToken = default)
        => RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Teams,
            ActivityType = ActivityTypes.UserSwitchedTeam,
            Module = "Teams",
            Action = "SWITCH_TEAM",
            Description = $"Switched active team to {ActivitySanitizer.SanitizeDisplayName(teamName) ?? $"Team #{teamId}"}.",
            OrganisationId = organisationId,
            TeamId = teamId,
            TargetEntityType = "Team",
            TargetEntityId = teamId.ToString(),
            TargetDisplayName = teamName
        }, cancellationToken);

    public Task RecordPayrollInitiatedAsync(
        string payrollRunId, string periodLabel, int? organisationId = null, CancellationToken cancellationToken = default)
        => RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Payroll,
            ActivityType = ActivityTypes.PayrollGenerationStarted,
            Module = "Payroll",
            Action = "INITIATE_PAYROLL",
            Description = $"Initiated payroll run {periodLabel}.",
            OrganisationId = organisationId,
            TargetEntityType = "PayrollRun",
            TargetEntityId = payrollRunId,
            TargetDisplayName = periodLabel,
            RequireGuaranteedDelivery = true
        }, cancellationToken);

    public Task RecordTransferSubmittedAsync(
        string transferId, string targetDisplayName, int? organisationId = null, CancellationToken cancellationToken = default)
        => RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Transfers,
            ActivityType = ActivityTypes.EmployeeTransferSubmitted,
            Module = "Transfers",
            Action = "SUBMIT_TRANSFER",
            Description = $"Submitted staff transfer request for {ActivitySanitizer.SanitizeDisplayName(targetDisplayName) ?? transferId}.",
            OrganisationId = organisationId,
            TargetEntityType = "StaffTransfer",
            TargetEntityId = transferId,
            TargetDisplayName = targetDisplayName,
            RequireGuaranteedDelivery = true
        }, cancellationToken);

    public Task RecordWorkflowDecisionAsync(
        ActivityCategory category, string activityType, string module, string action,
        string targetEntityType, string targetEntityId, string? targetDisplayName, string description,
        int? organisationId = null, CancellationToken cancellationToken = default)
        => RecordAsync(new ActivityLogRequest
        {
            Category = category,
            ActivityType = activityType,
            Module = module,
            Action = action,
            Description = description,
            OrganisationId = organisationId,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            TargetDisplayName = targetDisplayName,
            RequireGuaranteedDelivery = true
        }, cancellationToken);

    public Task RecordDocumentDownloadAsync(
        string documentId, string documentTitle, int? organisationId = null, CancellationToken cancellationToken = default)
        => RecordAsync(new ActivityLogRequest
        {
            Category = ActivityCategory.Communication,
            ActivityType = ActivityTypes.DocumentDownloaded,
            Module = "Documents",
            Action = "DOWNLOAD",
            Description = $"Downloaded document '{ActivitySanitizer.SanitizeDisplayName(documentTitle) ?? documentId}'.",
            OrganisationId = organisationId,
            TargetEntityType = "Document",
            TargetEntityId = documentId,
            TargetDisplayName = documentTitle
        }, cancellationToken);

    private ActivityLog BuildEntity(ActivityLogRequest request, ActivityActorContext actor)
    {
        var now = clock.UtcNow;
        // Authoritative server-side context takes precedence for ordinary users (no client impersonation).
        var isTrustedSystem = actor.ActorType == ActivityActorTypes.System || actor.IsSuperAdministrator;
        var userId = isTrustedSystem ? (request.UserId ?? actor.UserId) : actor.UserId;
        var orgId = isTrustedSystem ? (request.OrganisationId ?? actor.OrganisationId) : (actor.OrganisationId ?? request.OrganisationId);
        var tenantId = actor.TenantId ?? orgId ?? request.TenantId;
        var divId = request.DivisionId ?? actor.DivisionId;
        var teamId = request.TeamId ?? actor.TeamId;

        return new ActivityLog
        {
            EventId = request.EventId ?? Guid.NewGuid(),
            TenantId = tenantId,
            OrganisationId = orgId,
            DivisionId = divId,
            TeamId = teamId,
            UserId = userId,
            ActorType = string.IsNullOrWhiteSpace(request.ActorType) ? actor.ActorType : request.ActorType.Trim(),
            ActivityCategory = request.Category.ToString(),
            ActivityType = ActivitySanitizer.Truncate(string.IsNullOrWhiteSpace(request.ActivityType) ? request.Action : request.ActivityType.Trim(), 100) ?? "Activity",
            Module = ActivitySanitizer.Truncate(string.IsNullOrWhiteSpace(request.Module) ? request.Category.ToString() : request.Module.Trim(), 64) ?? "General",
            Action = ActivitySanitizer.Truncate(string.IsNullOrWhiteSpace(request.Action) ? request.ActivityType : request.Action.Trim().ToUpperInvariant(), 64) ?? "EVENT",
            Description = ActivitySanitizer.Truncate(string.IsNullOrWhiteSpace(request.Description) ? $"{request.Action} {request.TargetEntityType}" : request.Description.Trim(), 1000) ?? "",
            TargetEntityType = ActivitySanitizer.Truncate(request.TargetEntityType?.Trim(), 128),
            TargetEntityId = ActivitySanitizer.Truncate(request.TargetEntityId?.Trim(), 128),
            TargetDisplayName = ActivitySanitizer.SanitizeDisplayName(request.TargetDisplayName),
            CorrelationId = ActivitySanitizer.Truncate(request.CorrelationId ?? actor.CorrelationId, 128),
            SessionId = ActivitySanitizer.ToOpaqueSessionId(actor.OpaqueSessionId),
            Source = ActivitySanitizer.Truncate(request.Source ?? actor.Source ?? _options.DefaultSource, 64) ?? "HRM.Api",
            MetadataJson = ActivitySanitizer.SanitizeMetadata(request.Metadata, _options.MaxMetadataLength),
            OccurredAtUtc = request.OccurredAtUtc?.ToUniversalTime() ?? now,
            CreatedAtUtc = now
        };
    }

    private async Task PersistDirectlyWithIdempotencyAsync(ActivityLog entry, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            if (await db.ActivityLogs.AsNoTracking().AnyAsync(x => x.EventId == entry.EventId, cancellationToken))
                return;

            db.ActivityLogs.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
        {
            // Idempotent duplicate EventId — safe to ignore
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Direct activity log persistence failed for EventId {EventId} ({ActivityType}).", entry.EventId, entry.ActivityType);
        }
    }
}
