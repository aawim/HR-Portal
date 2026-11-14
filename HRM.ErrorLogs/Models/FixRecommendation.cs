namespace HRM.ErrorLogs.Models;

/// <summary>
/// Prescriptive troubleshooting guidance explaining what happened and what must be done to resolve the error.
/// </summary>
public sealed class FixRecommendation
{
    /// <summary>
    /// Plain-English explanation of why this error occurred.
    /// </summary>
    public string RootCauseSummary { get; set; } = string.Empty;

    /// <summary>
    /// Primary actionable fix recommendation.
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// Ordered troubleshooting checklist for the developer.
    /// </summary>
    public List<string> DiagnosticSteps { get; set; } = new();

    /// <summary>
    /// Suggested code pattern or snippet demonstrating the correct pattern.
    /// </summary>
    public string? SuggestedCodePattern { get; set; }

    /// <summary>
    /// Documentation or architectural guideline reference.
    /// </summary>
    public string? DocumentationUrl { get; set; }
}
