using System.Collections.Concurrent;

namespace HRM.UI.Services;

public sealed record ChatTypingParticipant(
    int UserId,
    string DisplayName,
    string RoleLabel,
    DateTime ExpiresAtUtc);

/// <summary>
/// Real-time in-memory typing indicator hub shared across Blazor Server circuits
/// for Staff Chat ("staff:{conversationId}") and Helpdesk ("helpdesk:{ticketId}").
/// </summary>
public sealed class ChatTypingStateService
{
    private static readonly TimeSpan TypingTtl = TimeSpan.FromSeconds(4.5);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, ChatTypingParticipant>> channels = new(StringComparer.Ordinal);

    public event Action<string>? Changed;

    public void NotifyTyping(string channelKey, int userId, string displayName, string roleLabel = "")
    {
        if (string.IsNullOrWhiteSpace(channelKey) || userId <= 0)
            return;

        var name = string.IsNullOrWhiteSpace(displayName) ? "Someone" : displayName.Trim();
        var expiresAt = DateTime.UtcNow.Add(TypingTtl);
        var entry = new ChatTypingParticipant(userId, name, roleLabel?.Trim() ?? "", expiresAt);

        var channel = channels.GetOrAdd(channelKey, _ => new ConcurrentDictionary<int, ChatTypingParticipant>());
        channel[userId] = entry;

        RaiseChanged(channelKey);

        // Schedule automatic cleanup after TTL expires if no newer keystroke refreshed it.
        _ = ExpireLaterAsync(channelKey, userId, expiresAt);
    }

    public void ClearTyping(string channelKey, int userId)
    {
        if (string.IsNullOrWhiteSpace(channelKey) || userId <= 0)
            return;

        if (channels.TryGetValue(channelKey, out var channel) && channel.TryRemove(userId, out _))
        {
            if (channel.IsEmpty)
                channels.TryRemove(channelKey, out _);

            RaiseChanged(channelKey);
        }
    }

    public IReadOnlyList<ChatTypingParticipant> GetActiveTypists(string channelKey, int excludeUserId)
    {
        if (string.IsNullOrWhiteSpace(channelKey) || !channels.TryGetValue(channelKey, out var channel))
            return [];

        var now = DateTime.UtcNow;
        var active = new List<ChatTypingParticipant>();
        foreach (var kvp in channel)
        {
            if (kvp.Value.ExpiresAtUtc <= now)
            {
                channel.TryRemove(kvp.Key, out _);
                continue;
            }

            if (kvp.Key != excludeUserId)
                active.Add(kvp.Value);
        }

        if (channel.IsEmpty)
            channels.TryRemove(channelKey, out _);

        active.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return active;
    }

    public static string FormatTypingLabel(IReadOnlyList<ChatTypingParticipant> typists)
    {
        if (typists.Count == 0) return "";
        if (typists.Count == 1)
        {
            var t = typists[0];
            return string.IsNullOrWhiteSpace(t.RoleLabel)
                ? $"{t.DisplayName} is typing"
                : $"{t.DisplayName} ({t.RoleLabel}) is typing";
        }
        if (typists.Count == 2)
            return $"{typists[0].DisplayName} and {typists[1].DisplayName} are typing";

        return $"{typists[0].DisplayName}, {typists[1].DisplayName} and {typists.Count - 2} other{(typists.Count - 2 == 1 ? "" : "s")} are typing";
    }

    private async Task ExpireLaterAsync(string channelKey, int userId, DateTime expectedExpiry)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (channels.TryGetValue(channelKey, out var channel)
                && channel.TryGetValue(userId, out var current)
                && current.ExpiresAtUtc <= expectedExpiry)
            {
                if (channel.TryRemove(userId, out _))
                {
                    if (channel.IsEmpty)
                        channels.TryRemove(channelKey, out _);

                    RaiseChanged(channelKey);
                }
            }
        }
        catch { }
    }

    private void RaiseChanged(string channelKey)
    {
        try { Changed?.Invoke(channelKey); }
        catch { }
    }
}
