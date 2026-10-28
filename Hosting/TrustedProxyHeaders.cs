using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HRM.Hosting;

public static class TrustedProxyHeaders
{
    public static void UseTrustedProxyHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var policy = context.RequestServices.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
            var remote = context.Connection.RemoteIpAddress;
            var addresses = remote is null ? [] : remote.IsIPv4MappedToIPv6 ? new[] { remote, remote.MapToIPv4() } : new[] { remote };
            var trusted = addresses.Any(ip => policy.KnownProxies.Contains(ip) || policy.KnownNetworks.Any(network => network.Contains(ip)));
            if (!trusted)
                foreach (var name in new[] { "X-Forwarded-Host", "X-Forwarded-Proto", "X-Forwarded-For", "X-Forwarded-Prefix" })
                    context.Request.Headers.Remove(name);
            await next();
        });
        app.UseForwardedHeaders();
    }
    public static void AddTrustedProxyHeaders(this IServiceCollection services, IConfiguration config, Uri publicOrigin)
        => services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.AllowedHosts.Add(publicOrigin.Host);
            // Preserve framework loopback trust; never clear lists or trust an arbitrary forwarded header.
            foreach (var address in (config.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                .Concat(config.GetSection("UiProxy:KnownProxies").Get<string[]>() ?? []))
            {
                if (!IPAddress.TryParse(address, out var proxy)) throw new InvalidOperationException("Configure proxy trust using explicit IP addresses.");
                options.KnownProxies.Add(proxy);
            }
        });
}
