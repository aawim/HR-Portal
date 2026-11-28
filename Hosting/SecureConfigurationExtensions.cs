using Microsoft.Extensions.Configuration;

namespace HRM.Hosting;

/// <summary>
/// Loads database connection strings and host secrets from a protected JSON file stored outside the repository.
/// Resolution order:
/// 1. Explicit path via HRM_SECRETS_FILE environment variable or Security:SecretsFilePath configuration.
/// 2. Per-user local secure directory: %LocalAppData%/HRM/secrets/connectionstrings.json.
/// </summary>
public static class SecureConfigurationExtensions
{
    public const string SecretsFileEnvironmentVariable = "HRM_SECRETS_FILE";

    public static string GetDefaultSecretsFilePath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(baseDir, "HRM", "secrets", "connectionstrings.json");
    }

    public static IConfigurationBuilder AddSecureExternalConnectionStrings(this IConfigurationBuilder configuration)
    {
        var explicitPath = Environment.GetEnvironmentVariable(SecretsFileEnvironmentVariable);
        var path = !string.IsNullOrWhiteSpace(explicitPath)
            ? explicitPath.Trim()
            : GetDefaultSecretsFilePath();

        if (!string.IsNullOrWhiteSpace(path))
            configuration.AddJsonFile(path, optional: true, reloadOnChange: true);

        return configuration;
    }
}
