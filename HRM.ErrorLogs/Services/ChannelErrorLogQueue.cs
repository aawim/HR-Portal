using System.Threading.Channels;
using HRM.ErrorLogs.Models;

namespace HRM.ErrorLogs.Services;

/// <summary>
/// Thread-safe bounded channel queue for error log entries.
/// Protects HRM application performance by preventing thread blocking and capping memory consumption.
/// </summary>
public sealed class ChannelErrorLogQueue : IErrorLogQueue
{
    private readonly Channel<ErrorLogEntry> _channel;

    public ChannelErrorLogQueue(int capacity = 5000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false,
            SingleReader = true
        };
        _channel = Channel.CreateBounded<ErrorLogEntry>(options);
    }

    public bool Enqueue(ErrorLogEntry entry)
    {
        if (entry == null) return false;
        return _channel.Writer.TryWrite(entry);
    }

    public ValueTask<ErrorLogEntry> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }

    public IAsyncEnumerable<ErrorLogEntry> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
