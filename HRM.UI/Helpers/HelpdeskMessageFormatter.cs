using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace HRM.UI.Helpers;

/// <summary>
/// Safely formats Helpdesk bot/agent replies (bold, italic, inline code, clickable internal routes,
/// bullet lists, and numbered lists) after HTML-encoding all raw input.
/// </summary>
public static partial class HelpdeskMessageFormatter
{
    public sealed record ParsedBotMessage(string MainBody, List<string> RelatedGuides);

    /// <summary>
    /// Splits out any "Related guides you can also ask about:" bullet block so the UI can render
    /// those topics as 1-click interactive chat pills instead of plain text bullets.
    /// </summary>
    public static ParsedBotMessage SplitRelatedGuides(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ParsedBotMessage(string.Empty, []);

        var lines = text.Replace("\r\n", "\n").Split('\n');
        var main = new List<string>(lines.Length);
        var guides = new List<string>();
        var inRelatedBlock = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Equals("Related guides you can also ask about:", StringComparison.OrdinalIgnoreCase))
            {
                inRelatedBlock = true;
                continue;
            }

            if (inRelatedBlock)
            {
                if (trimmed.Length == 0)
                {
                    inRelatedBlock = false;
                    main.Add(line);
                    continue;
                }

                var bullet = BulletLineRegex().Match(trimmed);
                if (bullet.Success)
                {
                    var topic = bullet.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(topic))
                        guides.Add(topic);
                    continue;
                }

                inRelatedBlock = false;
            }

            main.Add(line);
        }

        return new ParsedBotMessage(string.Join("\n", main).Trim(), guides);
    }

    public static MarkupString Format(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new MarkupString(string.Empty);

        var lines = text.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        var listMode = 0; // 0 = none, 1 = ul, 2 = ol
        var paragraphLines = new List<string>();

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0) return;
            sb.Append("<p class=\"my-1 leading-relaxed\">");
            for (var i = 0; i < paragraphLines.Count; i++)
            {
                if (i > 0) sb.Append("<br />");
                sb.Append(FormatInline(paragraphLines[i]));
            }
            sb.Append("</p>");
            paragraphLines.Clear();
        }

        void CloseList()
        {
            if (listMode == 1) sb.Append("</ul>");
            else if (listMode == 2) sb.Append("</ol>");
            listMode = 0;
        }

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (trimmed.Length == 0)
            {
                FlushParagraph();
                CloseList();
                continue;
            }

            var numberedMatch = NumberedLineRegex().Match(trimmed);
            if (numberedMatch.Success)
            {
                FlushParagraph();
                if (listMode != 2)
                {
                    CloseList();
                    sb.Append("<ol class=\"list-decimal pl-5 my-1.5 space-y-1\">");
                    listMode = 2;
                }
                sb.Append("<li class=\"leading-relaxed\">").Append(FormatInline(numberedMatch.Groups[1].Value)).Append("</li>");
                continue;
            }

            var bulletMatch = BulletLineRegex().Match(trimmed);
            if (bulletMatch.Success)
            {
                FlushParagraph();
                if (listMode != 1)
                {
                    CloseList();
                    sb.Append("<ul class=\"list-disc pl-5 my-1.5 space-y-1\">");
                    listMode = 1;
                }
                sb.Append("<li class=\"leading-relaxed\">").Append(FormatInline(bulletMatch.Groups[1].Value)).Append("</li>");
                continue;
            }

            CloseList();
            paragraphLines.Add(trimmed);
        }

        FlushParagraph();
        CloseList();
        return new MarkupString(sb.ToString());
    }

    private static string FormatInline(string raw)
    {
        var encoded = WebUtility.HtmlEncode(raw);

        // Inline code spans (turn safe internal app routes starting with / into clickable links)
        encoded = InlineCodeRegex().Replace(encoded, match =>
        {
            var inner = match.Groups[1].Value;
            if (IsSafeInternalRoute(inner))
            {
                return $"<a href=\"{inner}\" class=\"inline-flex items-center font-mono text-[11px] font-semibold underline text-teal-700 dark:text-teal-300 bg-teal-50 dark:bg-teal-950/60 border border-teal-200/80 dark:border-teal-800 px-1.5 py-0.2 rounded hover:opacity-85\">{inner}</a>";
            }
            return $"<code class=\"font-mono text-[11px] bg-slate-100 dark:bg-slate-800 text-slate-800 dark:text-slate-200 px-1.5 py-0.2 rounded\">{inner}</code>";
        });

        // Bold **text**
        encoded = BoldRegex().Replace(encoded, "<strong class=\"font-semibold text-slate-900 dark:text-white\">$1</strong>");

        // Italic *text*
        encoded = ItalicRegex().Replace(encoded, "<em class=\"italic\">$1</em>");

        return encoded;
    }

    private static bool IsSafeInternalRoute(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2) return false;
        if (!value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal)) return false;
        return InternalRouteRegex().IsMatch(value);
    }

    [GeneratedRegex(@"^\d+\.\s+(.+)$")]
    private static partial Regex NumberedLineRegex();

    [GeneratedRegex(@"^(?:•|-|\*)\s+(.+)$")]
    private static partial Regex BulletLineRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"\*\*([^\*]+)\*\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\*)\*([^\*]+)\*(?!\*)")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"^/[a-zA-Z0-9/_\-?=&.]+$")]
    private static partial Regex InternalRouteRegex();
}
