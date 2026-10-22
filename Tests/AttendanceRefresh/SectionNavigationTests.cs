using System.Reflection;
using HRM.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using SettingsPage = HRM.Components.Admin.Settings.SettingsCategoryPage;

static class SectionNavigationTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static Task RunAsync()
    {
        Check(SectionRoutes.Slug("Manage Organisations") == "organisations" && SectionRoutes.Slug("OrgDaily") == "org-daily" &&
            SectionRoutes.Slug("LeaveType") == "leave-type", "Section slugs are inconsistent");
        Check(SectionRoutes.Tab(["Attendance", "Break Late", "Out Of Office"], "break-late", "Attendance") == "Break Late", "Hyphenated tab URL not restored");
        Check(SectionRoutes.Tab(["Attendance", "Break Late"], "unknown", "Attendance") == "Attendance", "Unknown tab fallback failed");
        var page = new SettingsPage { CategoryName = "org", Section = "teams" };
        Check(Selected(page) == "Teams and supervisors", "Organisation team URL did not restore selection");
        page.Section = "permissions";
        Check(Selected(page) == "Organisation Permissions", "Back/forward within a category retained the previous section");
        foreach (var (section, title) in new[] { ("manage", "Manage"), ("types", "Manage type"), ("calendar", "Calendar"),
            ("locations", "Locations"), ("location-types", "Location Types"), ("organisation-types", "Organisation Types"), ("requests", "Requests"), ("request-types", "Request Types") })
        {
            page.Section = section; Check(Selected(page) == title, $"Organisation section {section} did not restore");
        }
        page.CategoryName = "positions"; page.Section = "overview";
        Check(Selected(page) == "Overview", "Category navigation retained the previous category");
        page.CategoryName = "requests"; page.Section = "state-transitions";
        Check(Selected(page) == null, "A protected settings section loaded without Developer membership");
        page.CategoryName = "org"; page.Section = "unknown";
        Check(Selected(page) == null, "Invalid settings section selected unrelated content");
        Check(Route("/admin/settings/org/teams") == typeof(SettingsPage), "Organisation teams URL resolves to the wrong page");
        Check(Route("/admin/settings/org/teams/departments") == typeof(SettingsPage), "Department selection URL is missing");
        Check(Route("/admin/settings/org/permissions") == typeof(SettingsPage), "Organisation permissions URL resolves to the wrong page");
        Check(Route("/admin/settings/org/organisations/3/structure") == typeof(SettingsPage), "Organisation record section URL is missing");
        Check(Route("/admin/settings/workplaning/templates") == typeof(SettingsPage), "Work planning section URL is missing");
        Check(Route("/admin/hr/individuals/search") == typeof(HRM.Components.Pages.HrCategoryPage), "Individual search was captured by the organisation search route");
        Check(Route("/admin/hr/staff/staff-onboarding") == typeof(HRM.Components.Pages.HrCategoryPage), "HR section URL is missing");
        Check(Route("/admin/payroll/payroll/payroll-period") == typeof(HRM.Components.Pages.PayrollCategoryPage), "Payroll section URL is missing");
        Check(Route("/admin/budget/reports/groups") == typeof(HRM.Components.Pages.BudgetCategoryPage), "Budget section URL is missing");
        Check(Route("/admin/my/workspace/staff/918/attendance") == typeof(HRM.Components.Admin.My.Workspace.Workspace), "Workspace entity tab URL is missing");
        Check(Route("/admin/my/workspace/org-daily") == typeof(HRM.Components.Admin.My.Workspace.Workspace), "Workspace tab URL is missing");
        Check(Route("/online/attendance/my-attendance/break-late") == typeof(HRM.Components.Pages.Online.MyAttendanceGrid), "Employee attendance tab URL is missing");
        Check(Route("/admin/attendance/manage-attendance/team-attendance/summary") == typeof(HRM.Components.Admin.Attendance.Attendance.TeamAttendanceGrid), "Team attendance tab URL is missing");
        Check(Route("/admin/settings/org/team") == typeof(HRM.Components.Admin.Settings.Organisations.OrganisationTeamsPage), "Legacy team link was removed");
        Check(Route("/admin/attendance/manage-attendance/team-attendace") == typeof(HRM.Components.Admin.Attendance.Attendance.TeamAttendanceGrid), "Legacy attendance link was removed");
        return Task.CompletedTask;
    }
    private static string? Selected(SettingsPage page)
    {
        typeof(SettingsPage).GetMethod("OnParametersSet", Flags)!.Invoke(page, null);
        var item = typeof(SettingsPage).GetField("activeItem", Flags)!.GetValue(page);
        return item?.GetType().GetProperty("Title")!.GetValue(item)?.ToString();
    }
    private static Type? Route(string path)
    {
        var matches = new List<(Type Type, string Rank, string Template)>();
        foreach (var type in typeof(SettingsPage).Assembly.GetTypes())
        foreach (var route in type.GetCustomAttributes<RouteAttribute>())
        {
            var parsed = TemplateParser.Parse(route.Template.TrimStart('/'));
            var matcher = new TemplateMatcher(parsed, new RouteValueDictionary());
            if (!matcher.TryMatch(path, new RouteValueDictionary())) continue;
            var rank = string.Concat(parsed.Segments.Select(s => s.Parts.All(p => p.IsLiteral) ? "3" : s.Parts.Any(p => p.InlineConstraints.Any()) ? "2" : "1"));
            matches.Add((type, rank, route.Template));
        }
        var best = matches.OrderByDescending(m => m.Rank, StringComparer.Ordinal).FirstOrDefault();
        Check(matches.Count(m => m.Rank == best.Rank) <= 1, $"Ambiguous section URL {path}");
        return best.Type;
    }
}
