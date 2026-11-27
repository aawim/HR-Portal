using HRM.DTOs.Communications;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Communications;

public sealed partial class CommunicationHubService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, HRM.Communications.Contracts.ICommunicationConnectionValidator validator, IConfiguration configuration, ILogger<CommunicationHubService> logger, HRM.Services.Interfaces.AppovalWF.IWorkflowEngine workflow, IOperationLogService logs, HRM.Services.Interfaces.AppovalWF.IWorkflowApproverResolver approvers, HRM.Services.Interfaces.AppovalWF.IWorkflowRequestContextResolver workflowContexts) : HRM.Services.Contracts.CommunicationHubContracts.IOperations
{
    public async Task SubmitAnnouncementAsync(long id)
    {
        var org = await RequireAdminAsync();
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var message = await db.CommunicationMessages.SingleOrDefaultAsync(x => x.CommunicationMessageId == id && x.OrganisationBusinessEntityId == org)
            ?? throw new InvalidOperationException("Announcement is not available in this organisation.");
        if (message.ApprovalStatus != "Pending" || message.ApprovalRequestId.HasValue)
            throw new InvalidOperationException("This announcement has already been submitted or is no longer pending.");
        if (!message.CommunicationDestinationId.HasValue)
            throw new InvalidOperationException("This older announcement has no saved destination. Create a new announcement before submission.");
        await ValidateAnnouncementReleaseAsync(db, message);
        var today = DateTime.UtcNow.AddHours(5).Date;
        var job = await db.Jobs.Where(j => j.IndividualID == actor.IndividualId && j.OrganisationID == org && j.JobStateId == 4 &&
            j.JoinedDate <= today && (j.TerminatedDate == null || j.TerminatedDate > today) && db.JobPositions.Any(p => p.JobId == j.JobId &&
                p.JobPositionStateId == 4 && p.FromDate <= today && (p.ToDate == null || p.ToDate > today)))
            .OrderByDescending(j => j.JoinedDate).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Active approved employment is required to submit an announcement.");
        var type = await db.RequestTypes.SingleOrDefaultAsync(x => x.TypeName == "Communication Announcement")
            ?? throw new InvalidOperationException("Configure the Communication Announcement request type and its organisation approval flow first.");
        var audit = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.REQUESTS_CREATE, $"Submitted Communication Hub message {id} for approval.");
        var request = new Request { RequestTypeId = type.RequestTypeId, RequestStateId = 1, ApplicantBusinessEntityId = actor.IndividualId,
            OrganisationBusinessEntityID = org, ApplicationDate = today, RequestEffectiveDate = today,
            OperationLogId = audit.OperationLogId, LastStateChangedByUserId = actor.UserId, LastStateChangedDate = DateTime.UtcNow };
        db.Requests.Add(request);
        await db.SaveChangesAsync();
        request.ReferenceNumber = $"CM-{org}-{request.RequestId}";
        message.ApprovalRequestId = request.RequestId; message.ApprovalJobId = job.JobId;
        await db.SaveChangesAsync();
        await workflow.StartAsync(db, request.RequestId, job.JobId);
        await transaction.CommitAsync();
    }
    public async Task QueueTelegramTestAsync(long id)
    {
        var org = await RequireAdminAsync();
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var allowed = await (from d in db.CommunicationDestinations
            join c in db.CommunicationProviderConnections on d.CommunicationProviderConnectionId equals c.CommunicationProviderConnectionId
            where d.CommunicationDestinationId == id && d.OrganisationBusinessEntityId == org && c.OrganisationBusinessEntityId == org && d.IsActive && c.IsEnabled && c.ProviderCode == "TELEGRAM"
            select d).AnyAsync();
        if (!allowed) throw new InvalidOperationException("Enable a Telegram connection and its destination before requesting a test.");
        var now = DateTime.UtcNow;
        var aggregateId = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (await db.CommunicationOutbox.AnyAsync(x => x.OrganisationBusinessEntityId == org && x.AggregateType == "CommunicationTest" && x.AggregateId == aggregateId && x.OccurredAtUtc > now.AddMinutes(-1)))
            throw new InvalidOperationException("A test was already requested for this destination. Wait one minute before requesting another.");
        db.CommunicationOutbox.Add(new CommunicationOutbox
        {
            EventId = Guid.NewGuid(), OrganisationBusinessEntityId = org,
            EventCode = HRM.Communications.Contracts.CommunicationEventCodes.TelegramTestRequested,
            AggregateType = "CommunicationTest", AggregateId = aggregateId,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { RequestedByUserId = actor.UserId }),
            OccurredAtUtc = now, AvailableAtUtc = now
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Telegram test requested by user {UserId} for destination {DestinationId} in organisation {OrganisationId}.", actor.UserId, id, org);
    }
    public async Task<CommunicationConnectionValidationDto> ValidateTelegramDestinationAsync(long id)
    {
        var org = await RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        var target = await (from d in db.CommunicationDestinations.AsNoTracking()
            join c in db.CommunicationProviderConnections.AsNoTracking() on d.CommunicationProviderConnectionId equals c.CommunicationProviderConnectionId
            where d.CommunicationDestinationId == id && d.OrganisationBusinessEntityId == org && c.OrganisationBusinessEntityId == org && c.ProviderCode == "TELEGRAM"
            select new { c.CredentialReference, d.ExternalDestinationId }).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException("Destination is not available in this organisation.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            var result = await validator.ValidateDestinationAsync(target.CredentialReference, target.ExternalDestinationId, timeout.Token, org);
            return new(result.Succeeded, result.ErrorCode);
        }
        catch (OperationCanceledException) { return new(false, "Telegram_ValidationTimeout"); }
    }
    public async Task RetryDeliveryAsync(long id)
    {
        var org = await RequireAdminAsync();
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var row = await db.CommunicationDeliveries.AsNoTracking().SingleOrDefaultAsync(x => x.CommunicationDeliveryId == id && x.OrganisationBusinessEntityId == org)
            ?? throw new InvalidOperationException("Delivery is not available in this organisation.");
        var maxAge = configuration.GetValue<int?>("Communications:DeliveryMaxAgeHours") ?? 24;
        var now = DateTime.UtcNow;
        if (!HRM.Communications.CommunicationRetryPolicy.CanRetry(row.Status, row.LastErrorCode, row.AttemptCount, row.CreatedAtUtc, now, maxAge))
            throw new InvalidOperationException("This delivery cannot be retried safely or has reached its age or attempt limit.");
        var updated = await db.CommunicationDeliveries.Where(x => x.CommunicationDeliveryId == id && x.OrganisationBusinessEntityId == org &&
            x.Status == "Failed" && x.AttemptCount == row.AttemptCount && x.LastErrorCode == row.LastErrorCode)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Pending").SetProperty(x => x.NextRetryAtUtc, now));
        if (updated != 1) throw new InvalidOperationException("Delivery changed. Refresh the history before retrying.");
        logger.LogInformation("Communication delivery {DeliveryId} requeued by user {UserId} in organisation {OrganisationId}.", id, actor.UserId, org);
    }
    public async Task<CommunicationConnectionValidationDto> ValidateTelegramConnectionAsync(long id)
    {
        var org = await RequireAdminAsync();
        await using var db = await factory.CreateDbContextAsync();
        var connection = await db.CommunicationProviderConnections.AsNoTracking().SingleOrDefaultAsync(x => x.CommunicationProviderConnectionId == id && x.OrganisationBusinessEntityId == org && x.ProviderCode == "TELEGRAM")
            ?? throw new InvalidOperationException("Connection is not available in this organisation.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            var result = await validator.ValidateAsync(connection.CredentialReference, timeout.Token, org);
            return new(result.Succeeded, result.ErrorCode);
        }
        catch (OperationCanceledException) { return new(false, "Telegram_ValidationTimeout"); }
    }
    private async Task<int> RequireAdminAsync()
    {
        var context = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("An authorised organisation context is required.");
        await using var db = await factory.CreateDbContextAsync();
        var admitted = await db.Users.AsNoTracking().AnyAsync(u => u.UserId == context.UserId && u.IsActive && u.LocalUserStateId == 1 &&
            u.AdminPortalAccessEnabled && u.UserOrganisationId != null && u.AdminPortalAccessOrganisationId == u.UserOrganisationId &&
            u.UserOrganisation!.BusinessEntityID == org);
        if (!admitted) throw new UnauthorizedAccessException("Approved Admin Portal access for the current tenant is required.");
        if (!context.IsOrganisationAdmin && !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Communication.Manage"))
            throw new UnauthorizedAccessException("Tenant Administrator authority or Communication.Manage permission is required.");
        return org;
    }

    public async Task<CommunicationHubConfigurationDto> LoadAsync()
    {
        var org = await RequireAdminAsync(); await using var db = await factory.CreateDbContextAsync(); await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        var connections = await db.CommunicationProviderConnections.AsNoTracking().Where(x => x.OrganisationBusinessEntityId == org && x.ProviderCode == "TELEGRAM").OrderBy(x => x.ConnectionName).ToListAsync();
        var result = new CommunicationHubConfigurationDto
        {
            PendingApprovals = await db.CommunicationMessages.AsNoTracking().Where(x => x.OrganisationBusinessEntityId == org && x.ApprovalStatus == "Pending")
                .OrderByDescending(x => x.CreatedAtUtc).Take(50)
                .Select(x => new CommunicationApprovalPreviewDto(x.CommunicationMessageId, x.Subject, x.RenderedBody, x.CreatedAtUtc, x.ApprovalRequestId)).ToListAsync(),
            TelegramConnections = connections.Select(x => new TelegramConnectionDto { Id = x.CommunicationProviderConnectionId, Name = x.ConnectionName, CredentialReference = x.CredentialReference, IsEnabled = x.IsEnabled }).ToList(),
            TelegramDestinations = await db.CommunicationDestinations.AsNoTracking().Where(x => x.OrganisationBusinessEntityId == org).OrderBy(x => x.DisplayName).Select(x => new TelegramDestinationDto { Id = x.CommunicationDestinationId, ConnectionId = x.CommunicationProviderConnectionId, DisplayName = x.DisplayName, ChatId = x.ExternalDestinationId, LanguageCode = x.LanguageCode, IsActive = x.IsActive }).ToListAsync(),
            Rules = await db.CommunicationAutomationRules.AsNoTracking().Where(x => x.OrganisationBusinessEntityId == org).OrderBy(x => x.RuleName).Select(x => new CommunicationRuleDto { Id = x.CommunicationAutomationRuleId, DestinationId = x.CommunicationDestinationId, Name = x.RuleName, EventCode = x.EventCode, TemplateBody = x.TemplateBody, DhivehiTemplateBody = x.DhivehiTemplateBody, TemplateVersion = x.TemplateVersion, RequiresApproval = x.RequiresApproval, IsEnabled = x.IsEnabled }).ToListAsync(),
            RecentDeliveries = await (from delivery in db.CommunicationDeliveries.AsNoTracking()
                                      join destination in db.CommunicationDestinations.AsNoTracking() on delivery.CommunicationDestinationId equals destination.CommunicationDestinationId
                                      join connection in db.CommunicationProviderConnections.AsNoTracking() on delivery.CommunicationProviderConnectionId equals connection.CommunicationProviderConnectionId
                                      where delivery.OrganisationBusinessEntityId == org && destination.OrganisationBusinessEntityId == org && connection.OrganisationBusinessEntityId == org
                                      orderby delivery.CreatedAtUtc descending
                                      select new CommunicationDeliveryHistoryDto { Id = delivery.CommunicationDeliveryId, Provider = connection.ProviderCode, Destination = destination.DisplayName, Status = delivery.Status, Attempts = delivery.AttemptCount, ErrorCode = delivery.LastErrorCode, CreatedAtUtc = delivery.CreatedAtUtc, SentAtUtc = delivery.SentAtUtc }).Take(50).ToListAsync()
        };
        var now = DateTime.UtcNow;
        var maxAge = configuration.GetValue<int?>("Communications:DeliveryMaxAgeHours") ?? 24;
        foreach (var item in result.RecentDeliveries)
            item.CanRetry = HRM.Communications.CommunicationRetryPolicy.CanRetry(item.Status, item.ErrorCode, item.Attempts, item.CreatedAtUtc, now, maxAge);
        await LoadPlatformConfigurationAsync(db, org, result);
        result.TelegramDestinations = result.TelegramDestinations.Where(x => result.Destinations.Any(d => d.Id == x.Id && d.ProviderCode == "TELEGRAM")).ToList();
        return result;
    }

    public async Task SaveTelegramConnectionAsync(TelegramConnectionDto model)
    {
        var org = await RequireAdminAsync(); if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.CredentialReference)) throw new InvalidOperationException("Connection name and credential reference are required.");
        if (model.Id < 0 || model.Name.Trim().Length > 120) throw new InvalidOperationException("Enter a connection name of up to 120 characters.");
        if (model.CredentialReference.Length > 256 || model.CredentialReference.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
            throw new InvalidOperationException("Credential references may contain only letters, numbers, hyphens and underscores. Enter a reference, not the bot token.");
        await using var db = await factory.CreateDbContextAsync(); await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        var row = model.Id == 0 ? new CommunicationProviderConnection { OrganisationBusinessEntityId = org, ProviderCode = "TELEGRAM", CreatedAtUtc = DateTime.UtcNow } : await db.CommunicationProviderConnections.SingleAsync(x => x.CommunicationProviderConnectionId == model.Id && x.OrganisationBusinessEntityId == org && x.ProviderCode == "TELEGRAM");
        row.ConnectionName = model.Name.Trim(); row.CredentialReference = model.CredentialReference.Trim(); row.IsEnabled = model.IsEnabled; if (model.Id == 0) db.CommunicationProviderConnections.Add(row); await db.SaveChangesAsync();
    }

    public async Task SaveTelegramDestinationAsync(TelegramDestinationDto model)
    {
        var org = await RequireAdminAsync(); if (string.IsNullOrWhiteSpace(model.DisplayName) || string.IsNullOrWhiteSpace(model.ChatId)) throw new InvalidOperationException("Destination name and chat ID are required.");
        if (model.Id < 0 || model.ConnectionId <= 0 || model.DisplayName.Trim().Length > 160 || model.ChatId.Trim().Length > 256)
            throw new InvalidOperationException("Select a connection and enter a destination name up to 160 characters and chat ID up to 256 characters.");
        await using var db = await factory.CreateDbContextAsync(); await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        if (!await db.CommunicationProviderConnections.AnyAsync(x => x.CommunicationProviderConnectionId == model.ConnectionId && x.OrganisationBusinessEntityId == org && x.ProviderCode == "TELEGRAM")) throw new InvalidOperationException("Select a Telegram connection in this organisation.");
        if (model.LanguageCode is not ("en" or "dv")) throw new InvalidOperationException("Select English or Dhivehi.");
        var row = model.Id == 0 ? new CommunicationDestination { OrganisationBusinessEntityId = org, CommunicationProviderConnectionId = model.ConnectionId, DestinationType = "TelegramChat" } : await db.CommunicationDestinations.SingleAsync(x => x.CommunicationDestinationId == model.Id && x.OrganisationBusinessEntityId == org);
        if (model.Id != 0 && !await db.CommunicationProviderConnections.AnyAsync(c => c.CommunicationProviderConnectionId == row.CommunicationProviderConnectionId && c.OrganisationBusinessEntityId == org && c.ProviderCode == "TELEGRAM")) throw new InvalidOperationException("Create a new destination when changing platforms.");
        row.LanguageCode = model.LanguageCode;
        row.CommunicationProviderConnectionId = model.ConnectionId; row.DisplayName = model.DisplayName.Trim(); row.ExternalDestinationId = model.ChatId.Trim(); row.IsActive = model.IsActive; if (model.Id == 0) db.CommunicationDestinations.Add(row); await db.SaveChangesAsync();
    }

    public async Task SaveRuleAsync(CommunicationRuleDto model)
    {
        var org = await RequireAdminAsync(); if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.TemplateBody) || model.EventCode is not ("organisation-event.published.v1" or "organisation-event.cancelled.v1" or "employee.schedule-changed.v1")) throw new InvalidOperationException("Enter a name, template, and supported event trigger.");
        if (model.Id < 0 || model.Name.Trim().Length > 160) throw new InvalidOperationException("Enter a rule name of up to 160 characters.");
        HRM.Communications.CommunicationMessageRenderer.Validate(model.TemplateBody, model.EventCode);
        if (!string.IsNullOrWhiteSpace(model.DhivehiTemplateBody)) HRM.Communications.CommunicationMessageRenderer.Validate(model.DhivehiTemplateBody, model.EventCode);
        await using var db = await factory.CreateDbContextAsync(); await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        var selected = await (from d in db.CommunicationDestinations
            join c in db.CommunicationProviderConnections on d.CommunicationProviderConnectionId equals c.CommunicationProviderConnectionId
            where d.CommunicationDestinationId == model.DestinationId && d.OrganisationBusinessEntityId == org && c.OrganisationBusinessEntityId == org
            select new { Destination = d, c.ProviderCode }).SingleOrDefaultAsync();
        if (model.DestinationId.HasValue && selected is null) throw new InvalidOperationException("Select a destination in this organisation.");
        if (selected?.ProviderCode == "FACEBOOK_PAGE" && !model.RequiresApproval) throw new InvalidOperationException("Public Facebook posts require manual approval.");
        if (selected?.ProviderCode == "WHATSAPP" && model.EventCode != HRM.Communications.Contracts.CommunicationEventCodes.EmployeeScheduleChanged)
            throw new InvalidOperationException("WhatsApp currently supports consented employee schedule notifications only.");
        if (model.EventCode == HRM.Communications.Contracts.CommunicationEventCodes.EmployeeScheduleChanged && selected?.Destination.IndividualId is null)
            throw new InvalidOperationException("Private employee updates require an employee-specific destination.");
        if (model.IsEnabled && !model.DestinationId.HasValue) throw new InvalidOperationException("Select a destination before enabling this rule.");
        var row = model.Id == 0 ? new CommunicationAutomationRule { OrganisationBusinessEntityId = org } : await db.CommunicationAutomationRules.SingleAsync(x => x.CommunicationAutomationRuleId == model.Id && x.OrganisationBusinessEntityId == org);
        if (model.Id != 0 && (row.TemplateBody != model.TemplateBody.Trim() || row.DhivehiTemplateBody != model.DhivehiTemplateBody?.Trim())) row.TemplateVersion++;
        row.DhivehiTemplateBody = model.DhivehiTemplateBody?.Trim();
        row.CommunicationDestinationId = model.DestinationId;
        row.RuleName = model.Name.Trim(); row.EventCode = model.EventCode; row.TemplateBody = model.TemplateBody.Trim(); row.RequiresApproval = model.RequiresApproval; row.IsEnabled = model.IsEnabled; if (model.Id == 0) db.CommunicationAutomationRules.Add(row); await db.SaveChangesAsync();
    }
}
