using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HRM.Api.Infrastructure;

public static class HrmRateLimitPolicies
{
    public const string Export = "hrm-export";
    public const string Auth = "hrm-auth";
}

public sealed class HrmRateLimitingOptions
{
    public const string SectionName = "Resilience:RateLimiting";

    public bool Enabled { get; set; } = true;
    public int GlobalPermitLimitPerMinute { get; set; } = 300;
    public int GlobalQueueLimit { get; set; } = 0;
    public int ExportPermitLimitPerMinute { get; set; } = 10;
    public int AuthPermitLimitPerMinute { get; set; } = 20;
}

public static class ProductionResilienceExtensions
{
    public static IServiceCollection AddHrmRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = new HrmRateLimitingOptions();
        configuration.GetSection(HrmRateLimitingOptions.SectionName).Bind(opts);

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Too many requests. Please slow down and try again shortly."
                }, token);
            };

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!opts.Enabled || httpContext.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetNoLimiter("exempt");

                var partitionKey = ResolvePartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, opts.GlobalPermitLimitPerMinute),
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = Math.Max(0, opts.GlobalQueueLimit),
                        AutoReplenishment = true
                    });
            });

            limiter.AddPolicy(HrmRateLimitPolicies.Export, httpContext =>
            {
                if (!opts.Enabled)
                    return RateLimitPartition.GetNoLimiter("disabled");

                return RateLimitPartition.GetFixedWindowLimiter(
                    "export:" + ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, opts.ExportPermitLimitPerMinute),
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            limiter.AddPolicy(HrmRateLimitPolicies.Auth, httpContext =>
            {
                if (!opts.Enabled)
                    return RateLimitPartition.GetNoLimiter("disabled");

                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    "auth:" + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, opts.AuthPermitLimitPerMinute),
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    public static IServiceCollection AddHrmHealthChecks(
        this IServiceCollection services,
        string mainConnection,
        string auditLogsConnection,
        string activityLogsConnection)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Process is alive."), tags: ["live"])
            .AddCheck("sql-main", new SqlPingHealthCheck(mainConnection), tags: ["ready"])
            .AddCheck("sql-audit-logs", new SqlPingHealthCheck(auditLogsConnection), tags: ["ready"])
            .AddCheck("sql-activity-logs", new SqlPingHealthCheck(activityLogsConnection), tags: ["ready"]);

        return services;
    }

    public static IEndpointRouteBuilder MapHrmHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteSanitizedHealthResponseAsync
        }).AllowAnonymous().DisableRateLimiting();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteSanitizedHealthResponseAsync
        }).AllowAnonymous().DisableRateLimiting();

        return endpoints;
    }

    private static string ResolvePartitionKey(HttpContext context)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User?.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(userId))
            return "user:" + userId;

        return "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }

    private static Task WriteSanitizedHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            timestampUtc = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1)
            })
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private sealed class SqlPingHealthCheck(string connectionString) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return HealthCheckResult.Unhealthy("Database connection is not configured.");

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(cts.Token);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.CommandTimeout = 3;
                await cmd.ExecuteScalarAsync(cts.Token);

                return HealthCheckResult.Healthy("Database reachable.");
            }
            catch
            {
                // Never expose connection strings, server names, or raw SQL exceptions on anonymous readiness probes.
                return HealthCheckResult.Unhealthy("Database connectivity check failed.");
            }
        }
    }
}
