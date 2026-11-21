using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HRM.ActivityLogs.Security;

public static class ActivitySanitizer
{
    private static readonly string[] BlockedMetadataTokens =
    [
        "password", "passwd", "secret", "token", "apikey", "authorization", "cookie",
        "salary", "basicsalary", "grosspay", "netpay", "grosssalary", "netsalary",
        "accountnumber", "iban", "pin", "nationalid", "idnumber", "passport"
    ];

    public static string? SanitizeMetadata(Dictionary<string, object?>? metadata, int maxLength)
    {
        if (metadata is null || metadata.Count == 0) return null;

        var safe = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in metadata)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (IsSensitiveKey(key)) continue;
            safe[key.Trim()] = value;
        }

        if (safe.Count == 0) return null;

        var json = JsonSerializer.Serialize(safe);
        return Truncate(json, maxLength);
    }

    public static string? SanitizeDisplayName(string? value, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var clean = value.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Trim();
        return Truncate(clean, maxLength);
    }

    /// <summary>
    /// Converts any session identifier into a one-way opaque hash prefix so raw session tokens are never stored.
    /// </summary>
    public static string? ToOpaqueSessionId(string? rawSessionId)
    {
        if (string.IsNullOrWhiteSpace(rawSessionId)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawSessionId.Trim()));
        return "sid_" + Convert.ToHexString(bytes.AsSpan(0, 12)).ToLowerInvariant();
    }

    public static string? Truncate(string? value, int maxLength)
    {
        if (value is null) return null;
        if (maxLength <= 0 || value.Length <= maxLength) return value;
        return value[..maxLength];
    }

    private static bool IsSensitiveKey(string key)
    {
        foreach (var token in BlockedMetadataTokens)
        {
            if (key.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
