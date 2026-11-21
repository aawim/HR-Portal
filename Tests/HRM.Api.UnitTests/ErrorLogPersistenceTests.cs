using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class ErrorLogPersistenceTests
{
    private sealed class FakeErrorLogStore : IErrorLogStore
    {
        public List<ErrorLogEntry> Persisted { get; } = [];
        public int FailBatchAttemptsRemaining { get; set; }

        public Task PersistAsync(ErrorLogEntry entry, CancellationToken cancellationToken = default)
        {
            Persisted.Add(entry);
            return Task.CompletedTask;
        }

        public Task PersistBatchAsync(IReadOnlyList<ErrorLogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (FailBatchAttemptsRemaining > 0)
            {
                FailBatchAttemptsRemaining--;
                throw new InvalidOperationException("Simulated transient SQL failure.");
            }

            Persisted.AddRange(entries);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ErrorLogEntry>> LoadRecentAsync(int count, bool includeDeleted = false, bool onlyDeleted = false, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ErrorLogEntry>>(Persisted.Take(count).ToList());

        public Task<ErrorLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Persisted.FirstOrDefault(e => e.Id == id));

        public Task<bool> UpdateResolutionAsync(Guid id, bool isResolved, string? resolvedBy = null, string? notes = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> MarkResolvedAsync(Guid id, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ReopenAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<int> SoftDeleteResolvedAsync(string deletedBy, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    [Fact]
    public async Task ErrorLogService_DoesNotSeedSyntheticSampleOnStartup()
    {
        var service = new ErrorLogService();
        var recent = await service.GetRecentErrorsAsync();
        Assert.Empty(recent);
    }

    [Fact]
    public async Task ErrorLogService_DoesNotReturnStaleRamOnlyErrorsWhenPersistentStoreSucceeds()
    {
        var store = new FakeErrorLogStore();
        var service = new ErrorLogService(store);

        // Stale RAM-only entry (older than 15s, never persisted to store)
        var stale = new ErrorLogEntry
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.AddMinutes(-5),
            TimestampLocal = DateTime.Now.AddMinutes(-5),
            ExceptionType = "System.Exception",
            ErrorMessage = "Stale unpersisted RAM error"
        };
        // Persisted DB entry
        var persisted = new ErrorLogEntry
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow.AddMinutes(-2),
            TimestampLocal = DateTime.Now.AddMinutes(-2),
            ExceptionType = "System.InvalidOperationException",
            ErrorMessage = "Persisted DB error"
        };
        store.Persisted.Add(persisted);

        // Inject stale entry into memory only (simulate failed DB write 5 minutes ago)
        var queue = new ChannelErrorLogQueue();
        var serviceWithQueue = new ErrorLogService(store, queue);
        await serviceWithQueue.LogAsync(stale);

        var recent = await serviceWithQueue.GetRecentErrorsAsync();
        Assert.Single(recent);
        Assert.Equal(persisted.Id, recent[0].Id);
    }

    [Fact]
    public async Task ErrorLogBackgroundProcessor_RetriesTransientBatchFailureAndPersists()
    {
        var store = new FakeErrorLogStore { FailBatchAttemptsRemaining = 1 };
        var queue = new ChannelErrorLogQueue();
        var entry = new ErrorLogEntry
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTime.UtcNow,
            TimestampLocal = DateTime.Now,
            ExceptionType = "System.Exception",
            ErrorMessage = "Transient retry test"
        };
        queue.Enqueue(entry);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        var processor = new ErrorLogBackgroundProcessor(queue, store);
        await processor.StartAsync(cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (store.Persisted.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        await processor.StopAsync(CancellationToken.None);
        Assert.Single(store.Persisted);
        Assert.Equal(entry.Id, store.Persisted[0].Id);
    }
}
