using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace HRM.Helpers;

public static class SectionRoutes
{
    public static string Slug(string title)
    {
        var name = title.Trim();
        if (name.StartsWith("Manage ", StringComparison.OrdinalIgnoreCase)) name = name[7..];
        name = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1-$2");
        return Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    }
    public static bool Matches(string? section, string slug) => string.Equals(section, slug, StringComparison.OrdinalIgnoreCase);
    public static string Tab(IEnumerable<string> tabs, string? section, string fallback) =>
        tabs.FirstOrDefault(t => Matches(section, Slug(t)) || Matches(section, t)) ?? fallback;
    public static void NavigateTab(Microsoft.AspNetCore.Components.NavigationManager navigation, string path, string tab)
    {
        var current = new Uri(navigation.GetUriWithQueryParameter("tab", (string?)null));
        navigation.NavigateTo($"{path}/{Slug(tab)}{current.Query}{current.Fragment}");
    }
}
