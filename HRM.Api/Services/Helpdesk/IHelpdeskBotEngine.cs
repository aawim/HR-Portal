using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Models;

namespace HRM.Api.Services.Helpdesk;

public sealed record HelpdeskBotHistoryItem(string SenderType, string Body);

public sealed record HelpdeskLeaveBalanceItem(string LeaveType, decimal RemainingDays);

public sealed record HelpdeskRecentLeaveItem(string LeaveType, DateTime FromDate, DateTime ToDate, int Duration, string State);

public sealed record HelpdeskRecentRequestItem(string ReferenceNumber, string RequestType, string State, bool IsOpen, DateTime AppliedDate);

public sealed record HelpdeskLiveUserContext(
    string FullName,
    string OrganisationName,
    string? DepartmentName,
    string? PositionName,
    string? JobTypeName,
    string? EmployeeNumber,
    DateTime? JoinedDate,
    string? ServiceDurationText,
    string? SupervisorName,
    IReadOnlyList<string> TeamNames,
    bool AdminPortalAccessEnabled,
    bool IsSuperAdministrator,
    IReadOnlyList<string> RoleGroups,
    IReadOnlyList<HelpdeskLeaveBalanceItem> LeaveBalances,
    IReadOnlyList<HelpdeskRecentLeaveItem> RecentLeaves,
    IReadOnlyList<HelpdeskRecentRequestItem> RecentRequests);

public sealed record HelpdeskBotRequest(
    string OrganisationName,
    string RequesterName,
    string Category,
    string Subject,
    string UserMessage,
    int PriorBotTurns,
    IReadOnlyList<HelpdeskBotHistoryItem> RecentHistory,
    IReadOnlyList<HelpdeskArticle> ActiveArticles,
    HelpdeskLiveUserContext? UserContext = null);

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
