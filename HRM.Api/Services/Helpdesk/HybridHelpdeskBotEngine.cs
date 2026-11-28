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
        "please", "help", "need", "want", "my", "our", "your", "you", "about", "into", "onto", "not"
    };

    private static readonly Dictionary<string, string[]> HrSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ot"] = ["overtime"],
        ["overtime"] = ["ot", "extra", "hours"],
        ["leave"] = ["vacation", "annual", "sick", "holiday", "absence"],
        ["vacation"] = ["leave", "annual"],
        ["sick"] = ["leave", "medical"],
        ["punch"] = ["attendance", "biometric", "checkin", "checkout"],
        ["checkin"] = ["attendance", "late", "punch"],
        ["attendance"] = ["punch", "late", "ooo", "biometric"],
        ["ooo"] = ["out", "office", "attendance"],
        ["payslip"] = ["salary", "payroll", "pay"],
        ["salary"] = ["payslip", "payroll", "allowance", "deduction"],
        ["payroll"] = ["payslip", "salary"],
        ["login"] = ["access", "password", "account", "portal"],
        ["permission"] = ["access", "role", "unauthorized", "forbidden"],
        ["unauthorized"] = ["permission", "access", "role"],
        ["wrong"] = ["organisation", "org", "access"],
        ["letter"] = ["certificate", "document"],
        ["certificate"] = ["letter", "document"],
        ["bug"] = ["error", "crash", "broken", "issue"],
        ["error"] = ["bug", "failed", "crash", "unavailable"]
    };

    private string Provider => (configuration["HelpdeskBot:Provider"] ?? "KnowledgeBase").Trim();
    private string? ApiKey => configuration["HelpdeskBot:ApiKey"];
    private string? Endpoint => configuration["HelpdeskBot:Endpoint"];
    private string Model => (configuration["HelpdeskBot:Model"] ?? "").Trim();

    public string EngineMode => Provider.ToLowerInvariant() switch
    {
        "gemini" when !string.IsNullOrWhiteSpace(ApiKey) => $"Hybrid KB + Gemini ({(string.IsNullOrWhiteSpace(Model) ? "gemini-2.0-flash" : Model)})",
        "openaicompatible" when !string.IsNullOrWhiteSpace(Endpoint) => $"Hybrid KB + LLM ({(string.IsNullOrWhiteSpace(Model) ? "configured" : Model)})",
        _ => "Knowledge Base + Guided Escalation (AI-Ready)"
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

        // 3. Optional Grounded LLM call (Gemini or OpenAI-Compatible / Local Ollama)
        if (Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ApiKey))
        {
            var aiReply = await TryCallGeminiAsync(request, ranked.Take(3).Select(x => x.Article).ToList(), ct);
            if (aiReply != null) return aiReply;
        }
        else if (Provider.Equals("OpenAICompatible", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(Endpoint))
        {
            var aiReply = await TryCallOpenAiCompatibleAsync(request, ranked.Take(3).Select(x => x.Article).ToList(), ct);
            if (aiReply != null) return aiReply;
        }

        // 4. Deterministic Knowledge Base response with automatic escalation safeguard
        if (request.PriorBotTurns >= 2 && (best == null || best.Confidence < 0.72m))
        {
            return new HelpdeskBotResponse(
                "It looks like this needs personal assistance from our support team. I have escalated your ticket to a real Helpdesk Officer who will review our conversation and take over shortly.",
                best?.Confidence ?? 0.25m,
                true,
                "Escalated automatically after multiple unresolved bot turns.",
                best?.Article.Id);
        }

        if (best != null && best.Confidence >= 0.42m)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"**{best.Article.Title}**");
            sb.AppendLine();
            sb.AppendLine(best.Article.Answer.Trim());
            sb.AppendLine();
            sb.Append("Did this resolve your question? You can click **That solved it** below, reply with more details, or click **Talk to a Real Person** anytime.");
            return new HelpdeskBotResponse(sb.ToString(), best.Confidence, false, null, best.Article.Id);
        }

        // Low confidence on first/second turn: either escalate immediately if urgent/technical or ask + escalate
        var isUrgentTopic = request.Category.Contains("Technical", StringComparison.OrdinalIgnoreCase)
            || request.Category.Contains("Access", StringComparison.OrdinalIgnoreCase)
            || messageLower.Contains("urgent", StringComparison.Ordinal)
            || messageLower.Contains("locked", StringComparison.Ordinal);

        if (isUrgentTopic || request.PriorBotTurns >= 1)
        {
            return new HelpdeskBotResponse(
                "I could not find an exact self-service answer for your issue, so I have transferred this chat to a real Helpdesk Officer. They can see your organisation and conversation history and will respond here shortly.",
                best?.Confidence ?? 0.15m,
                true,
                "Bot confidence below threshold; automatically handed over to a Helpdesk Officer.",
                null);
        }

        var suggestionList = request.ActiveArticles
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder)
            .Take(4)
            .Select(a => $"• {a.Title}");

        var fallback = new StringBuilder();
        fallback.AppendLine("I want to make sure I give you the right answer. Could you share a bit more detail about what you need help with?");
        if (suggestionList.Any())
        {
            fallback.AppendLine();
            fallback.AppendLine("Common topics I can help with immediately:");
            foreach (var item in suggestionList) fallback.AppendLine(item);
        }
        fallback.AppendLine();
        fallback.Append("Or click **Talk to a Real Person** below and I will connect you directly to a Helpdesk Officer.");

        return new HelpdeskBotResponse(fallback.ToString(), 0.25m, false, null, null);
    }

    private sealed record RankedArticle(HelpdeskArticle Article, double Score, decimal Confidence);

    private static List<RankedArticle> RankArticles(string query, string category, IReadOnlyList<HelpdeskArticle> articles)
    {
        var rawTokens = Tokenize(query);
        var expanded = new HashSet<string>(rawTokens, StringComparer.OrdinalIgnoreCase);
        foreach (var t in rawTokens)
        {
            if (HrSynonyms.TryGetValue(t, out var syns))
            {
                foreach (var s in syns) expanded.Add(s);
            }
        }

        var queryLower = query.Trim().ToLowerInvariant();
        var results = new List<RankedArticle>();

        foreach (var article in articles.Where(a => a.IsActive))
        {
            double score = 0;
            var keywords = article.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var titleTokens = Tokenize(article.Title);
            var answerTokens = Tokenize(article.Answer);

            foreach (var kw in keywords)
            {
                var kwLower = kw.ToLowerInvariant();
                if (kwLower.Length >= 3 && queryLower.Contains(kwLower, StringComparison.Ordinal))
                {
                    score += 6.0;
                }
                else
                {
                    var kwTokens = Tokenize(kwLower);
                    foreach (var kt in kwTokens)
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
                    if (answerMatches <= 6) score += 0.6;
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
                var confidence = (decimal)Math.Min(0.96, Math.Round(score / 12.0, 2));
                results.Add(new RankedArticle(article, score, confidence));
            }
        }

        return results.OrderByDescending(r => r.Score).ThenBy(r => r.Article.SortOrder).ToList();
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
        sb.AppendLine("Answer ONLY using the Knowledge Base facts below. Never invent policies, URLs, or permissions.");
        sb.AppendLine("If the user's problem requires account changes, bug investigation, payroll correction, or isn't answered by the KB, set shouldEscalate=true.");
        sb.AppendLine("Respond ONLY with valid JSON: {\"reply\":\"string\",\"confidence\":0.0,\"shouldEscalate\":false,\"escalationReason\":\"string or null\"}.");
        sb.AppendLine();
        sb.AppendLine($"Organisation: {req.OrganisationName} | Category: {req.Category} | Subject: {req.Subject}");
        sb.AppendLine("Knowledge Base:");
        foreach (var a in topArticles)
        {
            sb.AppendLine($"- [{a.Category}] {a.Title}: {a.Answer}");
        }
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
