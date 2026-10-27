using Microsoft.Data.SqlClient;

namespace HRM.Helpers;

public static class DatabaseConnectionPolicy
{
    public static string Validate(string? connection, bool development, string setting)
    {
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException($"Configure {setting} using deployment configuration or development user-secrets.");
        SqlConnectionStringBuilder options;
        try { options = new SqlConnectionStringBuilder(connection); }
        catch (ArgumentException) { throw new InvalidOperationException($"The {setting} connection configuration is invalid."); }
        var encrypt = options.Encrypt.ToString();
        if (!development && (options.TrustServerCertificate ||
            encrypt.Equals("Optional", StringComparison.OrdinalIgnoreCase) || encrypt.Equals("False", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"{setting} must require encryption and validate the SQL Server certificate outside Development.");
        return connection;
    }
}
