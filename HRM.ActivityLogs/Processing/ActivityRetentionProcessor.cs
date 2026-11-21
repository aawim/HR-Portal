using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using HRM.ActivityLogs.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.ActivityLogs.Processing;

public sealed class ActivityRetentionProcessor(
    IDbContextFactory<ActivityDbContext> dbFactory,
    IActivityClock clock,
    IOptions<ActivityLogOptions> optionsAccessor,
    ILogger<ActivityRetentionProcessor> logger) : BackgroundService
{
    private readonly ActivityLogOptions _options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Clamp(_options.RetentionCheckIntervalHours, 1, 168));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var deleted = await RunCleanupOnceAsync(stoppingToken);
                if (deleted > 0)
                    logger.LogInformation("Activity retention cleanup removed {Count} expired activity log(s).", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Activity retention cleanup failed.");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task<int> RunCleanupOnceAsync(CancellationToken cancellationToken = default)
    {
        if (_options.DefaultRetentionDays <= 0) return 0;

        var now = clock.UtcNow;
        var batchSize = Math.Clamp(_options.RetentionBatchSize, 50, 10_000);
        int totalDeleted = 0;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // 1. Apply per-category retention overrides first
        var overriddenCategories = new HashSet<string>(_options.CategoryRetentionDays.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var (category, days) in _options.CategoryRetentionDays)
        {
            if (days <= 0) continue;
            var cutoff = now.AddDays(-days);
            var expired = await db.ActivityLogs
                .Where(x => x.ActivityCategory == category && x.OccurredAtUtc < cutoff)
                .OrderBy(x => x.OccurredAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (expired.Count > 0)
            {
                db.ActivityLogs.RemoveRange(expired);
                await db.SaveChangesAsync(cancellationToken);
                totalDeleted += expired.Count;
            }
        }

        // 2. Apply default retention for all other categories
        var defaultCutoff = now.AddDays(-_options.DefaultRetentionDays);
        var defaultExpiredQuery = db.ActivityLogs.Where(x => x.OccurredAtUtc < defaultCutoff);
        if (overriddenCategories.Count > 0)
        {
            var excluded = overriddenCategories.ToList();
            defaultExpiredQuery = defaultExpiredQuery.Where(x => !excluded.Contains(x.ActivityCategory));
        }

        var defaultExpired = await defaultExpiredQuery
            .OrderBy(x => x.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (defaultExpired.Count > 0)
        {
            db.ActivityLogs.RemoveRange(defaultExpired);
            await db.SaveChangesAsync(cancellationToken);
            totalDeleted += defaultExpired.Count;
        }

        // 3. Clean up already-processed outbox messages older than 7 days
        var outboxCutoff = now.AddDays(-7);
        var oldOutbox = await db.ActivityOutbox
            .Where(x => x.ProcessedAtUtc != null && x.ProcessedAtUtc < outboxCutoff)
            .OrderBy(x => x.OutboxId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (oldOutbox.Count > 0)
        {
            db.ActivityOutbox.RemoveRange(oldOutbox);
            await db.SaveChangesAsync(cancellationToken);
        }

        return totalDeleted;
    }
}
