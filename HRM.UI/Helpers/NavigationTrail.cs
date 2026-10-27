namespace HRM.Helpers;

/// <summary>Tracks local navigation for a conditional in-app Back link.</summary>
public sealed class NavigationTrail
{
    private readonly List<string> locations = [];
    private string current;
    public NavigationTrail(string initialLocation) => current = initialLocation;
    public string? Previous => locations.Count == 0 ? null : locations[^1];

    public void Visit(string location)
    {
        if (location == current) return;
        var previousUri = new Uri(current);
        var nextUri = new Uri(location);
        if (previousUri.GetLeftPart(UriPartial.Authority) != nextUri.GetLeftPart(UriPartial.Authority)
            || previousUri.AbsolutePath.Contains("/auth/", StringComparison.OrdinalIgnoreCase)
            || nextUri.AbsolutePath.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
            locations.Clear();
        else
        {
            var backIndex = locations.LastIndexOf(location);
            if (backIndex >= 0) locations.RemoveRange(backIndex, locations.Count - backIndex);
            else
            {
                locations.Add(current);
                if (locations.Count > 100) locations.RemoveAt(0);
            }
        }
        current = location;
    }
}
