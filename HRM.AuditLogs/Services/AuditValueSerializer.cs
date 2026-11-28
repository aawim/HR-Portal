using System.Globalization;
using System.Text.Json;
using HRM.AuditLogs.Security;

namespace HRM.AuditLogs.Services;

public static class AuditValueSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public static (string? Serialized, string ValueType) Serialize(object? value, int maxLength)
    {
        if (value is null || value == DBNull.Value)
            return (null, "Null");

        var type = value.GetType();
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var typeName = underlying.Name;

        string raw = value switch
        {
            string s => s,
            byte[] bytes => $"[binary {bytes.Length} bytes]",
            DateTime dt => dt.Kind == DateTimeKind.Utc
                ? dt.ToString("O", CultureInfo.InvariantCulture)
                : dt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString("c", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("R", CultureInfo.InvariantCulture),
            float flt => flt.ToString("R", CultureInfo.InvariantCulture),
            Enum e => e.ToString(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => JsonSerializer.Serialize(value, JsonOptions)
        };

        return (AuditDataRedaction.Truncate(raw, maxLength), typeName);
    }

    public static string? SerializeMetadata(object? metadata, string? existingJson, int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(existingJson))
            return AuditDataRedaction.Truncate(existingJson.Trim(), maxLength);

        if (metadata is null)
            return null;

        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        return AuditDataRedaction.Truncate(json, maxLength);
    }

    public static bool AreEqual(object? oldValue, object? newValue)
    {
        if (ReferenceEquals(oldValue, newValue)) return true;
        if (oldValue is null || newValue is null) return false;
        if (oldValue is decimal d1 && newValue is decimal d2) return d1 == d2;
        if (oldValue is byte[] b1 && newValue is byte[] b2) return b1.SequenceEqual(b2);
        return Equals(oldValue, newValue);
    }
}
