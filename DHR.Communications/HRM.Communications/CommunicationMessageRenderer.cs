using System.Text.RegularExpressions;
namespace HRM.Communications;

/// <summary>Small, deterministic renderer for approved template variables.</summary>
public static class CommunicationMessageRenderer
{
    private static readonly Regex Variables = new(@"\{\{([A-Za-z][A-Za-z0-9]*)\}\}", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly HashSet<string> AllowedVariables = new(StringComparer.Ordinal)
    { "OrganisationEventId", "Title", "Description", "Category", "StartDateTime", "EndDateTime", "TimeZoneId", "EmployeeName", "EffectiveDate", "JobId", "IndividualId" };

    public static void Validate(string template)
    {
        if (string.IsNullOrWhiteSpace(template) || template.Length > 4000)
            throw new InvalidOperationException("Enter a message template of up to 4,000 characters.");
        foreach (Match match in Variables.Matches(template))
            if (!AllowedVariables.Contains(match.Groups[1].Value))
                throw new InvalidOperationException($"Unsupported template variable: {match.Groups[1].Value}.");
        var remaining = Variables.Replace(template, string.Empty);
        if (remaining.Contains("{{", StringComparison.Ordinal) || remaining.Contains("}}", StringComparison.Ordinal))
            throw new InvalidOperationException("Use template variables in the form {{Title}}.");
    }

    public static string Render(string template, IReadOnlyDictionary<string, string?> values)
    {
        Validate(template);
        // Replace once: braces inside event content are literal text, never further instructions.
        var rendered = Variables.Replace(template, match => values.TryGetValue(match.Groups[1].Value, out var value)
            ? value ?? string.Empty : throw new InvalidOperationException($"Missing template value: {match.Groups[1].Value}."));
        if (string.IsNullOrWhiteSpace(rendered) || rendered.Length > 4000)
            throw new InvalidOperationException("The rendered message must contain between 1 and 4,000 characters.");
        return rendered;
    }
}
