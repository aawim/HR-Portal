using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HRM.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRM.Api.Services.Helpdesk;

public sealed class HybridHelpdeskBotEngine(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<HybridHelpdeskBotEngine> logger) : IHelpdeskBotEngine
{
    private static readonly string[] HumanHandoverPhrases =
    [
        "real person", "human", "real agent", "live agent", "live support", "talk to someone",
        "speak to someone", "speak with someone", "helpdesk officer", "support officer",
        "transfer me", "escalate", "connect me to", "not helpful", "didn't help", "did not help",
        "still not working", "still stuck", "cant solve", "cannot solve", "couldn't solve"
    ];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "that", "this", "from", "have", "has", "had", "are", "was", "were",
        "can", "could", "would", "should", "will", "what", "when", "where", "how", "why", "who",
        "please", "help", "need", "want", "my", "our", "your", "you", "about", "into", "onto", "not",
        "tell", "show", "check", "give", "know", "does", "get", "see"
    };

    private static readonly Dictionary<string, string[]> HrSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ot"] = ["overtime"],
        ["overtime"] = ["ot", "extra", "hour"],
        ["leave"] = ["vacation", "annual", "sick", "holiday", "absence", "handover"],
        ["vacation"] = ["leave", "annual"],
        ["sick"] = ["leave", "medical"],
        ["handover"] = ["leave", "cover"],
        ["roster"] = ["shift", "schedule", "duty"],
        ["shift"] = ["roster", "schedule", "duty"],
        ["punch"] = ["attendance", "biometric", "checkin", "checkout"],
        ["checkin"] = ["attendance", "late", "punch"],
        ["attendance"] = ["punch", "late", "ooo", "biometric"],
        ["ooo"] = ["out", "office", "attendance"],
        ["payslip"] = ["salary", "payroll", "pay"],
        ["salary"] = ["payslip", "payroll", "allowance", "deduction"],
        ["payroll"] = ["payslip", "salary"],
        ["loan"] = ["goods", "installment", "deduction"],
        ["login"] = ["access", "password", "account", "portal"],
        ["permission"] = ["access", "role", "unauthorized", "forbidden", "admin"],
        ["unauthorized"] = ["permission", "access", "role"],
        ["role"] = ["permission", "access", "admin"],
        ["letter"] = ["certificate", "document"],
        ["certificate"] = ["letter", "document"],
        ["contract"] = ["document", "position"],
        ["sign"] = ["signature"],
        ["resign"] = ["separation", "termination", "retire", "exit"],
        ["retire"] = ["separation", "resign"],
        ["transfer"] = ["move", "department", "organisation"],
        ["job"] = ["vacancy", "recruitment", "position"],
        ["laptop"] = ["asset", "equipment", "item"],
        ["bug"] = ["error", "crash", "broken", "issue"],
        ["error"] = ["bug", "failed", "crash", "unavailable"]
    };

    private string Provider => (configuration["HelpdeskBot:Provider"] ?? "KnowledgeBase").Trim();
    private string? ApiKey => configuration["HelpdeskBot:ApiKey"];
    private string? Endpoint => configuration["HelpdeskBot:Endpoint"];
    private string Model => (configuration["HelpdeskBot:Model"] ?? "").Trim();

    public string EngineMode => Provider.ToLowerInvariant() switch
    {
        "gemini" when !string.IsNullOrWhiteSpace(ApiKey) => $"Live HRM + KB + Gemini ({(string.IsNullOrWhiteSpace(Model) ? "gemini-2.0-flash" : Model)})",
        "openaicompatible" when !string.IsNullOrWhiteSpace(Endpoint) => $"Live HRM + KB + LLM ({(string.IsNullOrWhiteSpace(Model) ? "configured" : Model)})",
        _ => "Live HRM Context + Knowledge Base (AI-Ready)"
    };

    public async Task<HelpdeskBotResponse> GenerateReplyAsync(HelpdeskBotRequest request, CancellationToken ct = default)
    {
        var messageLower = (request.UserMessage ?? "").Trim().ToLowerInvariant();

        // 1. Immediate escalation when user explicitly asks for a human
        if (HumanHandoverPhrases.Any(p => messageLower.Contains(p, StringComparison.Ordinal)))
        {
            return new HelpdeskBotResponse(
                "No problem — I am transferring your conversation to a real Helpdesk Officer right now. Your conversation history has been shared so you won't need to repeat yourself.",
                0.0m,
                true,
                "Requester asked to speak with a real Helpdesk Officer.",
                null);
        }

        // 2. Retrieve top matching Knowledge Base articles
        var ranked = RankArticles(request.UserMessage ?? "", request.Category, request.ActiveArticles);
        var best = ranked.FirstOrDefault();

        // 3. Optional Grounded LLM call (receives both KB + Live User HRM Context)
        if (Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ApiKey))
        {
            var aiReply = await TryCallGeminiAsync(request, ranked.Take(4).Select(x => x.Article).ToList(), ct);
            if (aiReply != null) return aiReply;
        }
        else if (Provider.Equals("OpenAICompatible", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(Endpoint))
        {
            var aiReply = await TryCallOpenAiCompatibleAsync(request, ranked.Take(4).Select(x => x.Article).ToList(), ct);
            if (aiReply != null) return aiReply;
        }

        // 4. Deterministic Live HRM Personal Context Intents (instant, 100% accurate live data)
        if (request.UserContext != null)
        {
            var liveReply = TryAnswerFromLiveUserContext(messageLower, request.UserContext);
            if (liveReply != null) return liveReply;
        }

        // 5. Deterministic Knowledge Base response with automatic escalation safeguard
        if (request.PriorBotTurns >= 2 && (best == null || best.Confidence < 0.65m))
        {
            return new HelpdeskBotResponse(
                "It looks like this needs personal assistance from our support team. I have escalated your ticket to a real Helpdesk Officer who will review our conversation and take over shortly.",
                best?.Confidence ?? 0.25m,
                true,
                "Escalated automatically after multiple unresolved bot turns.",
                best?.Article.Id);
        }

        if (best != null && best.Confidence >= 0.30m)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**{best.Article.Title}**");
            sb.AppendLine();
            sb.AppendLine(best.Article.Answer.Trim());

            var related = ranked.Skip(1).Where(r => r.Confidence >= 0.25m).Take(2).ToList();
            if (related.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Related guides you can also ask about:");
                foreach (var rel in related)
                    sb.AppendLine($"• {rel.Article.Title}");
            }

            sb.AppendLine();
            sb.Append("Did this solve your question? Click **That Solved It** above, ask a follow-up, or click **Talk to a Real Person** anytime.");
            return new HelpdeskBotResponse(sb.ToString(), best.Confidence, false, null, best.Article.Id);
        }

        // Only escalate immediately on turn 1 if the user explicitly reports a crash/locked account
        var isCriticalBlocker = messageLower.Contains("locked out", StringComparison.Ordinal)
            || messageLower.Contains("account locked", StringComparison.Ordinal)
            || messageLower.Contains("500 error", StringComparison.Ordinal)
            || messageLower.Contains("cannot login", StringComparison.Ordinal)
            || messageLower.Contains("can't login", StringComparison.Ordinal);

        if (isCriticalBlocker || request.PriorBotTurns >= 1)
        {
            return new HelpdeskBotResponse(
                "I could not find an exact self-service answer for your issue, so I have transferred this chat to a real Helpdesk Officer. They can see your organisation and conversation history and will respond here shortly.",
                best?.Confidence ?? 0.15m,
                true,
                "Bot confidence below threshold; automatically handed over to a Helpdesk Officer.",
                null);
        }

        // First-turn helpful guidance showing what the bot knows (live data + top KB guides)
        var fallback = new StringBuilder();
        fallback.AppendLine("I'm not sure I matched your exact question yet, but here's what I can help with right away:");
        fallback.AppendLine();
        fallback.AppendLine("**Live personal lookups:**");
        fallback.AppendLine("• Ask *\"Check my leave balance\"*");
        fallback.AppendLine("• Ask *\"Show my pending requests\"*");
        fallback.AppendLine("• Ask *\"Who is my supervisor / what is my department?\"*");
        fallback.AppendLine("• Ask *\"Check my portal access and roles\"*");
        fallback.AppendLine();
        fallback.AppendLine("**Platform guides:** Leave, Attendance, Duty Roster, Overtime, Payslips, Loans, Item Requests, Assets, Certificates, Contracts, Digital Signature, Transfers, Separation, and Approvals.");
        fallback.AppendLine();
        fallback.Append("Reply with a specific question, or click **Talk to a Real Person** above to connect directly with a Helpdesk Officer.");

        return new HelpdeskBotResponse(fallback.ToString(), 0.25m, false, null, null);
    }

    private static HelpdeskBotResponse? TryAnswerFromLiveUserContext(string msg, HelpdeskLiveUserContext ctx)
    {
        // A. Leave Balance
        if (ContainsAny(msg, "leave balance", "remaining leave", "how many leave", "leave days left", "days of leave", "my balance", "annual leave balance", "sick leave balance", "check my leave"))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**Your Live Leave Balances ({ctx.OrganisationName})**");
            sb.AppendLine();
            if (ctx.LeaveBalances.Count == 0)
            {
                sb.AppendLine("I couldn't find any active leave balance records linked to your current job.");
            }
            else
            {
                foreach (var lb in ctx.LeaveBalances)
                    sb.AppendLine($"• **{lb.LeaveType}**: {lb.RemainingDays:0.##} day(s) remaining");
            }

            var pendingLeaves = ctx.RecentLeaves.Where(l => l.State.Contains("Pending", StringComparison.OrdinalIgnoreCase) || l.State.Contains("Incomplete", StringComparison.OrdinalIgnoreCase)).ToList();
            if (pendingLeaves.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Pending / Draft Leaves:**");
                foreach (var pl in pendingLeaves)
                    sb.AppendLine($"• {pl.LeaveType} ({pl.FromDate:dd MMM} – {pl.ToDate:dd MMM}, {pl.Duration}d) — *{pl.State}*");
            }

            sb.AppendLine();
            sb.Append("You can apply or manage leaves under **Leave** (`/online/leaves`). Need a correction? Click **Talk to a Real Person**.");
            return new HelpdeskBotResponse(sb.ToString(), 0.95m, false, null, null);
        }

        // B. Pending Requests / Leave Application Status
        if (ContainsAny(msg, "pending request", "my request", "request status", "status of my", "my application", "leave status", "my leaves", "open request", "track my"))
        {
            var sb = new StringBuilder();
            sb.AppendLine("**Your Recent & Open Requests**");
            sb.AppendLine();
            if (ctx.RecentRequests.Count == 0 && ctx.RecentLeaves.Count == 0)
            {
                sb.AppendLine("You do not have any recent requests or leave applications on record.");
            }
            else
            {
                if (ctx.RecentRequests.Count > 0)
                {
                    foreach (var r in ctx.RecentRequests.Take(6))
                        sb.AppendLine($"• `{r.ReferenceNumber}` · **{r.RequestType}** — *{r.State}* (Applied {r.AppliedDate:dd MMM yyyy})");
                }
                if (ctx.RecentLeaves.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("**Recent Leave Applications:**");
                    foreach (var l in ctx.RecentLeaves.Take(4))
                        sb.AppendLine($"• **{l.LeaveType}** ({l.FromDate:dd MMM} – {l.ToDate:dd MMM}, {l.Duration}d) — *{l.State}*");
                }
            }

            sb.AppendLine();
            sb.Append("If a request is stuck or needs urgent follow-up, click **Talk to a Real Person** above.");
            return new HelpdeskBotResponse(sb.ToString(), 0.94m, false, null, null);
        }

        // C. My Profile / Supervisor / Department / Organisation
        if (ContainsAny(msg, "who is my supervisor", "my supervisor", "my manager", "my department", "which organisation", "what organisation", "my organisation", "my profile", "my job", "my position", "employee number", "my details", "my team"))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**Your Current Employment Profile ({ctx.FullName})**");
            sb.AppendLine();
            sb.AppendLine($"• **Organisation**: {ctx.OrganisationName}");
            if (!string.IsNullOrWhiteSpace(ctx.DepartmentName)) sb.AppendLine($"• **Department**: {ctx.DepartmentName}");
            if (!string.IsNullOrWhiteSpace(ctx.PositionName)) sb.AppendLine($"• **Position**: {ctx.PositionName}");
            if (!string.IsNullOrWhiteSpace(ctx.JobTypeName)) sb.AppendLine($"• **Job Type**: {ctx.JobTypeName}");
            if (!string.IsNullOrWhiteSpace(ctx.EmployeeNumber)) sb.AppendLine($"• **Employee No.**: `{ctx.EmployeeNumber}`");
            if (ctx.JoinedDate.HasValue) sb.AppendLine($"• **Joined Date**: {ctx.JoinedDate.Value:dd MMM yyyy}{(string.IsNullOrWhiteSpace(ctx.ServiceDurationText) ? "" : $" ({ctx.ServiceDurationText})")}");
            sb.AppendLine($"• **Supervisor**: {(string.IsNullOrWhiteSpace(ctx.SupervisorName) ? "Not currently assigned" : ctx.SupervisorName)}");
            if (ctx.TeamNames.Count > 0) sb.AppendLine($"• **Teams**: {string.Join(", ", ctx.TeamNames)}");
            sb.AppendLine();
            sb.Append("If any of these details look outdated, click **Talk to a Real Person** so a Helpdesk Officer can assist.");
            return new HelpdeskBotResponse(sb.ToString(), 0.95m, false, null, null);
        }

        // D. My Portal Access & Roles
        if (ContainsAny(msg, "my role", "my roles", "my access", "my permission", "portal access", "can i access admin", "why can't i open admin", "cannot open admin", "can't open admin", "admin access"))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**Your Live Portal Access & Roles ({ctx.OrganisationName})**");
            sb.AppendLine();
            sb.AppendLine($"• **Employee Portal (`/online`)**: Enabled");
            sb.AppendLine($"• **Admin Portal (`/admin`)**: {(ctx.AdminPortalAccessEnabled || ctx.IsSuperAdministrator ? "Enabled — use the portal switcher at the bottom of the left sidebar." : "Not enabled on your account.")}");
            if (ctx.IsSuperAdministrator)
                sb.AppendLine("• **SuperAdmin Portal (`/superadmin`)**: Enabled");
            sb.AppendLine($"• **Assigned Role Groups**: {(ctx.RoleGroups.Count > 0 ? string.Join(", ", ctx.RoleGroups) : "Standard Employee (no special admin role groups assigned)")}");
            sb.AppendLine();
            if (!ctx.AdminPortalAccessEnabled && !ctx.IsSuperAdministrator)
                sb.Append("To request Admin Portal access or a new HR role, click **Talk to a Real Person** above so a Helpdesk Officer can help.");
            else
                sb.Append("If you are missing a specific permission inside Admin Portal, click **Talk to a Real Person** above and tell us which screen you need.");

            return new HelpdeskBotResponse(sb.ToString(), 0.94m, false, null, null);
        }

        return null;
    }

    private static bool ContainsAny(string text, params string[] phrases)
        => phrases.Any(p => text.Contains(p, StringComparison.Ordinal));

    private sealed record RankedArticle(HelpdeskArticle Article, double Score, decimal Confidence);

    private static List<RankedArticle> RankArticles(string query, string category, IReadOnlyList<HelpdeskArticle> articles)
    {
        var rawTokens = Tokenize(query);
        var expanded = new HashSet<string>(rawTokens, StringComparer.OrdinalIgnoreCase);
        foreach (var t in rawTokens)
        {
            var stemmed = Stem(t);
            expanded.Add(stemmed);
            if (HrSynonyms.TryGetValue(t, out var syns))
                foreach (var s in syns) expanded.Add(Stem(s));
            if (HrSynonyms.TryGetValue(stemmed, out var stemSyns))
                foreach (var s in stemSyns) expanded.Add(Stem(s));
        }

        var queryLower = query.Trim().ToLowerInvariant();
        var results = new List<RankedArticle>();

        foreach (var article in articles.Where(a => a.IsActive))
        {
            double score = 0;
            var keywords = article.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var titleTokens = Tokenize(article.Title).Select(Stem).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var answerTokens = Tokenize(article.Answer).Select(Stem).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var kw in keywords)
            {
                var kwLower = kw.ToLowerInvariant();
                if (kwLower.Length >= 3 && queryLower.Contains(kwLower, StringComparison.Ordinal))
                {
                    score += 6.0;
                }
                else
                {
                    foreach (var kt in Tokenize(kwLower).Select(Stem))
                    {
                        if (expanded.Contains(kt)) score += 2.75;
                    }
                }
            }

            foreach (var tt in titleTokens)
            {
                if (expanded.Contains(tt)) score += 2.25;
            }

            var answerMatches = 0;
            foreach (var at in answerTokens)
            {
                if (expanded.Contains(at))
                {
                    answerMatches++;
                    if (answerMatches <= 6) score += 0.65;
                }
            }

            if (!string.IsNullOrWhiteSpace(category)
                && !category.Equals("General", StringComparison.OrdinalIgnoreCase)
                && article.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                && score > 0)
            {
                score += 2.0;
            }

            if (score > 0)
            {
                var confidence = (decimal)Math.Min(0.96, Math.Round(score / 11.0, 2));
                results.Add(new RankedArticle(article, score, confidence));
            }
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.Article.SortOrder).ToList();
    }

    private static string Stem(string token)
    {
        var t = token.ToLowerInvariant();
        if (t.Length > 5 && t.EndsWith("ies", StringComparison.Ordinal)) return t[..^3] + "y";
        if (t.Length > 5 && t.EndsWith("ing", StringComparison.Ordinal)) return t[..^3];
        if (t.Length > 4 && t.EndsWith("es", StringComparison.Ordinal) && !t.EndsWith("les", StringComparison.Ordinal)) return t[..^2];
        if (t.Length > 3 && t.EndsWith("s", StringComparison.Ordinal) && !t.EndsWith("ss", StringComparison.Ordinal) && !t.EndsWith("us", StringComparison.Ordinal)) return t[..^1];
        return t;
    }

    private static HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]{2,}"))
        {
            if (!StopWords.Contains(m.Value))
                tokens.Add(m.Value);
        }
        return tokens;
    }

    private static string BuildLlmPrompt(HelpdeskBotRequest req, IReadOnlyList<HelpdeskArticle> topArticles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are the official GHR Platform Helpdesk Assistant supporting staff across government organisations.");
        sb.AppendLine("Answer ONLY using the Live User Context and Knowledge Base facts below. Never invent policies, balances, URLs, or permissions.");
        sb.AppendLine("If the user's problem requires changing data, granting roles, fixing bugs, payroll corrections, or isn't answered below, set shouldEscalate=true.");
        sb.AppendLine("Respond ONLY with valid JSON: {\"reply\":\"string\",\"confidence\":0.0,\"shouldEscalate\":false,\"escalationReason\":\"string or null\"}.");
        sb.AppendLine();

        if (req.UserContext != null)
        {
            var u = req.UserContext;
            sb.AppendLine("Live User Context (for this requester only):");
            sb.AppendLine($"- Name: {u.FullName} | Org: {u.OrganisationName} | Dept: {u.DepartmentName ?? "N/A"} | Position: {u.PositionName ?? "N/A"} | JobType: {u.JobTypeName ?? "N/A"} | EmpNo: {u.EmployeeNumber ?? "N/A"} | Supervisor: {u.SupervisorName ?? "None"}");
            sb.AppendLine($"- AdminPortalAccess: {u.AdminPortalAccessEnabled} | SuperAdmin: {u.IsSuperAdministrator} | Roles: {(u.RoleGroups.Count > 0 ? string.Join(", ", u.RoleGroups) : "None")}");
            if (u.LeaveBalances.Count > 0)
                sb.AppendLine($"- Leave Balances: {string.Join("; ", u.LeaveBalances.Select(l => $"{l.LeaveType}: {l.RemainingDays:0.##}d"))}");
            if (u.RecentRequests.Count > 0)
                sb.AppendLine($"- Recent Requests: {string.Join("; ", u.RecentRequests.Take(5).Select(r => $"{r.ReferenceNumber} {r.RequestType} ({r.State})"))}");
            if (u.RecentLeaves.Count > 0)
                sb.AppendLine($"- Recent Leaves: {string.Join("; ", u.RecentLeaves.Take(4).Select(l => $"{l.LeaveType} {l.FromDate:dd MMM}-{l.ToDate:dd MMM} ({l.State})"))}");
            sb.AppendLine();
        }

        sb.AppendLine($"Category: {req.Category} | Subject: {req.Subject}");
        sb.AppendLine("Knowledge Base:");
        foreach (var a in topArticles)
            sb.AppendLine($"- [{a.Category}] {a.Title}: {a.Answer}");

        if (req.RecentHistory.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Recent Conversation:");
            foreach (var h in req.RecentHistory.TakeLast(6))
                sb.AppendLine($"{h.SenderType}: {h.Body}");
        }
        sb.AppendLine();
        sb.AppendLine($"Latest User Message: {req.UserMessage}");
        return sb.ToString();
    }

    private async Task<HelpdeskBotResponse?> TryCallGeminiAsync(HelpdeskBotRequest req, IReadOnlyList<HelpdeskArticle> topArticles, CancellationToken ct)
    {
        try
        {
            var model = string.IsNullOrWhiteSpace(Model) ? "gemini-2.0-flash" : Model;
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(ApiKey!)}";
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = BuildLlmPrompt(req, topArticles) } } } },
                generationConfig = new { temperature = 0.2, responseMimeType = "application/json" }
            };
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(12);
            using var response = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
            return ParseStructuredBotJson(text, topArticles.FirstOrDefault()?.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Gemini helpdesk bot call failed; falling back to Knowledge Base engine.");
            return null;
        }
    }

    private async Task<HelpdeskBotResponse?> TryCallOpenAiCompatibleAsync(HelpdeskBotRequest req, IReadOnlyList<HelpdeskArticle> topArticles, CancellationToken ct)
    {
        try
        {
            var model = string.IsNullOrWhiteSpace(Model) ? "llama3.1" : Model;
            var payload = new
            {
                model,
                temperature = 0.2,
                messages = new[]
                {
                    new { role = "system", content = "Respond ONLY with strict JSON: {\"reply\":\"...\",\"confidence\":0.8,\"shouldEscalate\":false,\"escalationReason\":null}." },
                    new { role = "user", content = BuildLlmPrompt(req, topArticles) }
                }
            };
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(12);
            using var msg = new HttpRequestMessage(HttpMethod.Post, Endpoint!);
            if (!string.IsNullOrWhiteSpace(ApiKey))
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            msg.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(msg, ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return ParseStructuredBotJson(text, topArticles.FirstOrDefault()?.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenAI-compatible helpdesk bot call failed; falling back to Knowledge Base engine.");
            return null;
        }
    }

    private static HelpdeskBotResponse? ParseStructuredBotJson(string? raw, int? matchedArticleId)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var trimmed = raw.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        var json = trimmed[start..(end + 1)];

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var reply = root.TryGetProperty("reply", out var r) ? r.GetString() : null;
        if (string.IsNullOrWhiteSpace(reply)) return null;

        var confidence = root.TryGetProperty("confidence", out var c) && c.TryGetDecimal(out var dec)
            ? Math.Clamp(dec, 0m, 1m)
            : 0.7m;
        var shouldEscalate = root.TryGetProperty("shouldEscalate", out var e) && e.ValueKind == JsonValueKind.True;
        var reason = root.TryGetProperty("escalationReason", out var er) && er.ValueKind == JsonValueKind.String ? er.GetString() : null;
        if (confidence < 0.45m)
        {
            shouldEscalate = true;
            reason ??= "Low bot confidence; escalated to Helpdesk Officer.";
        }
        return new HelpdeskBotResponse(reply.Trim(), confidence, shouldEscalate, reason, matchedArticleId);
    }
}
