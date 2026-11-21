using HRM.Models;
using HRM.Services.Communications;
using HRM.Communications;
using HRM.Communications.Contracts;
using HRM.Communications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetConnectionString("HrmTeContext")
    ?? throw new InvalidOperationException("A Communications worker connection string is required.");

builder.Services.AddDbContextFactory<HrmTeContext>(options => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddHttpClient<TelegramCommunicationProvider>(client => client.BaseAddress = new Uri("https://api.telegram.org/"))
    .RemoveAllLoggers(); // Telegram embeds the credential in the request URL.
builder.Services.AddScoped<ICommunicationProvider>(provider => provider.GetRequiredService<TelegramCommunicationProvider>());
builder.Services.AddHttpClient<FacebookPageCommunicationProvider>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false }).RemoveAllLoggers();
builder.Services.AddHttpClient<WhatsAppCommunicationProvider>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false }).RemoveAllLoggers();
builder.Services.AddHttpClient<MicrosoftTeamsCommunicationProvider>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false }).RemoveAllLoggers();
builder.Services.AddScoped<ICommunicationProvider>(p => p.GetRequiredService<FacebookPageCommunicationProvider>());
builder.Services.AddScoped<ICommunicationProvider>(p => p.GetRequiredService<WhatsAppCommunicationProvider>());
builder.Services.AddScoped<ICommunicationProvider>(p => p.GetRequiredService<MicrosoftTeamsCommunicationProvider>());
builder.Services.AddHostedService<CommunicationWorker>();

await builder.Build().RunAsync();

internal sealed class CommunicationWorker(IDbContextFactory<HrmTeContext> contexts, IServiceScopeFactory scopes, IConfiguration configuration, ILogger<CommunicationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var db = await contexts.CreateDbContextAsync(stoppingToken);
                await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db, stoppingToken);
                await RecoverAndExpireAsync(db, stoppingToken);
                await CreateDeliveriesAsync(db, stoppingToken);
                if (configuration.GetValue<bool>("Communications:OutboundEnabled"))
                    await SendPendingDeliveriesAsync(db, stoppingToken);
                var instance = configuration["Communications:WorkerInstanceId"] ?? Environment.MachineName;
                if (string.IsNullOrWhiteSpace(instance) || instance.Length > 100) throw new InvalidOperationException("WorkerInstanceId must contain 1 to 100 characters.");
                // One configured instance ID per supervised worker. A stale heartbeat exposes failed cycles/database outages.
                await db.Database.ExecuteSqlInterpolatedAsync($@"MERGE dbo.CM_WorkerHeartbeats WITH (HOLDLOCK) AS target
                    USING (SELECT {instance} AS InstanceId) AS source ON target.InstanceId=source.InstanceId
                    WHEN MATCHED THEN UPDATE SET LastSuccessfulCycleUtc={DateTime.UtcNow}, OutboundEnabled={configuration.GetValue<bool>("Communications:OutboundEnabled")}
                    WHEN NOT MATCHED THEN INSERT (InstanceId,LastSuccessfulCycleUtc,OutboundEnabled) VALUES ({instance},{DateTime.UtcNow},{configuration.GetValue<bool>("Communications:OutboundEnabled")});", stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Communication worker health check failed.");
            }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task CreateDeliveriesAsync(HrmTeContext db, CancellationToken token)
    {
        // SQL retries must replay the complete transaction using a fresh change tracker.
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var routing = await contexts.CreateDbContextAsync(token);
            await using var transaction = await routing.Database.BeginTransactionAsync(token);
            await RouteNextEventAsync(routing, token);
            await transaction.CommitAsync(token);
        });
    }

    private static async Task RouteNextEventAsync(HrmTeContext db, CancellationToken token)
    {
        var item = await db.CommunicationOutbox.OrderBy(x => x.CommunicationOutboxId)
            .FirstOrDefaultAsync(x => x.Status == "Pending" && x.AvailableAtUtc <= DateTime.UtcNow, token);
        if (item is null) return;

        var claimed = await db.CommunicationOutbox.Where(x => x.CommunicationOutboxId == item.CommunicationOutboxId && x.Status == "Pending")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Processing"), token);
        if (claimed != 1) return;
        db.ChangeTracker.Clear();

        if (!await CommunicationEligibility.SourceIsEligibleAsync(db, item, token))
        {
            await CompleteOutboxAsync(db, item.CommunicationOutboxId, token);
            return;
        }

        if (item.AggregateType == "CommunicationTest")
        {
            var destinationId = long.Parse(item.AggregateId, System.Globalization.CultureInfo.InvariantCulture);
            var target = await db.CommunicationDestinations.SingleOrDefaultAsync(x => x.CommunicationDestinationId == destinationId && x.OrganisationBusinessEntityId == item.OrganisationBusinessEntityId, token);
            if (target is null) { await CompleteOutboxAsync(db, item.CommunicationOutboxId, token); return; }
            var test = new CommunicationMessage { CommunicationOutboxId = item.CommunicationOutboxId, OrganisationBusinessEntityId = item.OrganisationBusinessEntityId,
                CommunicationDestinationId = target.CommunicationDestinationId, DestinationConnectionId = target.CommunicationProviderConnectionId,
                DestinationExternalId = target.ExternalDestinationId, DestinationDisplayName = target.DisplayName,
                Subject = "Telegram delivery test", RenderedBody = CommunicationEventCodes.TelegramTestBody, CreatedAtUtc = DateTime.UtcNow };
            db.CommunicationMessages.Add(test);
            await db.SaveChangesAsync(token);
            db.CommunicationDeliveries.Add(new CommunicationDelivery { CommunicationMessageId = test.CommunicationMessageId,
                CommunicationProviderConnectionId = target.CommunicationProviderConnectionId, CommunicationDestinationId = target.CommunicationDestinationId,
                OrganisationBusinessEntityId = item.OrganisationBusinessEntityId, IdempotencyKey = $"{item.EventId:N}:test:{destinationId}", CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync(token);
            await CompleteOutboxAsync(db, item.CommunicationOutboxId, token);
            return;
        }

        var rules = await db.CommunicationAutomationRules.AsNoTracking().Where(x => x.OrganisationBusinessEntityId == item.OrganisationBusinessEntityId &&
            x.EventCode == item.EventCode && x.IsEnabled && x.CommunicationDestinationId != null).OrderBy(x => x.CommunicationAutomationRuleId).ToListAsync(token);
        if (rules.Count == 0)
        {
            await CompleteOutboxAsync(db, item.CommunicationOutboxId, token);
            return;
        }

        var values = ReadTemplateValues(item.PayloadJson);
        foreach (var rule in rules)
        {
        string body;
        try { body = CommunicationMessageRenderer.Render(rule.TemplateBody, values); }
        catch (InvalidOperationException)
        {
            await db.CommunicationOutbox.Where(x => x.CommunicationOutboxId == item.CommunicationOutboxId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed"), token);
            return;
        }
        var snapshot = await (from d in db.CommunicationDestinations
            join c in db.CommunicationProviderConnections on d.CommunicationProviderConnectionId equals c.CommunicationProviderConnectionId
            where d.CommunicationDestinationId == rule.CommunicationDestinationId && d.OrganisationBusinessEntityId == item.OrganisationBusinessEntityId && c.OrganisationBusinessEntityId == item.OrganisationBusinessEntityId && d.IsActive && c.IsEnabled
            select new { Destination = d, Connection = c }).SingleOrDefaultAsync(token);
        if (snapshot is null || !await CommunicationEligibility.CanDeliverToAsync(db, item, snapshot.Destination, snapshot.Connection.ProviderCode, token)) continue;
        if (snapshot.Destination.LanguageCode == "dv" && !string.IsNullOrWhiteSpace(rule.DhivehiTemplateBody))
        {
            try { body = CommunicationMessageRenderer.Render(rule.DhivehiTemplateBody, values); }
            catch (InvalidOperationException)
            {
                await db.CommunicationOutbox.Where(x => x.CommunicationOutboxId == item.CommunicationOutboxId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed"), token);
                return;
            }
        }
        var requiresApproval = rule.RequiresApproval || snapshot.Connection.ProviderCode == "FACEBOOK_PAGE";
        var message = new CommunicationMessage { TemplateVersion = rule.TemplateVersion, ProviderConfigurationJson = snapshot.Connection.ConfigurationJson,
            DestinationConnectionId = snapshot.Destination.CommunicationProviderConnectionId, DestinationExternalId = snapshot.Destination.ExternalDestinationId, DestinationDisplayName = snapshot.Destination.DisplayName,
            CommunicationDestinationId = rule.CommunicationDestinationId, ApprovalStatus = requiresApproval ? "Pending" : "NotRequired", CommunicationAutomationRuleId = rule.CommunicationAutomationRuleId, CommunicationOutboxId = item.CommunicationOutboxId, OrganisationBusinessEntityId = item.OrganisationBusinessEntityId,
            Subject = values.GetValueOrDefault("Title") ?? "Organisation announcement", RenderedBody = body, CreatedAtUtc = DateTime.UtcNow };
        db.CommunicationMessages.Add(message);
        await db.SaveChangesAsync(token);
        if (requiresApproval) continue; // Only the existing workflow may release this snapshot later.

        var destinations = await (from destination in db.CommunicationDestinations
                                  join connection in db.CommunicationProviderConnections on destination.CommunicationProviderConnectionId equals connection.CommunicationProviderConnectionId
                                  where destination.CommunicationDestinationId == rule.CommunicationDestinationId &&
                                        destination.OrganisationBusinessEntityId == item.OrganisationBusinessEntityId && destination.IsActive &&
                                        connection.OrganisationBusinessEntityId == item.OrganisationBusinessEntityId && connection.IsEnabled
                                  select new { destination, connection }).ToListAsync(token);
        foreach (var target in destinations)
        {
            db.CommunicationDeliveries.Add(new CommunicationDelivery
            {
                CommunicationMessageId = message.CommunicationMessageId, CommunicationProviderConnectionId = target.connection.CommunicationProviderConnectionId,
                CommunicationDestinationId = target.destination.CommunicationDestinationId, OrganisationBusinessEntityId = item.OrganisationBusinessEntityId,
                IdempotencyKey = $"{item.EventId:N}:{rule.CommunicationAutomationRuleId}:{target.destination.CommunicationDestinationId}", CreatedAtUtc = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(token);
        }
        await CompleteOutboxAsync(db, item.CommunicationOutboxId, token);
    }

    private async Task SendPendingDeliveriesAsync(HrmTeContext db, CancellationToken token)
    {
        if (!configuration.GetValue<bool>("Communications:OutboundEnabled")) return;
        var delivery = await db.CommunicationDeliveries.OrderBy(x => x.CommunicationDeliveryId).FirstOrDefaultAsync(x => x.Status == "Pending" &&
            (!x.NextRetryAtUtc.HasValue || x.NextRetryAtUtc <= DateTime.UtcNow), token);
        if (delivery is null) return;
        var claimed = await db.CommunicationDeliveries.Where(x => x.CommunicationDeliveryId == delivery.CommunicationDeliveryId && x.Status == "Pending")
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Sending").SetProperty(x => x.NextRetryAtUtc, DateTime.UtcNow.AddMinutes(10)).SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), token);
        if (claimed != 1) return;
        db.ChangeTracker.Clear();

        var message = await db.CommunicationMessages.AsNoTracking().SingleOrDefaultAsync(x => x.CommunicationMessageId == delivery.CommunicationMessageId && x.OrganisationBusinessEntityId == delivery.OrganisationBusinessEntityId, token);
        var connection = await db.CommunicationProviderConnections.AsNoTracking().SingleOrDefaultAsync(x => x.CommunicationProviderConnectionId == delivery.CommunicationProviderConnectionId &&
            x.OrganisationBusinessEntityId == delivery.OrganisationBusinessEntityId && x.IsEnabled, token);
        var destination = await db.CommunicationDestinations.AsNoTracking().SingleOrDefaultAsync(x => x.CommunicationDestinationId == delivery.CommunicationDestinationId && x.CommunicationProviderConnectionId == delivery.CommunicationProviderConnectionId &&
            x.OrganisationBusinessEntityId == delivery.OrganisationBusinessEntityId && x.IsActive, token);
        using var scope = scopes.CreateScope();
        var provider = connection is null ? null : scope.ServiceProvider.GetServices<ICommunicationProvider>().SingleOrDefault(x => x.ProviderCode == connection.ProviderCode);
        var started = DateTime.UtcNow;
        var source = message is null ? null : await db.CommunicationOutbox.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CommunicationOutboxId == message.CommunicationOutboxId && x.OrganisationBusinessEntityId == delivery.OrganisationBusinessEntityId, token);
        var audienceAllowed = source is not null && destination is not null && connection is not null && await CommunicationEligibility.CanDeliverToAsync(db, source, destination, connection.ProviderCode, token);
        var result = !configuration.GetValue<bool>("Communications:OutboundEnabled") ? new ProviderSendResult(false, ErrorCode: "OutboundDisabled") :
            !audienceAllowed ? new ProviderSendResult(false, ErrorCode: "SourceNoLongerEligible") :
            message is null ? new ProviderSendResult(false, ErrorCode: "MessageUnavailable") :
            message.DestinationExternalId != null && (destination?.ExternalDestinationId != message.DestinationExternalId || destination?.CommunicationProviderConnectionId != message.DestinationConnectionId) ? new ProviderSendResult(false, ErrorCode: "DestinationChangedSinceCreation") :
            !CommunicationApprovalPolicy.CanDispatch(message.ApprovalStatus) ? new ProviderSendResult(false, ErrorCode: "MessageApprovalRequired") :
            connection is null ? new ProviderSendResult(false, ErrorCode: "ConnectionDisabledOrUnavailable") :
            destination is null ? new ProviderSendResult(false, ErrorCode: "DestinationDisabledOrUnavailable") :
            provider is null ? new ProviderSendResult(false, ErrorCode: "ProviderNotRegistered") :
            await provider.SendAsync(new CommunicationSendRequest(delivery.IdempotencyKey, connection.CredentialReference, destination.ExternalDestinationId, message.Subject, message.RenderedBody, message.ProviderConfigurationJson, delivery.OrganisationBusinessEntityId), token);

        db.CommunicationDeliveryAttempts.Add(new CommunicationDeliveryAttempt { CommunicationDeliveryId = delivery.CommunicationDeliveryId, AttemptNumber = delivery.AttemptCount + 1,
            StartedAtUtc = started, CompletedAtUtc = DateTime.UtcNow, ResultStatus = result.Succeeded ? "Sent" : "Failed", SanitizedErrorCode = result.ErrorCode });
        var tracked = await db.CommunicationDeliveries.SingleAsync(x => x.CommunicationDeliveryId == delivery.CommunicationDeliveryId, token);
        tracked.ProviderMessageId = result.ProviderMessageId; tracked.LastErrorCode = result.ErrorCode;
        tracked.Status = result.Succeeded ? "Sent" : result.IsTransient && tracked.AttemptCount < 5 ? "Pending" : "Failed";
        tracked.SentAtUtc = result.Succeeded ? DateTime.UtcNow : null;
        tracked.NextRetryAtUtc = result.IsTransient && !result.Succeeded ? DateTime.UtcNow.Add(result.RetryAfter ?? TimeSpan.FromMinutes(Math.Min(30, tracked.AttemptCount * 2))) : null;
        await db.SaveChangesAsync(token);
    }

    private static async Task CompleteOutboxAsync(HrmTeContext db, long id, CancellationToken token) =>
        await db.CommunicationOutbox.Where(x => x.CommunicationOutboxId == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Processed").SetProperty(x => x.ProcessedAtUtc, DateTime.UtcNow), token);

    private async Task RecoverAndExpireAsync(HrmTeContext db, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var oldestAllowed = now.AddHours(-Math.Clamp(configuration.GetValue<int?>("Communications:DeliveryMaxAgeHours") ?? 24, 1, 168));
        await db.CommunicationDeliveries.Where(x => x.Status == "Pending" && x.CreatedAtUtc < oldestAllowed)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed").SetProperty(x => x.LastErrorCode, "DeliveryExpired").SetProperty(x => x.NextRetryAtUtc, (DateTime?)null), token);
        // Telegram cannot reconcile an accepted request after a process crash. Never resend automatically.
        await db.CommunicationDeliveries.Where(x => x.Status == "Sending" && (x.NextRetryAtUtc == null || x.NextRetryAtUtc < now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Failed").SetProperty(x => x.LastErrorCode, "IndeterminateWorkerInterruption").SetProperty(x => x.NextRetryAtUtc, (DateTime?)null), token);
    }

    private static IReadOnlyDictionary<string, string?> ReadTemplateValues(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ValueKind == JsonValueKind.String ? x.Value.GetString() : x.Value.ToString(), StringComparer.Ordinal);
    }
}
