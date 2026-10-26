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
        var trail = new NavigationTrail("https://localhost/admin/my/workspace");
        Check(trail.Previous == null, "Direct visit must not display Back");
        trail.Visit("https://localhost/admin/settings/leaves");
        Check(trail.Previous == "https://localhost/admin/my/workspace", "Back lost the previous location");
        trail.Visit("https://localhost/admin/settings/leaves/manage-leave-sets");
        Check(trail.Previous == "https://localhost/admin/settings/leaves", "Settings section was not tracked");
        trail.Visit("https://localhost/admin/settings/leaves");
        Check(trail.Previous == "https://localhost/admin/my/workspace", "Back navigation created a loop");
        trail.Visit("https://localhost/Online/Auth/Logout");
        Check(trail.Previous == null, "Authentication navigation retained old account history");
        trail.Visit("https://localhost/admin/settings");
        Check(trail.Previous == null, "Back must not replay authentication endpoints");
        trail.Visit("https://other-host/admin/settings");
        Check(trail.Previous == null, "Back retained a different origin");

        Check(SectionRoutes.Slug("Manage Organisations") == "organisations" && SectionRoutes.Slug("OrgDaily") == "org-daily" &&
            SectionRoutes.Slug("LeaveType") == "leave-type", "Section slugs are inconsistent");
        Check(SectionRoutes.Tab(["Attendance", "Break Late", "Out Of Office"], "break-late", "Attendance") == "Break Late", "Hyphenated tab URL not restored");
        Check(SectionRoutes.Tab(["Attendance", "Break Late"], "unknown", "Attendance") == "Attendance", "Unknown tab fallback failed");
        var initial = new SettingsPage { CategoryName = null!, OrganisationId = 3, OrganisationSection = "structure" };
        var initialItems = (System.Collections.IEnumerable)typeof(SettingsPage).GetMethod("GetSubItems", Flags)!.Invoke(initial, [null])!;
        Check(initialItems.Cast<object>().Any(x => (string?)x.GetType().GetProperty("Title")!.GetValue(x) == "Manage"),
            "Organisation route crashed or lost its category during the initial async render");
        Check(Selected(initial) == "Manage", "Organisation route without CategoryName did not resolve Manage");
        initial = new SettingsPage { CategoryName = null!, TeamSection = "departments" };
        Check(Selected(initial) == null, "Legacy team route retained a duplicate settings editor");
        initial = new SettingsPage { CategoryName = null! };
        _ = typeof(SettingsPage).GetMethod("GetSubItems", Flags)!.Invoke(initial, [null]);
        _ = Selected(initial);
        var page = new SettingsPage { CategoryName = "org", Section = "teams" };
        Check(Selected(page) == null, "Organisation team URL retained a duplicate settings editor");
        page.Section = "permissions";
        Check(Selected(page) == "Organisation Permissions", "Back/forward within a category retained the previous section");
        foreach (var (section, title) in new[] { ("manage", "Manage"), ("document-types", "Document Types"), ("types", "Manage type"), ("calendar", "Calendar"),
            ("locations", "Locations"), ("location-types", "Location Types"), ("organisation-types", "Organisation Types"), ("requests", "Structure Request Types"), ("structure-request-types", "Structure Request Types") })
        {
            page.Section = section; Check(Selected(page) == title, $"Organisation section {section} did not restore");
        }
        page.CategoryName = "org"; page.Section = "request-types";
        Check(Selected(page) == null, "General request types are still available in Organisation settings");
        page.CategoryName = "requests";
        Check(Selected(page) == "Request Types", "General request types were removed from Request settings");
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
        Check(Route("/admin/settings/leaves/manage-leave-sets")==typeof(SettingsPage),"Canonical leave set settings route is missing");
        var cataloguePage = new SettingsPage { CategoryName = "leaves", Section = "leave-types" };
        Check(Selected(cataloguePage) == "Manage Leave Types", "Canonical leave catalogue route is missing");
        Check(Route("/admin/hr/leave/types") == typeof(HRM.Components.Leave.LeaveTypesRedirect), "Former type editor must redirect to Settings");
        var setsPage=new SettingsPage{CategoryName="leaves",Section="manage-leave-sets"};
        Check(Selected(setsPage)=="Leave Set Management","Settings did not select canonical leave set management");
        var legacySets=new HRM.Components.Pages.HrCategoryPage{CategoryName="leave-sets",Section="management"};var setNavigation=new SupervisorTestNavigation();
        legacySets.GetType().GetProperty("Navigation",Flags)!.SetValue(legacySets,setNavigation);
        legacySets.GetType().GetMethod("OnParametersSet",Flags)!.Invoke(legacySets,null);
        Check(setNavigation.Uri.EndsWith("/admin/settings/leaves/manage-leave-sets"),"Legacy HR link did not redirect to canonical Settings management");
        Check(Route("/admin/hr/departments/management")==typeof(HRM.Components.Pages.HrCategoryPage),"HR departments route is missing");
        Check(Route("/admin/hr/work-handovers")==typeof(HRM.Components.Pages.Online.Leave.WorkHandovers),"HR handover route is missing or ambiguous");
        Check(Route("/admin/hr/work-handovers/42")==typeof(HRM.Components.Pages.Online.Leave.WorkHandovers),"HR handover detail route is missing or ambiguous");
        Check(Route("/online/leave-handovers")==typeof(HRM.Components.Pages.Online.Leave.WorkHandovers),"Employee handovers were lost during the admin navigation move");
        Check(Route("/admin/hr/requests/received")==typeof(HRM.Components.Pages.HrCategoryPage),"HR received requests route is missing");
        Check(Route("/admin/hr/supervisors/management") == typeof(HRM.Components.Pages.HrCategoryPage), "HR supervisor management route is missing");
        var oldSupervisors = new SettingsPage { CategoryName="org", TeamSection="departments" };
        var navigation = new SupervisorTestNavigation();
        typeof(SettingsPage).GetProperty("Navigation", Flags)!.SetValue(oldSupervisors,navigation);
        Selected(oldSupervisors);
        Check(navigation.Uri.Contains("/admin/hr/supervisors/management") && navigation.Uri.Contains("organisationId=10") && navigation.Uri.Contains("teamView=departments"), "Legacy supervisor redirect lost organisation or department selection");
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
        if (typeof(SettingsPage).GetProperty("Navigation", Flags)!.GetValue(page) == null)
            typeof(SettingsPage).GetProperty("Navigation", Flags)!.SetValue(page,new SupervisorTestNavigation());
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

sealed class SupervisorTestNavigation : Microsoft.AspNetCore.Components.NavigationManager
{
    public SupervisorTestNavigation() { Initialize("https://localhost/", "https://localhost/admin/settings/org/teams?organisationId=10"); }
    protected override void NavigateToCore(string uri, bool forceLoad) => Uri=ToAbsoluteUri(uri).AbsoluteUri;
    protected override void NavigateToCore(string uri, Microsoft.AspNetCore.Components.NavigationOptions options) => Uri=ToAbsoluteUri(uri).AbsoluteUri;
}
