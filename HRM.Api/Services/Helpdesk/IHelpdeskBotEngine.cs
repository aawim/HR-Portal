using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Models;

namespace HRM.Api.Services.Helpdesk;

public sealed record HelpdeskBotHistoryItem(string SenderType, string Body);

public sealed record HelpdeskBotRequest(
    string OrganisationName,
    string RequesterName,
    string Category,
    string Subject,
    string UserMessage,
    int PriorBotTurns,
    IReadOnlyList<HelpdeskBotHistoryItem> RecentHistory,
    IReadOnlyList<HelpdeskArticle> ActiveArticles);

public sealed record HelpdeskBotResponse(
    string Reply,
    decimal Confidence,
    bool ShouldEscalate,
    string? EscalationReason,
    int? MatchedArticleId);

public interface IHelpdeskBotEngine
{
    string EngineMode { get; }
    Task<HelpdeskBotResponse> GenerateReplyAsync(HelpdeskBotRequest request, CancellationToken ct = default);
}
