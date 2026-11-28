using System.Net;
using System.Text.Json;
using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class ProductionResilienceTests
{
    private static async Task<WebApplication> BuildTestAppAsync(
        int globalLimitPerMinute = 3,
        string mainConn = "",
        string auditConn = "",
        string activityConn = "")
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Resilience:RateLimiting:Enabled"] = "true",
            ["Resilience:RateLimiting:GlobalPermitLimitPerMinute"] = globalLimitPerMinute.ToString(),
            ["Resilience:RateLimiting:ExportPermitLimitPerMinute"] = "2",
            ["Resilience:RateLimiting:AuthPermitLimitPerMinute"] = "2"
        });

        builder.Services.AddAuthorization();
        builder.Services.AddHrmRateLimiting(builder.Configuration);
        builder.Services.AddHrmHealthChecks(mainConn, auditConn, activityConn);

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapHrmHealthEndpoints();
        app.MapGet("/api/test/ping", () => Results.Ok(new { ok = true })).AllowAnonymous();
        app.MapGet("/api/test/export", () => Results.Ok(new { exported = true }))
            .AllowAnonymous()
            .RequireRateLimiting(HrmRateLimitPolicies.Export);

        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task HealthLive_Returns200AndIsExemptFromRateLimiting()
    {
        await using var app = await BuildTestAppAsync(globalLimitPerMinute: 1);
        var client = app.GetTestClient();

        for (var i = 0; i < 4; i++)
        {
            var resp = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task HealthReady_WhenUnconfigured_Returns503WithoutLeakingConnectionStrings()
    {
        await using var app = await BuildTestAppAsync(
            mainConn: "Server=secret-prod-sql;Password=SuperSecret123;Database=HRM;Connect Timeout=1;",
            auditConn: "",
            activityConn: "");
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SuperSecret123", body);
        Assert.DoesNotContain("secret-prod-sql", body);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("Unhealthy", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RateLimiter_Returns429WhenGlobalOrExportLimitExceeded()
    {
        await using var app = await BuildTestAppAsync(globalLimitPerMinute: 2);
        var client = app.GetTestClient();

        var r1 = await client.GetAsync("/api/test/ping");
        var r2 = await client.GetAsync("/api/test/ping");
        var r3 = await client.GetAsync("/api/test/ping");

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);
        Assert.True(r3.Headers.Contains("Retry-After"));
    }
}
