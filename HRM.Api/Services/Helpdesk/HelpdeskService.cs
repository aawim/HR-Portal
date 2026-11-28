using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HRM.Api.Services.Helpdesk;
using HRM.DTOs;
using HRM.DTOs.Helpdesk;
using HRM.Models;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class HelpdeskService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IHelpdeskBotEngine botEngine) : HelpdeskServiceContracts, HelpdeskServiceContracts.IOperations
{
    private const string BotDisplayName = "GHR Helpdesk Assistant";

    private async Task<bool> IsHelpdeskOfficerAsync()
    {
        var user = await access.RequireContextAsync();
        return user.IsSuperAdministrator
            || await access.HasPermissionAsync("Helpdesk.Agent")
            || await access.HasPermissionAsync("Helpdesk.Manage");
    }

    private async Task RequireHelpdeskOfficerAsync()
    {
        if (!await IsHelpdeskOfficerAsync())
            throw new UnauthorizedAccessException("You do not have permission to manage Helpdesk tickets.");
    }

    private async Task<int> ResolveRequesterOrganisationIdAsync(HrmTeContext db, int userId)
    {
        var orgId = await access.GetCurrentOrganisationIdAsync();
        if (orgId > 0) return orgId;

        var fromUser = await db.Users.AsNoTracking()
            .Where(u => u.UserId == userId && u.UserOrganisation != null)
            .Select(u => (int?)u.UserOrganisation!.BusinessEntityID)
            .FirstOrDefaultAsync();
        if (fromUser is > 0) return fromUser.Value;

        var firstOrg = await db.Organisations.AsNoTracking()
            .OrderBy(o => o.BusinessEntityID)
            .Select(o => o.BusinessEntityID)
            .FirstOrDefaultAsync();
        if (firstOrg <= 0)
            throw new InvalidOperationException("No active organisation context is available for Helpdesk.");
        return firstOrg;
    }

    private static string ResolveUserDisplayName(string? fullName, string? username, int userId)
        => !string.IsNullOrWhiteSpace(fullName) ? fullName.Trim() : (!string.IsNullOrWhiteSpace(username) ? username.Trim() : $"User #{userId}");

    private async Task RequireSuperAdminOrHelpdeskManageAsync()
    {
        var user = await access.RequireContextAsync();
        if (!user.IsSuperAdministrator && !await access.HasPermissionAsync("Helpdesk.Manage"))
            throw new UnauthorizedAccessException("SuperAdministrator or Helpdesk management permission is required to configure chatbot settings.");
    }

    private async Task<HelpdeskAvailabilityDto> EvaluateAvailabilityAsync(HrmTeContext db, int organisationId)
    {
        var settings = await db.HelpdeskSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
        if (settings == null)
            return new HelpdeskAvailabilityDto(true, true, HelpdeskScopeModes.Global, null);

        if (!settings.IsEnabled)
            return new HelpdeskAvailabilityDto(false, false, settings.ScopeMode, "The Helpdesk Chatbot is currently disabled by the platform administrator.");

        if (string.Equals(settings.ScopeMode, HelpdeskScopeModes.SelectedOrganisations, StringComparison.OrdinalIgnoreCase))
        {
            var orgEnabled = organisationId > 0 && await db.HelpdeskOrganisationAccesses.AsNoTracking()
                .AnyAsync(x => x.OrganisationId == organisationId && x.IsEnabled);
            if (!orgEnabled)
                return new HelpdeskAvailabilityDto(false, true, settings.ScopeMode, "The Helpdesk Chatbot is not currently enabled for your organisation.");
        }

        return new HelpdeskAvailabilityDto(true, true, settings.ScopeMode, null);
    }

    public async Task<HelpdeskAvailabilityDto> GetAvailabilityAsync()
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var orgId = await ResolveRequesterOrganisationIdAsync(db, user.UserId);
        return await EvaluateAvailabilityAsync(db, orgId);
    }

    public async Task<HelpdeskBotSettingsDto> GetBotSettingsAsync()
    {
        await RequireSuperAdminOrHelpdeskManageAsync();
        await using var db = await factory.CreateDbContextAsync();

        var settings = await db.HelpdeskSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
        var inactiveEntityStateIds = await db.BusinessEntityStates.AsNoTracking()
            .Where(s => s.StateName.Contains("Deleted") || s.StateName.Contains("Inactive") || s.StateName.Contains("Closed") || s.StateName.Contains("Disabled"))
            .Select(s => s.BusinessEntityStateId)
            .ToListAsync();

        var orgs = await db.Organisations.AsNoTracking()
            .Where(o => !db.BusinessEntities.Any(b => b.BusinessEntityID == o.BusinessEntityID && b.BusinessEntityStateId.HasValue && inactiveEntityStateIds.Contains(b.BusinessEntityStateId.Value)))
            .OrderBy(o => o.OrganisationName)
            .Select(o => new { o.BusinessEntityID, o.OrganisationName })
            .ToListAsync();

        var enabledMap = await db.HelpdeskOrganisationAccesses.AsNoTracking()
            .ToDictionaryAsync(x => x.OrganisationId, x => x.IsEnabled);

        return new HelpdeskBotSettingsDto
        {
            IsEnabled = settings?.IsEnabled ?? true,
            ScopeMode = settings?.ScopeMode ?? HelpdeskScopeModes.Global,
            UpdatedAtUtc = settings?.UpdatedAtUtc,
            Organisations = orgs.Select(o => new HelpdeskOrganisationToggleDto
            {
                OrganisationId = o.BusinessEntityID,
                OrganisationName = o.OrganisationName,
                IsEnabled = enabledMap.GetValueOrDefault(o.BusinessEntityID, false)
            }).ToList()
        };
    }

    public async Task<ServiceResult> SaveBotSettingsAsync(SaveHelpdeskBotSettingsDto model)
    {
        await RequireSuperAdminOrHelpdeskManageAsync();
        if (model == null) return ServiceResult.Fail("Settings payload is required.");

        var scopeMode = string.Equals(model.ScopeMode, HelpdeskScopeModes.SelectedOrganisations, StringComparison.OrdinalIgnoreCase)
            ? HelpdeskScopeModes.SelectedOrganisations
            : HelpdeskScopeModes.Global;

        var actor = await access.RequireContextAsync();
        var now = DateTime.UtcNow;
        var enabledSet = (model.EnabledOrganisationIds ?? []).Where(id => id > 0).ToHashSet();

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var settings = await db.HelpdeskSettings.FirstOrDefaultAsync(x => x.Id == 1);
        if (settings == null)
        {
            settings = new HelpdeskSettings
            {
                Id = 1,
                IsEnabled = model.IsEnabled,
                ScopeMode = scopeMode,
                UpdatedByUserId = actor.UserId,
                UpdatedAtUtc = now
            };
            db.HelpdeskSettings.Add(settings);
        }
        else
        {
            settings.IsEnabled = model.IsEnabled;
            settings.ScopeMode = scopeMode;
            settings.UpdatedByUserId = actor.UserId;
            settings.UpdatedAtUtc = now;
        }

        var existingRows = await db.HelpdeskOrganisationAccesses.ToListAsync();
        var existingByOrg = existingRows.ToDictionary(x => x.OrganisationId);

        foreach (var row in existingRows)
        {
            var shouldEnable = enabledSet.Contains(row.OrganisationId);
            if (row.IsEnabled != shouldEnable)
            {
                row.IsEnabled = shouldEnable;
                row.UpdatedByUserId = actor.UserId;
                row.UpdatedAtUtc = now;
            }
        }

        var validOrgIds = await db.Organisations.AsNoTracking()
            .Where(o => enabledSet.Contains(o.BusinessEntityID))
            .Select(o => o.BusinessEntityID)
            .ToListAsync();

        foreach (var orgId in validOrgIds)
        {
            if (!existingByOrg.ContainsKey(orgId))
            {
                db.HelpdeskOrganisationAccesses.Add(new HelpdeskOrganisationAccess
                {
                    OrganisationId = orgId,
                    IsEnabled = true,
                    UpdatedByUserId = actor.UserId,
                    UpdatedAtUtc = now
                });
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var summary = !model.IsEnabled
            ? "Helpdesk Chatbot disabled platform-wide."
            : scopeMode == HelpdeskScopeModes.Global
                ? "Helpdesk Chatbot enabled globally for all organisations."
                : $"Helpdesk Chatbot enabled for {validOrgIds.Count} selected organisation{(validOrgIds.Count == 1 ? "" : "s")}.";
        return ServiceResult.Ok(summary);
    }

    private async Task RequireChatbotAvailableAsync(HrmTeContext db, int organisationId)
    {
        var availability = await EvaluateAvailabilityAsync(db, organisationId);
        if (!availability.IsAvailable)
            throw new UnauthorizedAccessException(availability.Message ?? "The Helpdesk Chatbot is currently disabled.");
    }

    public async Task<List<HelpdeskTicketSummaryDto>> GetMyTicketsAsync()
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var orgId = await ResolveRequesterOrganisationIdAsync(db, user.UserId);
        await RequireChatbotAvailableAsync(db, orgId);

        var tickets = await db.HelpdeskTickets.AsNoTracking()
            .Where(t => t.RequesterUserId == user.UserId)
            .OrderByDescending(t => t.UpdatedAtUtc)
            .Take(100)
            .ToListAsync();

        return await BuildSummariesAsync(db, tickets, isAgentView: false);
    }

    public async Task<HelpdeskTicketDetailDto> GetTicketAsync(long ticketId)
    {
        var user = await access.RequireContextAsync();
        var isAgent = await IsHelpdeskOfficerAsync();
        await using var db = await factory.CreateDbContextAsync();

        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        if (ticket.RequesterUserId != user.UserId && !isAgent)
            throw new UnauthorizedAccessException("You do not have access to this Helpdesk ticket.");

        if (!isAgent)
            await RequireChatbotAvailableAsync(db, ticket.OrganisationId);

        var latestPublicId = await db.HelpdeskMessages.AsNoTracking()
            .Where(m => m.TicketId == ticketId && (isAgent || !m.IsInternalNote))
            .Select(m => (long?)m.Id)
            .MaxAsync() ?? 0;

        if (ticket.RequesterUserId == user.UserId && latestPublicId > ticket.RequesterLastReadMessageId)
        {
            ticket.RequesterLastReadMessageId = latestPublicId;
            await db.SaveChangesAsync();
        }
        else if (isAgent && latestPublicId > ticket.AgentLastReadMessageId)
        {
            ticket.AgentLastReadMessageId = latestPublicId;
            await db.SaveChangesAsync();
        }

        return await BuildDetailAsync(db, ticket, user.UserId, isAgent);
    }

    private const int MaxAttachmentsPerMessage = 4;
    private const long MaxAttachmentSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".zip"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif"
    };

    private sealed record ValidatedAttachment(string FileName, string ContentType, byte[] Bytes, bool IsImage);

    private static List<ValidatedAttachment> ValidateAttachments(List<HelpdeskAttachmentUploadDto>? uploads)
    {
        if (uploads == null || uploads.Count == 0) return [];
        if (uploads.Count > MaxAttachmentsPerMessage)
            throw new InvalidOperationException($"You can attach up to {MaxAttachmentsPerMessage} files per message.");

        var list = new List<ValidatedAttachment>(uploads.Count);
        foreach (var item in uploads)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Base64Data)) continue;

            var rawName = System.IO.Path.GetFileName((item.FileName ?? "attachment").Trim());
            if (string.IsNullOrWhiteSpace(rawName)) rawName = "attachment";
            if (rawName.Length > 255) rawName = rawName[^255..];

            var ext = System.IO.Path.GetExtension(rawName);
            if (string.IsNullOrWhiteSpace(ext) || !AllowedAttachmentExtensions.Contains(ext))
                throw new InvalidOperationException($"File type '{ext}' is not supported. Allowed: images (PNG, JPG, WEBP, GIF), PDF, Office documents, CSV, TXT, and ZIP.");

            var b64 = item.Base64Data.Trim();
            var commaIdx = b64.IndexOf(',');
            if (b64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIdx >= 0)
                b64 = b64[(commaIdx + 1)..];

            byte[] bytes;
            try { bytes = Convert.FromBase64String(b64); }
            catch { throw new InvalidOperationException($"Attachment '{rawName}' could not be decoded."); }

            if (bytes.Length == 0)
                throw new InvalidOperationException($"Attachment '{rawName}' is empty.");
            if (bytes.Length > MaxAttachmentSizeBytes)
                throw new InvalidOperationException($"Attachment '{rawName}' exceeds the 5 MB file size limit.");

            var isImage = ImageExtensions.Contains(ext);
            var contentType = string.IsNullOrWhiteSpace(item.ContentType)
                ? (isImage ? $"image/{ext.TrimStart('.').ToLowerInvariant().Replace("jpg", "jpeg")}" : "application/octet-stream")
                : item.ContentType.Trim();
            if (contentType.Length > 120) contentType = contentType[..120];

            list.Add(new ValidatedAttachment(rawName, contentType, bytes, isImage));
        }

        return list;
    }

    public Task<HelpdeskTicketDetailDto> CreateTicketAsync(string category, string subject, string initialMessage)
        => CreateTicketWithAttachmentsAsync(category, subject, initialMessage, []);

    public async Task<HelpdeskTicketDetailDto> CreateTicketWithAttachmentsAsync(string category, string subject, string initialMessage, List<HelpdeskAttachmentUploadDto> attachments)
    {
        var user = await access.RequireContextAsync();
        var cleanCategory = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        if (cleanCategory.Length > 60) cleanCategory = cleanCategory[..60];

        var validAttachments = ValidateAttachments(attachments);
        var cleanBody = (initialMessage ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanBody) && validAttachments.Count == 0)
            throw new InvalidOperationException("Please enter your message or attach a file/screenshot.");
        if (cleanBody.Length > 4000) cleanBody = cleanBody[..4000];

        var defaultSubject = string.IsNullOrWhiteSpace(cleanBody)
            ? $"Attachment: {validAttachments[0].FileName}"
            : (cleanBody.Length > 80 ? cleanBody[..77].TrimEnd() + "..." : cleanBody);

        var cleanSubject = string.IsNullOrWhiteSpace(subject)
            ? defaultSubject
            : subject.Trim();
        if (cleanSubject.Length > 200) cleanSubject = cleanSubject[..200];

        await using var db = await factory.CreateDbContextAsync();
        var orgId = await ResolveRequesterOrganisationIdAsync(db, user.UserId);
        var availability = await EvaluateAvailabilityAsync(db, orgId);
        if (!availability.IsAvailable)
            throw new InvalidOperationException(availability.Message ?? "The Helpdesk Chatbot is currently disabled for your organisation.");
        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == orgId)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? $"Organisation #{orgId}";
        var requesterName = ResolveUserDisplayName(user.FullName, user.Username, user.UserId);
        var activeArticles = await db.HelpdeskArticles.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .ToListAsync();

        var now = DateTime.UtcNow;
        HelpdeskTicket ticket;
        await using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            ticket = new HelpdeskTicket
            {
                TicketNumber = $"HD-{now.AddHours(5):yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                OrganisationId = orgId,
                RequesterUserId = user.UserId,
                Subject = cleanSubject,
                Category = cleanCategory,
                Status = HelpdeskTicketStatuses.BotActive,
                Priority = cleanCategory.Contains("Technical", StringComparison.OrdinalIgnoreCase) ? "High" : "Normal",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.HelpdeskTickets.Add(ticket);
            await db.SaveChangesAsync();

            ticket.TicketNumber = $"HD-{now.AddHours(5):yyyy}-{ticket.Id:D5}";

            var userMsg = new HelpdeskMessage
            {
                TicketId = ticket.Id,
                SenderType = HelpdeskSenderTypes.User,
                SenderUserId = user.UserId,
                SenderName = requesterName,
                Body = cleanBody,
                SentAtUtc = now
            };
            db.HelpdeskMessages.Add(userMsg);
            await db.SaveChangesAsync();

            foreach (var att in validAttachments)
            {
                db.HelpdeskAttachments.Add(new HelpdeskAttachment
                {
                    TicketId = ticket.Id,
                    MessageId = userMsg.Id,
                    UploadedByUserId = user.UserId,
                    FileName = att.FileName,
                    ContentType = att.ContentType,
                    FileSizeBytes = att.Bytes.LongLength,
                    Content = att.Bytes,
                    IsImage = att.IsImage,
                    UploadedAtUtc = now
                });
            }

            ticket.RequesterLastReadMessageId = userMsg.Id;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        var botInput = string.IsNullOrWhiteSpace(cleanBody)
            ? $"User attached file(s): {string.Join(", ", validAttachments.Select(a => a.FileName))}"
            : cleanBody;

        // Let the hybrid bot engine generate the initial reply with live user context
        await RunBotTurnAsync(db, ticket, user, orgName, requesterName, botInput, activeArticles);

        var isAgent = await IsHelpdeskOfficerAsync();
        return await BuildDetailAsync(db, ticket, user.UserId, isAgent);
    }

    public Task<HelpdeskTicketDetailDto> SendUserMessageAsync(long ticketId, string body)
        => SendUserMessageWithAttachmentsAsync(ticketId, body, []);

    public async Task<HelpdeskTicketDetailDto> SendUserMessageWithAttachmentsAsync(long ticketId, string body, List<HelpdeskAttachmentUploadDto> attachments)
    {
        var user = await access.RequireContextAsync();
        var validAttachments = ValidateAttachments(attachments);
        var cleanBody = (body ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanBody) && validAttachments.Count == 0)
            throw new InvalidOperationException("Enter a message or attach a file/screenshot before sending.");
        if (cleanBody.Length > 4000) cleanBody = cleanBody[..4000];

        await using var db = await factory.CreateDbContextAsync();
        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        if (ticket.RequesterUserId != user.UserId)
            throw new UnauthorizedAccessException("You can only reply to your own Helpdesk tickets.");

        await RequireChatbotAvailableAsync(db, ticket.OrganisationId);

        var requesterName = ResolveUserDisplayName(user.FullName, user.Username, user.UserId);
        var now = DateTime.UtcNow;

        if (ticket.Status == HelpdeskTicketStatuses.Resolved)
        {
            ticket.Status = ticket.AssignedAgentUserId.HasValue
                ? HelpdeskTicketStatuses.WaitingForAgent
                : HelpdeskTicketStatuses.BotActive;
            ticket.ResolvedAtUtc = null;
        }

        var userMsg = new HelpdeskMessage
        {
            TicketId = ticket.Id,
            SenderType = HelpdeskSenderTypes.User,
            SenderUserId = user.UserId,
            SenderName = requesterName,
            Body = cleanBody,
            SentAtUtc = now
        };
        db.HelpdeskMessages.Add(userMsg);
        ticket.UpdatedAtUtc = now;
        await db.SaveChangesAsync();

        foreach (var att in validAttachments)
        {
            db.HelpdeskAttachments.Add(new HelpdeskAttachment
            {
                TicketId = ticket.Id,
                MessageId = userMsg.Id,
                UploadedByUserId = user.UserId,
                FileName = att.FileName,
                ContentType = att.ContentType,
                FileSizeBytes = att.Bytes.LongLength,
                Content = att.Bytes,
                IsImage = att.IsImage,
                UploadedAtUtc = now
            });
        }

        ticket.RequesterLastReadMessageId = userMsg.Id;
        await db.SaveChangesAsync();

        // Bot only responds while ticket is in BotActive status
        if (ticket.Status == HelpdeskTicketStatuses.BotActive)
        {
            var orgName = await db.Organisations.AsNoTracking()
                .Where(o => o.BusinessEntityID == ticket.OrganisationId)
                .Select(o => o.OrganisationName)
                .FirstOrDefaultAsync() ?? $"Organisation #{ticket.OrganisationId}";
            var activeArticles = await db.HelpdeskArticles.AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.SortOrder)
                .ToListAsync();
            var botInput = string.IsNullOrWhiteSpace(cleanBody)
                ? $"User attached file(s): {string.Join(", ", validAttachments.Select(a => a.FileName))}"
                : cleanBody;
            await RunBotTurnAsync(db, ticket, user, orgName, requesterName, botInput, activeArticles);
        }

        var isAgent = await IsHelpdeskOfficerAsync();
        return await BuildDetailAsync(db, ticket, user.UserId, isAgent);
    }

    public async Task<HelpdeskTicketDetailDto> EscalateToAgentAsync(long ticketId, string reason)
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        var isAgent = await IsHelpdeskOfficerAsync();
        if (ticket.RequesterUserId != user.UserId && !isAgent)
            throw new UnauthorizedAccessException("You do not have access to escalate this ticket.");
        if (!isAgent)
            await RequireChatbotAvailableAsync(db, ticket.OrganisationId);

        if (ticket.Status != HelpdeskTicketStatuses.AgentAssigned && ticket.Status != HelpdeskTicketStatuses.WaitingForAgent)
        {
            var now = DateTime.UtcNow;
            var cleanReason = string.IsNullOrWhiteSpace(reason) ? "Requester requested a live Helpdesk Officer." : reason.Trim();
            if (cleanReason.Length > 300) cleanReason = cleanReason[..300];

            ticket.Status = HelpdeskTicketStatuses.WaitingForAgent;
            ticket.EscalationReason = cleanReason;
            ticket.EscalatedAtUtc ??= now;
            ticket.ResolvedAtUtc = null;
            ticket.UpdatedAtUtc = now;

            var sysMsg = new HelpdeskMessage
            {
                TicketId = ticket.Id,
                SenderType = HelpdeskSenderTypes.System,
                SenderName = "Helpdesk System",
                Body = "Escalated to a real Helpdesk Officer. Your full conversation history has been shared — an officer will take over shortly.",
                SentAtUtc = now
            };
            db.HelpdeskMessages.Add(sysMsg);
            await db.SaveChangesAsync();
            if (ticket.RequesterUserId == user.UserId)
            {
                ticket.RequesterLastReadMessageId = sysMsg.Id;
                await db.SaveChangesAsync();
            }
        }

        return await BuildDetailAsync(db, ticket, user.UserId, isAgent);
    }

    public async Task<HelpdeskTicketDetailDto> MarkResolvedByUserAsync(long ticketId, int rating)
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        if (ticket.RequesterUserId != user.UserId)
            throw new UnauthorizedAccessException("Only the requester can mark this ticket resolved here.");
        await RequireChatbotAvailableAsync(db, ticket.OrganisationId);

        var now = DateTime.UtcNow;
        ticket.Status = HelpdeskTicketStatuses.Resolved;
        ticket.ResolvedAtUtc = now;
        ticket.UpdatedAtUtc = now;
        if (rating is >= 1 and <= 5) ticket.Rating = rating;

        var sysMsg = new HelpdeskMessage
        {
            TicketId = ticket.Id,
            SenderType = HelpdeskSenderTypes.System,
            SenderName = "Helpdesk System",
            Body = rating is >= 1 and <= 5
                ? $"Marked as resolved by requester (Rating: {rating}/5)."
                : "Marked as resolved by requester.",
            SentAtUtc = now
        };
        db.HelpdeskMessages.Add(sysMsg);
        await db.SaveChangesAsync();
        ticket.RequesterLastReadMessageId = sysMsg.Id;
        await db.SaveChangesAsync();

        var isAgent = await IsHelpdeskOfficerAsync();
        return await BuildDetailAsync(db, ticket, user.UserId, isAgent);
    }

    public async Task<HelpdeskAgentInboxDto> GetAgentInboxAsync(string statusFilter, int organisationId, string search)
    {
        await RequireHelpdeskOfficerAsync();
        await using var db = await factory.CreateDbContextAsync();

        var counts = await db.HelpdeskTickets.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var query = db.HelpdeskTickets.AsNoTracking().AsQueryable();
        var normalizedFilter = (statusFilter ?? "queue").Trim().ToLowerInvariant();
        query = normalizedFilter switch
        {
            "waiting" => query.Where(t => t.Status == HelpdeskTicketStatuses.WaitingForAgent),
            "active" => query.Where(t => t.Status == HelpdeskTicketStatuses.AgentAssigned),
            "bot" => query.Where(t => t.Status == HelpdeskTicketStatuses.BotActive),
            "resolved" => query.Where(t => t.Status == HelpdeskTicketStatuses.Resolved),
            "all" => query,
            _ => query.Where(t => t.Status == HelpdeskTicketStatuses.WaitingForAgent || t.Status == HelpdeskTicketStatuses.AgentAssigned)
        };

        if (organisationId > 0)
            query = query.Where(t => t.OrganisationId == organisationId);

        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(t =>
                t.TicketNumber.Contains(term) ||
                t.Subject.Contains(term) ||
                t.Category.Contains(term) ||
                db.Users.Any(u => u.UserId == t.RequesterUserId && ((u.FullName != null && u.FullName.Contains(term)) || u.Username.Contains(term))) ||
                db.Organisations.Any(o => o.BusinessEntityID == t.OrganisationId && o.OrganisationName.Contains(term)));
        }

        var ticketRows = await query
            .OrderByDescending(t => t.Status == HelpdeskTicketStatuses.WaitingForAgent)
            .ThenByDescending(t => t.UpdatedAtUtc)
            .Take(150)
            .ToListAsync();

        var orgs = await db.Organisations.AsNoTracking()
            .OrderBy(o => o.OrganisationName)
            .Select(o => new HelpdeskOrganisationOption(o.BusinessEntityID, o.OrganisationName))
            .ToListAsync();

        return new HelpdeskAgentInboxDto
        {
            WaitingCount = counts.FirstOrDefault(c => c.Status == HelpdeskTicketStatuses.WaitingForAgent)?.Count ?? 0,
            AgentAssignedCount = counts.FirstOrDefault(c => c.Status == HelpdeskTicketStatuses.AgentAssigned)?.Count ?? 0,
            BotActiveCount = counts.FirstOrDefault(c => c.Status == HelpdeskTicketStatuses.BotActive)?.Count ?? 0,
            ResolvedCount = counts.FirstOrDefault(c => c.Status == HelpdeskTicketStatuses.Resolved)?.Count ?? 0,
            BotEngineMode = botEngine.EngineMode,
            Organisations = orgs,
            Tickets = await BuildSummariesAsync(db, ticketRows, isAgentView: true)
        };
    }

    private static bool IsValidPinFormat(string? pin) =>
        !string.IsNullOrWhiteSpace(pin) && pin.Length is >= 4 and <= 8 && pin.All(char.IsDigit);

    private static bool VerifyPinHash(string pin, string storedHash, string storedSalt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            var expectedBytes = Convert.FromBase64String(storedHash);
            var actualBytes = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pin), saltBytes, 100_000, HashAlgorithmName.SHA256, expectedBytes.Length);
            return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }
        catch
        {
            return false;
        }
    }

    private static async Task ValidateOfficerPinAsync(HrmTeContext db, int userId, string? suppliedPin)
    {
        var userSig = await db.UserSignatures.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (userSig == null || string.IsNullOrWhiteSpace(userSig.PinHash) || string.IsNullOrWhiteSpace(userSig.PinSalt))
            throw new InvalidOperationException("Set up your 4–8 digit Personal Security PIN in My Profile before taking over Helpdesk chats.");

        if (userSig.PinLockedUntilUtc.HasValue && userSig.PinLockedUntilUtc.Value > DateTime.UtcNow)
        {
            var remaining = Math.Max(1, (int)Math.Ceiling((userSig.PinLockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes));
            throw new InvalidOperationException($"Your Security PIN is temporarily locked due to too many failed attempts. Try again in {remaining} minute(s).");
        }

        var trimmedPin = suppliedPin?.Trim();
        if (!IsValidPinFormat(trimmedPin) || !VerifyPinHash(trimmedPin!, userSig.PinHash, userSig.PinSalt))
        {
            userSig.PinFailedAttempts++;
            string err;
            if (userSig.PinFailedAttempts >= 5)
            {
                userSig.PinLockedUntilUtc = DateTime.UtcNow.AddMinutes(5);
                userSig.PinFailedAttempts = 0;
                err = "Too many incorrect PIN attempts. Verification is locked for 5 minutes.";
            }
            else
            {
                err = $"Incorrect Security PIN ({5 - userSig.PinFailedAttempts} attempt(s) remaining).";
            }

            userSig.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            throw new InvalidOperationException(err);
        }

        if (userSig.PinFailedAttempts != 0 || userSig.PinLockedUntilUtc.HasValue)
        {
            userSig.PinFailedAttempts = 0;
            userSig.PinLockedUntilUtc = null;
            userSig.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public async Task<HelpdeskTicketDetailDto> TakeOverTicketAsync(long ticketId, string pin)
    {
        await RequireHelpdeskOfficerAsync();
        var agent = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();

        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        await ValidateOfficerPinAsync(db, agent.UserId, pin);

        var agentName = ResolveUserDisplayName(agent.FullName, agent.Username, agent.UserId);
        var now = DateTime.UtcNow;

        ticket.AssignedAgentUserId = agent.UserId;
        ticket.Status = HelpdeskTicketStatuses.AgentAssigned;
        ticket.EscalatedAtUtc ??= now;
        ticket.ResolvedAtUtc = null;
        ticket.UpdatedAtUtc = now;

        var sysMsg = new HelpdeskMessage
        {
            TicketId = ticket.Id,
            SenderType = HelpdeskSenderTypes.System,
            SenderUserId = agent.UserId,
            SenderName = "Helpdesk System",
            Body = $"{agentName} (Helpdesk Officer) has taken over the conversation. Automated bot replies are now paused.",
            SentAtUtc = now
        };
        db.HelpdeskMessages.Add(sysMsg);
        await db.SaveChangesAsync();
        ticket.AgentLastReadMessageId = sysMsg.Id;
        await db.SaveChangesAsync();

        return await BuildDetailAsync(db, ticket, agent.UserId, isAgent: true);
    }

    public Task<HelpdeskTicketDetailDto> SendAgentMessageAsync(long ticketId, string body, bool isInternalNote, string pin)
        => SendAgentMessageWithAttachmentsAsync(ticketId, body, isInternalNote, pin, []);

    public async Task<HelpdeskTicketDetailDto> SendAgentMessageWithAttachmentsAsync(long ticketId, string body, bool isInternalNote, string pin, List<HelpdeskAttachmentUploadDto> attachments)
    {
        await RequireHelpdeskOfficerAsync();
        var agent = await access.RequireContextAsync();
        var validAttachments = ValidateAttachments(attachments);
        var cleanBody = (body ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanBody) && validAttachments.Count == 0)
            throw new InvalidOperationException("Enter a reply or attach a file/screenshot before sending.");
        if (cleanBody.Length > 4000) cleanBody = cleanBody[..4000];

        await using var db = await factory.CreateDbContextAsync();
        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        var agentName = ResolveUserDisplayName(agent.FullName, agent.Username, agent.UserId);
        var now = DateTime.UtcNow;

        var requiresTakeover = !isInternalNote && (ticket.Status != HelpdeskTicketStatuses.AgentAssigned || ticket.AssignedAgentUserId != agent.UserId);
        if (requiresTakeover)
        {
            await ValidateOfficerPinAsync(db, agent.UserId, pin);

            ticket.AssignedAgentUserId = agent.UserId;
            ticket.Status = HelpdeskTicketStatuses.AgentAssigned;
            ticket.EscalatedAtUtc ??= now;
            ticket.ResolvedAtUtc = null;
            db.HelpdeskMessages.Add(new HelpdeskMessage
            {
                TicketId = ticket.Id,
                SenderType = HelpdeskSenderTypes.System,
                SenderUserId = agent.UserId,
                SenderName = "Helpdesk System",
                Body = $"{agentName} (Helpdesk Officer) joined the conversation.",
                SentAtUtc = now
            });
        }

        var msg = new HelpdeskMessage
        {
            TicketId = ticket.Id,
            SenderType = HelpdeskSenderTypes.Agent,
            SenderUserId = agent.UserId,
            SenderName = agentName,
            Body = cleanBody,
            IsInternalNote = isInternalNote,
            SentAtUtc = now
        };
        db.HelpdeskMessages.Add(msg);
        ticket.UpdatedAtUtc = now;
        await db.SaveChangesAsync();

        foreach (var att in validAttachments)
        {
            db.HelpdeskAttachments.Add(new HelpdeskAttachment
            {
                TicketId = ticket.Id,
                MessageId = msg.Id,
                UploadedByUserId = agent.UserId,
                FileName = att.FileName,
                ContentType = att.ContentType,
                FileSizeBytes = att.Bytes.LongLength,
                Content = att.Bytes,
                IsImage = att.IsImage,
                UploadedAtUtc = now
            });
        }

        ticket.AgentLastReadMessageId = msg.Id;
        await db.SaveChangesAsync();

        return await BuildDetailAsync(db, ticket, agent.UserId, isAgent: true);
    }

    public async Task<HelpdeskAttachmentContentDto> DownloadAttachmentAsync(long attachmentId)
    {
        var user = await access.RequireContextAsync();
        var isAgent = await IsHelpdeskOfficerAsync();
        await using var db = await factory.CreateDbContextAsync();

        var att = await db.HelpdeskAttachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId)
            ?? throw new InvalidOperationException("Attachment was not found.");

        var ticket = await db.HelpdeskTickets.AsNoTracking()
            .Where(t => t.Id == att.TicketId)
            .Select(t => new { t.Id, t.RequesterUserId })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        if (ticket.RequesterUserId != user.UserId && !isAgent)
            throw new UnauthorizedAccessException("You do not have permission to access this attachment.");

        if (!isAgent)
        {
            var isInternal = await db.HelpdeskMessages.AsNoTracking()
                .Where(m => m.Id == att.MessageId)
                .Select(m => m.IsInternalNote)
                .FirstOrDefaultAsync();
            if (isInternal)
                throw new UnauthorizedAccessException("You do not have permission to access this attachment.");
        }

        return new HelpdeskAttachmentContentDto(
            att.Id,
            att.FileName,
            att.ContentType,
            att.FileSizeBytes,
            att.IsImage,
            Convert.ToBase64String(att.Content));
    }

    public async Task<HelpdeskTicketDetailDto> ResolveTicketByAgentAsync(long ticketId, string resolutionSummary)
    {
        await RequireHelpdeskOfficerAsync();
        var agent = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();

        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        var agentName = ResolveUserDisplayName(agent.FullName, agent.Username, agent.UserId);
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(resolutionSummary))
        {
            var note = resolutionSummary.Trim();
            if (note.Length > 4000) note = note[..4000];
            db.HelpdeskMessages.Add(new HelpdeskMessage
            {
                TicketId = ticket.Id,
                SenderType = HelpdeskSenderTypes.Agent,
                SenderUserId = agent.UserId,
                SenderName = agentName,
                Body = note,
                SentAtUtc = now
            });
        }

        ticket.Status = HelpdeskTicketStatuses.Resolved;
        ticket.AssignedAgentUserId ??= agent.UserId;
        ticket.ResolvedAtUtc = now;
        ticket.UpdatedAtUtc = now;

        var sysMsg = new HelpdeskMessage
        {
            TicketId = ticket.Id,
            SenderType = HelpdeskSenderTypes.System,
            SenderUserId = agent.UserId,
            SenderName = "Helpdesk System",
            Body = $"Ticket marked as resolved by {agentName}. If you still need help, simply reply to reopen.",
            SentAtUtc = now
        };
        db.HelpdeskMessages.Add(sysMsg);
        await db.SaveChangesAsync();
        ticket.AgentLastReadMessageId = sysMsg.Id;
        await db.SaveChangesAsync();

        return await BuildDetailAsync(db, ticket, agent.UserId, isAgent: true);
    }

    public async Task<HelpdeskTicketDetailDto> ReopenTicketAsync(long ticketId)
    {
        var user = await access.RequireContextAsync();
        var isAgent = await IsHelpdeskOfficerAsync();
        await using var db = await factory.CreateDbContextAsync();

        var ticket = await db.HelpdeskTickets.FirstOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Helpdesk ticket was not found.");

        if (ticket.RequesterUserId != user.UserId && !isAgent)
            throw new UnauthorizedAccessException("You do not have permission to reopen this ticket.");
        if (!isAgent)
            await RequireChatbotAvailableAsync(db, ticket.OrganisationId);

        var now = DateTime.UtcNow;
        ticket.Status = ticket.AssignedAgentUserId.HasValue
            ? HelpdeskTicketStatuses.AgentAssigned
            : HelpdeskTicketStatuses.WaitingForAgent;
        ticket.ResolvedAtUtc = null;
        ticket.UpdatedAtUtc = now;

        db.HelpdeskMessages.Add(new HelpdeskMessage
        {
            TicketId = ticket.Id,
            SenderType = HelpdeskSenderTypes.System,
            SenderUserId = user.UserId,
            SenderName = "Helpdesk System",
            Body = $"Ticket reopened by {ResolveUserDisplayName(user.FullName, user.Username, user.UserId)}.",
            SentAtUtc = now
        });
        await db.SaveChangesAsync();

        return await BuildDetailAsync(db, ticket, user.UserId, isAgent);
    }

    private async Task RequireDeveloperForBotResourcesAsync()
    {
        await access.RequireContextAsync();
        if (!await access.HasUserGroupAsync("Developer") && !await access.HasPermissionAsync("Developer"))
            throw new UnauthorizedAccessException("Only Developers can manage Helpdesk Bot resources.");
    }

    private async Task RequireCanSaveArticleAsync(bool isNewArticle)
    {
        await access.RequireContextAsync();
        var isDeveloper = await access.HasUserGroupAsync("Developer") || await access.HasPermissionAsync("Developer");
        if (isDeveloper) return;

        if (isNewArticle && await IsHelpdeskOfficerAsync())
            return;

        throw new UnauthorizedAccessException(isNewArticle
            ? "Only Helpdesk Officers or Developers can save articles to the Knowledge Base."
            : "Only Developers can edit existing Helpdesk Bot Knowledge Base articles.");
    }

    public async Task<List<HelpdeskArticleDto>> GetArticlesAsync(string search)
    {
        await RequireDeveloperForBotResourcesAsync();
        await using var db = await factory.CreateDbContextAsync();

        var query = db.HelpdeskArticles.AsNoTracking().AsQueryable();
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(a =>
                a.Title.Contains(term) ||
                a.Category.Contains(term) ||
                a.Keywords.Contains(term) ||
                a.Answer.Contains(term));
        }

        return await query
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Title)
            .Select(a => new HelpdeskArticleDto
            {
                Id = a.Id,
                Category = a.Category,
                Title = a.Title,
                Keywords = a.Keywords,
                Answer = a.Answer,
                IsActive = a.IsActive,
                SortOrder = a.SortOrder,
                UpdatedAtUtc = a.UpdatedAtUtc
            })
            .ToListAsync();
    }

    public async Task<ServiceResult> SaveArticleAsync(HelpdeskArticleDto model)
    {
        if (model == null) return ServiceResult.Fail("Article payload is required.");
        await RequireCanSaveArticleAsync(isNewArticle: model.Id == 0);
        if (string.IsNullOrWhiteSpace(model.Title)) return ServiceResult.Fail("Enter an article title.");
        if (string.IsNullOrWhiteSpace(model.Answer)) return ServiceResult.Fail("Enter the bot response answer.");

        var category = string.IsNullOrWhiteSpace(model.Category) ? "General" : model.Category.Trim();
        var title = model.Title.Trim();
        var keywords = (model.Keywords ?? "").Trim();
        var answer = model.Answer.Trim();

        if (category.Length > 60) return ServiceResult.Fail("Category must be 60 characters or fewer.");
        if (title.Length > 200) return ServiceResult.Fail("Title must be 200 characters or fewer.");
        if (keywords.Length > 500) return ServiceResult.Fail("Keywords must be 500 characters or fewer.");
        if (answer.Length > 4000) return ServiceResult.Fail("Answer must be 4,000 characters or fewer.");

        await using var db = await factory.CreateDbContextAsync();
        if (model.Id == 0)
        {
            db.HelpdeskArticles.Add(new HelpdeskArticle
            {
                Category = category,
                Title = title,
                Keywords = keywords,
                Answer = answer,
                IsActive = model.IsActive,
                SortOrder = model.SortOrder,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            var entity = await db.HelpdeskArticles.FirstOrDefaultAsync(a => a.Id == model.Id);
            if (entity == null) return ServiceResult.Fail("Knowledge Base article was not found.");
            entity.Category = category;
            entity.Title = title;
            entity.Keywords = keywords;
            entity.Answer = answer;
            entity.IsActive = model.IsActive;
            entity.SortOrder = model.SortOrder;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return ServiceResult.Ok("Knowledge Base article saved.");
    }

    public async Task<ServiceResult> DeleteArticleAsync(int articleId)
    {
        await RequireDeveloperForBotResourcesAsync();
        await using var db = await factory.CreateDbContextAsync();
        var entity = await db.HelpdeskArticles.FirstOrDefaultAsync(a => a.Id == articleId);
        if (entity == null) return ServiceResult.Fail("Article was not found.");
        db.HelpdeskArticles.Remove(entity);
        await db.SaveChangesAsync();
        return ServiceResult.Ok("Knowledge Base article deleted.");
    }

    private async Task<HelpdeskLiveUserContext> BuildLiveUserContextAsync(HrmTeContext db, HRM.DTOs.UserContext.UserContextDto user, string orgName, string requesterName)
    {
        var job = user.ActiveJob;
        var adminPortalEnabled = await db.Users.AsNoTracking()
            .Where(u => u.UserId == user.UserId)
            .Select(u => u.AdminPortalAccessEnabled)
            .FirstOrDefaultAsync();

        List<HelpdeskLeaveBalanceItem> leaveBalances = [];
        List<HelpdeskRecentLeaveItem> recentLeaves = [];
        if (job != null && job.JobId > 0)
        {
            leaveBalances = await db.JobLeaveTypes.AsNoTracking()
                .Where(jlt => jlt.JobId == job.JobId && jlt.IsValid && jlt.LeaveTypeId != null)
                .OrderBy(jlt => jlt.LeaveType.Name)
                .Select(jlt => new HelpdeskLeaveBalanceItem(
                    jlt.LeaveType.Name,
                    jlt.RemainingDays ?? 0m))
                .ToListAsync();

            recentLeaves = await db.Leaves.AsNoTracking()
                .Where(l => l.JobId == job.JobId)
                .OrderByDescending(l => l.LeaveId)
                .Take(5)
                .Select(l => new HelpdeskRecentLeaveItem(
                    l.LeaveType.Name,
                    l.FromDate,
                    l.ToDate,
                    l.Duration,
                    l.LeaveState.StateName))
                .ToListAsync();
        }

        List<HelpdeskRecentRequestItem> recentRequests = [];
        var individualId = job?.IndividualId > 0 ? job.IndividualId : user.IndividualId;
        if (individualId > 0)
        {
            var rawReqs = await db.Requests.AsNoTracking()
                .Where(r => r.ApplicantBusinessEntityId == individualId)
                .OrderByDescending(r => r.ApplicationDate)
                .ThenByDescending(r => r.RequestId)
                .Take(8)
                .Select(r => new
                {
                    Ref = r.ReferenceNumber ?? ("#" + r.RequestId),
                    TypeName = r.RequestType.TypeName,
                    StateName = r.RequestState.StateName,
                    IsOpen = r.RequestState.IsProcessingState,
                    r.ApplicationDate
                })
                .ToListAsync();

            recentRequests = rawReqs
                .Select(r => new HelpdeskRecentRequestItem(r.Ref, r.TypeName, r.StateName, r.IsOpen, r.ApplicationDate))
                .ToList();
        }

        var roleGroups = user.UserGroups
            .Select(g => g.GroupName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        var teams = job?.Teams
            .Select(t => t.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        return new HelpdeskLiveUserContext(
            requesterName,
            !string.IsNullOrWhiteSpace(job?.OrganisationName) ? job.OrganisationName : orgName,
            !string.IsNullOrWhiteSpace(job?.DepartmentName) ? job.DepartmentName : job?.OrganisationStructureName,
            job?.PositionName,
            job?.JobTypeName,
            job?.EmployeeNumber,
            job?.JoinedDate,
            job?.ServiceDurationText,
            job?.SupervisorName,
            teams,
            adminPortalEnabled,
            user.IsSuperAdministrator,
            roleGroups,
            leaveBalances,
            recentLeaves,
            recentRequests);
    }

    private async Task RunBotTurnAsync(
        HrmTeContext db,
        HelpdeskTicket ticket,
        HRM.DTOs.UserContext.UserContextDto user,
        string orgName,
        string requesterName,
        string userMessage,
        IReadOnlyList<HelpdeskArticle> activeArticles)
    {
        var recentHistory = await db.HelpdeskMessages.AsNoTracking()
            .Where(m => m.TicketId == ticket.Id && !m.IsInternalNote)
            .OrderByDescending(m => m.Id)
            .Take(8)
            .OrderBy(m => m.Id)
            .Select(m => new HelpdeskBotHistoryItem(m.SenderType, m.Body))
            .ToListAsync();

        HelpdeskLiveUserContext? liveContext = null;
        try { liveContext = await BuildLiveUserContextAsync(db, user, orgName, requesterName); }
        catch { /* Keep bot functional even if a secondary lookup fails */ }

        var botResponse = await botEngine.GenerateReplyAsync(new HelpdeskBotRequest(
            orgName,
            requesterName,
            ticket.Category,
            ticket.Subject,
            userMessage,
            ticket.BotUnresolvedTurns,
            recentHistory,
            activeArticles,
            liveContext));

        var now = DateTime.UtcNow;
        var botMsg = new HelpdeskMessage
        {
            TicketId = ticket.Id,
            SenderType = HelpdeskSenderTypes.Bot,
            SenderName = BotDisplayName,
            Body = botResponse.Reply,
            MatchedArticleId = botResponse.MatchedArticleId,
            Confidence = botResponse.Confidence,
            SentAtUtc = now
        };
        db.HelpdeskMessages.Add(botMsg);

        ticket.BotUnresolvedTurns++;
        ticket.UpdatedAtUtc = now;

        if (botResponse.ShouldEscalate)
        {
            ticket.Status = HelpdeskTicketStatuses.WaitingForAgent;
            ticket.EscalationReason = botResponse.EscalationReason ?? "Escalated by Helpdesk Assistant.";
            ticket.EscalatedAtUtc ??= now;

            db.HelpdeskMessages.Add(new HelpdeskMessage
            {
                TicketId = ticket.Id,
                SenderType = HelpdeskSenderTypes.System,
                SenderName = "Helpdesk System",
                Body = "Ticket placed in the live Helpdesk queue. A real Helpdesk Officer will take over shortly.",
                SentAtUtc = now.AddMilliseconds(5)
            });
        }

        await db.SaveChangesAsync();
        var maxMsgId = await db.HelpdeskMessages.AsNoTracking()
            .Where(m => m.TicketId == ticket.Id && !m.IsInternalNote)
            .MaxAsync(m => m.Id);
        ticket.RequesterLastReadMessageId = maxMsgId;
        await db.SaveChangesAsync();
    }

    private async Task<List<HelpdeskTicketSummaryDto>> BuildSummariesAsync(HrmTeContext db, List<HelpdeskTicket> tickets, bool isAgentView)
    {
        if (tickets.Count == 0) return [];

        var orgIds = tickets.Select(t => t.OrganisationId).Distinct().ToList();
        var userIds = tickets.Select(t => t.RequesterUserId)
            .Concat(tickets.Where(t => t.AssignedAgentUserId.HasValue).Select(t => t.AssignedAgentUserId!.Value))
            .Distinct()
            .ToList();
        var ticketIds = tickets.Select(t => t.Id).ToList();

        var orgNames = await db.Organisations.AsNoTracking()
            .Where(o => orgIds.Contains(o.BusinessEntityID))
            .ToDictionaryAsync(o => o.BusinessEntityID, o => o.OrganisationName);

        var userNames = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId, u => ResolveUserDisplayName(u.FullName, u.Username, u.UserId));

        var msgMeta = await db.HelpdeskMessages.AsNoTracking()
            .Where(m => ticketIds.Contains(m.TicketId) && (isAgentView || !m.IsInternalNote))
            .Select(m => new { m.Id, m.TicketId, m.Body, m.SenderType })
            .ToListAsync();

        var byTicket = msgMeta.GroupBy(m => m.TicketId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());

        return tickets.Select(t =>
        {
            byTicket.TryGetValue(t.Id, out var msgs);
            var last = msgs?.LastOrDefault();
            var preview = last == null
                ? ""
                : string.IsNullOrWhiteSpace(last.Body)
                    ? "📎 Attachment"
                    : (last.Body.Length > 100 ? last.Body[..97] + "..." : last.Body);
            var lastRead = isAgentView ? t.AgentLastReadMessageId : t.RequesterLastReadMessageId;
            var unread = msgs == null ? 0 : msgs.Count(m => m.Id > lastRead && (isAgentView ? m.SenderType == HelpdeskSenderTypes.User : m.SenderType != HelpdeskSenderTypes.User));

            return new HelpdeskTicketSummaryDto(
                t.Id,
                t.TicketNumber,
                t.OrganisationId,
                orgNames.GetValueOrDefault(t.OrganisationId, $"Organisation #{t.OrganisationId}"),
                t.RequesterUserId,
                userNames.GetValueOrDefault(t.RequesterUserId, $"User #{t.RequesterUserId}"),
                t.Subject,
                t.Category,
                t.Status,
                t.Priority,
                t.AssignedAgentUserId,
                t.AssignedAgentUserId.HasValue ? userNames.GetValueOrDefault(t.AssignedAgentUserId.Value, $"Officer #{t.AssignedAgentUserId.Value}") : null,
                t.EscalationReason,
                unread,
                preview,
                t.CreatedAtUtc,
                t.UpdatedAtUtc,
                t.EscalatedAtUtc,
                t.ResolvedAtUtc,
                t.Rating);
        }).ToList();
    }

    private async Task<HelpdeskTicketDetailDto> BuildDetailAsync(HrmTeContext db, HelpdeskTicket ticket, int currentUserId, bool isAgent)
    {
        var summary = (await BuildSummariesAsync(db, [ticket], isAgent)).Single();

        var attachments = await db.HelpdeskAttachments.AsNoTracking()
            .Where(a => a.TicketId == ticket.Id)
            .OrderBy(a => a.Id)
            .Select(a => new HelpdeskAttachmentDto(
                a.Id,
                a.MessageId,
                a.FileName,
                a.ContentType,
                a.FileSizeBytes,
                a.IsImage,
                a.UploadedAtUtc))
            .ToListAsync();

        var attachmentsByMsg = attachments
            .GroupBy(a => a.MessageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rawMessages = await db.HelpdeskMessages.AsNoTracking()
            .Where(m => m.TicketId == ticket.Id && (isAgent || !m.IsInternalNote))
            .OrderBy(m => m.Id)
            .Select(m => new
            {
                m.Id,
                m.TicketId,
                m.SenderType,
                m.SenderUserId,
                m.SenderName,
                m.Body,
                m.IsInternalNote,
                m.Confidence,
                m.SentAtUtc
            })
            .ToListAsync();

        var messages = rawMessages.Select(m => new HelpdeskMessageDto(
            m.Id,
            m.TicketId,
            m.SenderType,
            m.SenderUserId,
            m.SenderName,
            m.Body,
            m.IsInternalNote,
            m.SenderUserId.HasValue && m.SenderUserId.Value == currentUserId && m.SenderType != HelpdeskSenderTypes.System,
            m.Confidence,
            m.SentAtUtc,
            attachmentsByMsg.GetValueOrDefault(m.Id))).ToList();

        var isRequester = ticket.RequesterUserId == currentUserId;
        var canEscalate = isRequester && ticket.Status == HelpdeskTicketStatuses.BotActive;
        var canResolve = ticket.Status != HelpdeskTicketStatuses.Resolved && (isRequester || isAgent);

        return new HelpdeskTicketDetailDto(summary, messages, isRequester, isAgent, canEscalate, canResolve);
    }
}
