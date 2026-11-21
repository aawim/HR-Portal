using System.Text.Json;
using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Models;
using HRM.ActivityLogs.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.ActivityLogs.Processing;

public sealed class ActivityEventProcessor(
    ActivityQueueChannel queue,
    IDbContextFactory<ActivityDbContext> dbFactory,
    IAuditlessClockAdapter clock,
    IOptions<ActivityLogOptions> optionsAccessor,
    ILogger<ActivityEventProcessor> logger) : BackgroundService
{
    private readonly ActivityLogOptions _options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var outboxTask = RunOutboxLoopAsync(stoppingToken);
        var queueTask = RunQueueLoopAsync(stoppingToken);
        await Task.WhenAll(outboxTask, queueTask);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Complete();
        await base.StopAsync(cancellationToken);
    }

    private async Task RunQueueLoopAsync(CancellationToken ct)
    {
        var batch = new List<ActivityLog>(64);
        try
        {
            while (await queue.Reader.WaitToReadAsync(ct))
            {
                batch.Clear();
                while (batch.Count < 64 && queue.Reader.TryRead(out var item))
                {
                    batch.Add(item);
                }

                if (batch.Count > 0)
                {
                    await PersistBatchWithIdempotencyAsync(batch, CancellationToken.None);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown drain
            batch.Clear();
            while (queue.Reader.TryRead(out var item))
            {
                batch.Add(item);
            }
            if (batch.Count > 0)
            {
                await PersistBatchWithIdempotencyAsync(batch, CancellationToken.None);
            }
        }
    }

    private async Task RunOutboxLoopAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(Math.Clamp(_options.OutboxPollSeconds, 1, 300));
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingOutboxBatchAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Activity outbox poll iteration failed.");
            }

            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task<int> ProcessPendingOutboxBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var messages = await db.ActivityOutbox
            .Where(m => m.ProcessedAtUtc == null &&
                        m.AttemptCount < _options.MaxOutboxRetries &&
                        m.NextAttemptAtUtc <= now)
            .OrderBy(m => m.OutboxId)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return 0;

        int processed = 0;
        foreach (var msg in messages)
        {
            try
            {
                var entry = JsonSerializer.Deserialize<ActivityLog>(msg.PayloadJson)
                            ?? throw new InvalidOperationException("Invalid activity outbox payload.");

                var exists = await db.ActivityLogs.AsNoTracking()
                    .AnyAsync(x => x.EventId == entry.EventId, cancellationToken);

                if (!exists)
                {
                    entry.ActivityLogId = 0;
                    db.ActivityLogs.Add(entry);
                }

                msg.ProcessedAtUtc = clock.UtcNow;
                msg.LastError = null;
                await db.SaveChangesAsync(cancellationToken);
                processed++;
                queue.MarkProcessed();
            }
            catch (Exception ex)
            {
                msg.AttemptCount++;
                msg.LastError = ex.Message.Length > 950 ? ex.Message[..950] : ex.Message;
                var backoffSeconds = Math.Min(300, (int)Math.Pow(2, msg.AttemptCount) * 5);
                msg.NextAttemptAtUtc = clock.UtcNow.AddSeconds(backoffSeconds);
                queue.MarkFailed();

                // Detach failed ActivityLog entry so outbox retry state still persists cleanly.
                foreach (var added in db.ChangeTracker.Entries<ActivityLog>().Where(e => e.State == EntityState.Added).ToList())
                    added.State = EntityState.Detached;

                await db.SaveChangesAsync(cancellationToken);
                logger.LogWarning(ex, "Activity outbox EventId {EventId} failed (attempt {Attempt}/{Max}).",
                    msg.EventId, msg.AttemptCount, _options.MaxOutboxRetries);
            }
        }

        return processed;
    }

    public async Task PersistBatchWithIdempotencyAsync(List<ActivityLog> batch, CancellationToken ct)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var ids = batch.Select(b => b.EventId).Distinct().ToList();
            var existing = (await db.ActivityLogs.AsNoTracking()
                .Where(x => ids.Contains(x.EventId))
                .Select(x => x.EventId)
                .ToListAsync(ct)).ToHashSet();

            var toInsert = batch.Where(b => existing.Add(b.EventId)).ToList();
            if (toInsert.Count > 0)
            {
                db.ActivityLogs.AddRange(toInsert);
                await db.SaveChangesAsync(ct);
            }
            queue.MarkProcessed(batch.Count);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
        {
            // Retry row-by-row to ignore only duplicate EventIds
            foreach (var item in batch)
            {
                try
                {
                    await using var singleDb = await dbFactory.CreateDbContextAsync(ct);
                    if (!await singleDb.ActivityLogs.AsNoTracking().AnyAsync(x => x.EventId == item.EventId, ct))
                    {
                        singleDb.ActivityLogs.Add(item);
                        await singleDb.SaveChangesAsync(ct);
                    }
                    queue.MarkProcessed();
                }
                catch (Exception rowEx)
                {
                    queue.MarkFailed();
                    logger.LogWarning(rowEx, "Failed persisting queued activity EventId {EventId}.", item.EventId);
                }
            }
        }
        catch (Exception ex)
        {
            queue.MarkFailed(batch.Count);
            logger.LogError(ex, "Failed persisting activity batch ({Count} items).", batch.Count);
        }
    }
}

/// <summary>
/// Thin internal wrapper over IActivityClock so processors stay testable without extra dependencies.
/// </summary>
public sealed class IAuditlessClockAdapter(IActivityClock inner)
{
    public DateTime UtcNow => inner.UtcNow;
}
