using Microsoft.AspNetCore.WebUtilities;

namespace HRM.Helpers;

public static class QueryStringReader
{
    public static string? GetString(string uri, string key)
    {
        var query = QueryHelpers.ParseQuery(new Uri(uri).Query);
        return query.TryGetValue(key, out var raw) ? raw.FirstOrDefault() : null;
    }

    public static int? GetInt(string uri, string key)
    {
        var value = GetString(uri, key);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
