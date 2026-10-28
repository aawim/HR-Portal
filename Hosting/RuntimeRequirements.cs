using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;

namespace HRM.Hosting;

public static class RuntimeRequirements
{
    // Security baseline from Microsoft's 25 October 2026 out-of-band servicing release.
    public static readonly Version Minimum = new(8, 0, 33);
    public static void RequireCurrent(IHostEnvironment environment)
    {
        var informational = typeof(ForwardedHeadersMiddleware).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var aspnet = Version.TryParse(informational?.Split('+')[0], out var version) ? version : new Version(0, 0);
        Validate(environment.IsDevelopment(), System.Environment.Version, aspnet);
    }
    public static void Validate(bool development, Version runtime, Version aspnet)
    {
        if (!development && (runtime.Major != 8 || aspnet.Major != 8 || runtime < Minimum || aspnet < Minimum))
            throw new InvalidOperationException("Production requires .NET and ASP.NET Core 8.0.33 or a later .NET 8 servicing update. Update the hosting runtime before starting HRM.");
    }
}
