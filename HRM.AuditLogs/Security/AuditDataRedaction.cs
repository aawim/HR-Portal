using HRM.AuditLogs.Models;

namespace HRM.AuditLogs.Security;

public static class AuditDataRedaction
{
    public const string RedactedPlaceholder = "[REDACTED]";
    public const string ConfidentialPlaceholder = "[RESTRICTED]";

    public static bool ShouldRedactAtWrite(string propertyName, AuditLogPolicy policy, AuditEntityRule? entityRule = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return false;

        if (entityRule != null && entityRule.RedactedProperties.Contains(propertyName))
            return true;

        foreach (var pattern in policy.SensitivePropertyPatterns)
        {
            if (propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsConfidentialProperty(string propertyName, AuditLogPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return false;

        foreach (var pattern in policy.ConfidentialPropertyPatterns)
        {
            if (propertyName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static string? Truncate(string? value, int maxLength)
    {
        if (value == null) return null;
        if (maxLength <= 0 || value.Length <= maxLength) return value;
        const string suffix = "...[truncated]";
        var take = Math.Max(0, maxLength - suffix.Length);
        return value[..take] + suffix;
    }
}
