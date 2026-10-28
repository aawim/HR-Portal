using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HRM.Hosting;

/// <summary>Shared host source; Application and Core remain independent of ASP.NET.</summary>
public static class SecureDataProtection
{
    public static void AddProtectedHostKeys(this IServiceCollection services, IConfiguration config, IHostEnvironment environment, string appName)
    {
        if (!environment.IsDevelopment())
        {
            var hosts = (config["AllowedHosts"] ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (hosts.Length == 0 || hosts.Any(host => host.Contains('*')))
                throw new InvalidOperationException("Configure explicit AllowedHosts for production; wildcards are not permitted.");
            var publicHost = HRM.Application.Transport.DeploymentOrigin.Require(config["Application:PublicOrigin"], "Application:PublicOrigin", production: true).Host;
            if (!hosts.Contains(publicHost, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("AllowedHosts must include the configured public host without a scheme or port.");
        }
        var protection = services.AddDataProtection().SetApplicationName(appName);
        var path = config["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(path))
        {
            if (!environment.IsDevelopment()) throw new InvalidOperationException("Configure DataProtection:KeysPath for this host outside the application directory.");
            return;
        }
        if (!Path.IsPathFullyQualified(path)) throw new InvalidOperationException("DataProtection:KeysPath must be an absolute path.");
        var directory = Path.GetFullPath(path);
        var app = Path.GetFullPath(environment.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (directory.StartsWith(app, StringComparison.OrdinalIgnoreCase) || directory.TrimEnd(Path.DirectorySeparatorChar) == app.TrimEnd(Path.DirectorySeparatorChar))
            throw new InvalidOperationException("Store data-protection keys outside the application directory.");
        var certificatePath = config["DataProtection:CertificatePath"];
        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            if (!Path.IsPathFullyQualified(certificatePath) || Path.GetFullPath(certificatePath).StartsWith(app, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Store the private data-protection certificate outside the application directory.");
            var certificate = new X509Certificate2(certificatePath, config["DataProtection:CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
            if (!certificate.HasPrivateKey || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow || certificate.NotBefore.ToUniversalTime() > DateTime.UtcNow)
            { certificate.Dispose(); throw new InvalidOperationException("The data-protection certificate must be valid and have a private key."); }
            services.AddSingleton(certificate);
            protection.ProtectKeysWithCertificate(certificate);
        }
        else if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        else throw new InvalidOperationException("Configure a data-protection certificate before persisting host keys on this platform.");
        protection.PersistKeysToFileSystem(new DirectoryInfo(directory));
    }
}
