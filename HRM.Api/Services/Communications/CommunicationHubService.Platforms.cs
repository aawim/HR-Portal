using System.Text.Json;
using System.Text.RegularExpressions;
using HRM.Communications.Infrastructure;
using HRM.DTOs.Communications;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Communications;

public sealed partial class CommunicationHubService
{
    public async Task SaveIntegrationAsync(CommunicationIntegrationDto model)
    {
        var org = await RequireAdminAsync();
        if (model.ProviderCode is not ("FACEBOOK_PAGE" or "WHATSAPP" or "MSTEAMS")) throw new InvalidOperationException("Select a supported platform.");
        if (model.Id < 0 || string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length > 120 || !Regex.IsMatch(model.CredentialReference ?? "", "^[A-Za-z0-9_-]{1,256}$"))
            throw new InvalidOperationException("Enter a name and a credential reference containing letters, numbers, hyphens or underscores.");
        if (model.ProviderCode == "WHATSAPP" && (!Regex.IsMatch(model.PhoneNumberId ?? "", "^[0-9]{1,64}$") || !Regex.IsMatch(model.TemplateName ?? "", "^[a-z0-9_]{1,512}$") || !Regex.IsMatch(model.TemplateLanguage ?? "", "^[a-z]{2,3}(_[A-Z]{2})?$")))
            throw new InvalidOperationException("Configure the WhatsApp phone-number ID and an approved template with one text body parameter and its approved language code.");
        await using var db = await factory.CreateDbContextAsync(); await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        var row = model.Id == 0 ? new CommunicationProviderConnection { OrganisationBusinessEntityId = org, ProviderCode = model.ProviderCode, CreatedAtUtc = DateTime.UtcNow } :
            await db.CommunicationProviderConnections.SingleOrDefaultAsync(x => x.CommunicationProviderConnectionId == model.Id && x.OrganisationBusinessEntityId == org && x.ProviderCode == model.ProviderCode)
            ?? throw new UnauthorizedAccessException("Integration is unavailable in this organisation.");
        row.ConnectionName = model.Name.Trim(); row.CredentialReference = model.CredentialReference ?? string.Empty; row.IsEnabled = model.IsEnabled;
        row.ConfigurationJson = model.ProviderCode == "WHATSAPP" ? JsonSerializer.Serialize(new WhatsAppTemplateConfiguration(model.PhoneNumberId, model.TemplateName, model.TemplateLanguage)) : null;
        if (model.Id == 0) db.CommunicationProviderConnections.Add(row);
        await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Configured communication integration {row.ProviderCode} in organisation {org}.");
        await db.SaveChangesAsync();
    }

    public async Task SaveDestinationAsync(CommunicationDestinationDto model)
    {
        var org = await RequireAdminAsync(); var actor = await access.RequireContextAsync();
        if (model.Id < 0 || string.IsNullOrWhiteSpace(model.DisplayName) || model.DisplayName.Trim().Length > 160 || string.IsNullOrWhiteSpace(model.ExternalId) || model.ExternalId.Length > 256 || model.LanguageCode is not ("en" or "dv"))
            throw new InvalidOperationException("Enter a destination name, identifier and English or Dhivehi language.");
        await using var db = await factory.CreateDbContextAsync(); await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var connection = await db.CommunicationProviderConnections.SingleOrDefaultAsync(x => x.CommunicationProviderConnectionId == model.ConnectionId && x.OrganisationBusinessEntityId == org)
            ?? throw new UnauthorizedAccessException("Select an integration in this organisation.");
        if (connection.ProviderCode is not ("MSTEAMS" or "FACEBOOK_PAGE" or "WHATSAPP")) throw new InvalidOperationException("Use Telegram setup for Telegram destinations.");
        var externalId = model.ExternalId.Trim();
        if (connection.ProviderCode == "FACEBOOK_PAGE" && !Regex.IsMatch(externalId, "^[0-9]{1,64}$")) throw new InvalidOperationException("Enter the numeric Facebook Page ID.");
        if (connection.ProviderCode == "MSTEAMS" && externalId != "workflow") throw new InvalidOperationException("Use workflow as the destination identifier; its URL belongs in the protected credential store.");
        if (connection.ProviderCode == "WHATSAPP")
        {
            externalId = externalId.TrimStart('+');
            if (!Regex.IsMatch(externalId, "^[1-9][0-9]{6,14}$") || model.IndividualId is not > 0) throw new InvalidOperationException("Select an employee and enter their international phone number.");
            await HRM.Helpers.StaffDataAccess.RequireIndividualAsync(db, access, model.IndividualId.Value);
            if (!await db.Jobs.AnyAsync(j => j.IndividualID == model.IndividualId && j.OrganisationID == org && j.JobStateId == 4)) throw new InvalidOperationException("The employee must belong to this organisation.");
            if (model.ConsentConfirmed && (string.IsNullOrWhiteSpace(model.ConsentSource) || model.ConsentSource.Length > 400)) throw new InvalidOperationException("Record the employee opt-in evidence/source (up to 400 characters).");
        }
        var row = model.Id == 0 ? new CommunicationDestination { OrganisationBusinessEntityId = org } :
            await db.CommunicationDestinations.SingleOrDefaultAsync(x => x.CommunicationDestinationId == model.Id && x.OrganisationBusinessEntityId == org)
            ?? throw new UnauthorizedAccessException("Destination is unavailable in this organisation.");
        if (model.Id != 0 && !await db.CommunicationProviderConnections.AnyAsync(c => c.CommunicationProviderConnectionId == row.CommunicationProviderConnectionId && c.ProviderCode == connection.ProviderCode && c.OrganisationBusinessEntityId == org))
            throw new InvalidOperationException("Create a new destination when changing platforms.");
        var identityChanged = row.ExternalDestinationId != externalId || row.IndividualId != model.IndividualId || row.CommunicationProviderConnectionId != model.ConnectionId;
        row.CommunicationProviderConnectionId = model.ConnectionId; row.DisplayName = model.DisplayName.Trim(); row.ExternalDestinationId = externalId;
        row.IndividualId = connection.ProviderCode == "WHATSAPP" ? model.IndividualId : null; row.LanguageCode = model.LanguageCode;
        row.DestinationType = connection.ProviderCode; row.IsActive = model.IsActive && (connection.ProviderCode != "WHATSAPP" || model.ConsentConfirmed);
        if (model.Id == 0) db.CommunicationDestinations.Add(row);
        await db.SaveChangesAsync();
        var active = await db.CommunicationConsents.Where(x => x.CommunicationDestinationId == row.CommunicationDestinationId && x.OrganisationBusinessEntityId == org && x.RevokedAtUtc == null).ToListAsync();
        if (identityChanged || !model.ConsentConfirmed) foreach (var consent in active) consent.RevokedAtUtc = DateTime.UtcNow;
        if (connection.ProviderCode == "WHATSAPP" && model.ConsentConfirmed && !active.Any(x => x.RevokedAtUtc == null))
            db.CommunicationConsents.Add(new() { OrganisationBusinessEntityId = org, CommunicationDestinationId = row.CommunicationDestinationId, IndividualId = model.IndividualId!.Value,
                Source = model.ConsentSource!.Trim(), ConsentedAtUtc = DateTime.UtcNow, RecordedByUserId = actor.UserId });
        await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Configured {connection.ProviderCode} destination {row.CommunicationDestinationId}; consent recorded={model.ConsentConfirmed}.");
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }

    private static async Task LoadPlatformConfigurationAsync(HrmTeContext db, int org, CommunicationHubConfigurationDto result)
    {
        result.QueuedDeliveryCount = await db.CommunicationDeliveries.CountAsync(x => x.OrganisationBusinessEntityId == org && (x.Status == "Pending" || x.Status == "Sending"));
        result.FailedDeliveryCount = await db.CommunicationDeliveries.CountAsync(x => x.OrganisationBusinessEntityId == org && x.Status == "Failed");
        var heartbeat = await db.CommunicationWorkerHeartbeats.AsNoTracking().OrderByDescending(x => x.LastSuccessfulCycleUtc).FirstOrDefaultAsync();
        result.LastWorkerCycleUtc = heartbeat?.LastSuccessfulCycleUtc;
        result.WorkerOutboundEnabled = heartbeat?.OutboundEnabled ?? false;
        var connections = await db.CommunicationProviderConnections.AsNoTracking().Where(x => x.OrganisationBusinessEntityId == org).ToListAsync();
        result.Integrations = connections.Select(x =>
        {
            WhatsAppTemplateConfiguration? options = null;
            try { options = JsonSerializer.Deserialize<WhatsAppTemplateConfiguration>(x.ConfigurationJson ?? "{}"); } catch (JsonException) { }
            return new CommunicationIntegrationDto { Id = x.CommunicationProviderConnectionId, ProviderCode = x.ProviderCode, Name = x.ConnectionName, CredentialReference = x.CredentialReference, IsEnabled = x.IsEnabled,
                PhoneNumberId = options?.PhoneNumberId, TemplateName = options?.TemplateName, TemplateLanguage = options?.Language };
        }).ToList();
        result.Destinations = await (from d in db.CommunicationDestinations.AsNoTracking()
            join c in db.CommunicationProviderConnections.AsNoTracking() on d.CommunicationProviderConnectionId equals c.CommunicationProviderConnectionId
            where d.OrganisationBusinessEntityId == org && c.OrganisationBusinessEntityId == org
            select new CommunicationDestinationDto { Id = d.CommunicationDestinationId, ConnectionId = c.CommunicationProviderConnectionId, ProviderCode = c.ProviderCode, DisplayName = d.DisplayName, ExternalId = d.ExternalDestinationId,
                IndividualId = d.IndividualId, LanguageCode = d.LanguageCode, IsActive = d.IsActive,
                ConsentConfirmed = db.CommunicationConsents.Any(a => a.CommunicationDestinationId == d.CommunicationDestinationId && a.OrganisationBusinessEntityId == org && a.IndividualId == d.IndividualId && a.RevokedAtUtc == null),
                ConsentSource = db.CommunicationConsents.Where(a => a.CommunicationDestinationId == d.CommunicationDestinationId && a.OrganisationBusinessEntityId == org && a.RevokedAtUtc == null).Select(a => a.Source).FirstOrDefault() }).ToListAsync();
    }
}
