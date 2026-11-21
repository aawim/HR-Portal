using System.Threading.Channels;
using HRM.ActivityLogs.Models;
using Microsoft.Extensions.Options;

namespace HRM.ActivityLogs.Processing;

public sealed record ActivityQueueSnapshot(
    long EnqueuedCount,
    long ProcessedCount,
    long FailedCount,
    long DroppedCount,
    int ApproximatePending);

public sealed class ActivityQueueChannel
{
    private readonly Channel<ActivityLog> _channel;
    private long _enqueued;
    private long _processed;
    private long _failed;
    private long _dropped;

    public ActivityQueueChannel(IOptions<ActivityLogOptions> optionsAccessor)
    {
        var capacity = Math.Clamp(optionsAccessor.Value.QueueCapacity, 64, 100_000);
        _channel = Channel.CreateBounded<ActivityLog>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelReader<ActivityLog> Reader => _channel.Reader;

    public async ValueTask EnqueueAsync(ActivityLog entry, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(entry, cancellationToken);
        Interlocked.Increment(ref _enqueued);
    }

    public bool TryEnqueue(ActivityLog entry)
    {
        if (_channel.Writer.TryWrite(entry))
        {
            Interlocked.Increment(ref _enqueued);
            return true;
        }
        Interlocked.Increment(ref _dropped);
        return false;
    }

    public void MarkProcessed(int count = 1) => Interlocked.Add(ref _processed, count);
    public void MarkFailed(int count = 1) => Interlocked.Add(ref _failed, count);
    public void Complete() => _channel.Writer.TryComplete();

    public ActivityQueueSnapshot GetSnapshot() => new(
        Interlocked.Read(ref _enqueued),
        Interlocked.Read(ref _processed),
        Interlocked.Read(ref _failed),
        Interlocked.Read(ref _dropped),
        _channel.Reader.CanCount ? _channel.Reader.Count : 0);
}
