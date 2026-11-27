using HRM.DTOs.Roles;

namespace HRM.Helpers;

public enum AccessActionBucket
{
    View,
    CreateEdit,
    Approve,
    Configure
}

public sealed record AccessModuleDefinition(string Key, string Label, string Icon, string Description);

public sealed record RoleTemplatePreset(
    string Key,
    string Name,
    string Summary,
    string[] ModuleKeys,
    AccessActionBucket[] AllowedActions);

public static class AccessControlUiHelper
{
    public static readonly IReadOnlyList<AccessModuleDefinition> Modules =
    [
        new("staff", "Core HR & Staff", "bi-people", "Employee profiles, jobs, positions, transfers, and separations."),
        new("organisation", "Organisation & Structure", "bi-building", "Departments, teams, operational groups, locations, and calendars."),
        new("attendance", "Attendance & Shifts", "bi-clock", "Clock events, work plans, schedules, duty rosters, and overtime."),
        new("leave", "Leave & Handovers", "bi-calendar2-check", "Leave applications, entitlements, policies, and work handovers."),
        new("payroll", "Payroll & Finance", "bi-cash-stack", "Pay frameworks, payslips, allowances, deductions, loans, and budgets."),
        new("recruitment", "Recruitment & Onboarding", "bi-person-plus", "Vacancies, applicants, evaluations, and appointment onboarding."),
        new("documents", "Documents & Certificates", "bi-folder2-open", "Official HR documents, contracts, certificates, and signatures."),
        new("items", "Items & Assets", "bi-box-seam", "Item requisitions, stock catalogue, and staff asset tracking."),
        new("communications", "Communications", "bi-broadcast", "Announcements, provider integrations, and staff messaging."),
        new("security", "Access & Security", "bi-shield-lock", "Roles, permissions, portal access, and staff visibility scopes."),
        new("platform", "Audit, Logs & Developer", "bi-terminal", "Audit trails, activity logs, workflows, transitions, and diagnostics.")
    ];

    public static readonly IReadOnlyList<RoleTemplatePreset> RoleTemplates =
    [
        new("hr-manager", "HR Manager", "Full HR operations & approvals across Staff, Attendance, Leave, Recruitment, and Documents.",
            ["staff", "organisation", "attendance", "leave", "recruitment", "documents", "items", "communications"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit, AccessActionBucket.Approve, AccessActionBucket.Configure]),
        new("hr-officer", "HR Officer", "Day-to-day HR administration without system configuration or platform security.",
            ["staff", "organisation", "attendance", "leave", "recruitment", "documents", "items"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit]),
        new("payroll-officer", "Payroll Officer", "Payroll processing, allowances, deductions, loans, and attendance verification.",
            ["payroll", "attendance", "leave", "staff"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit, AccessActionBucket.Approve]),
        new("supervisor", "Department Supervisor", "View team records and approve leave, attendance, overtime, and handovers.",
            ["staff", "attendance", "leave", "items"],
            [AccessActionBucket.View, AccessActionBucket.Approve]),
        new("attendance-officer", "Attendance Officer", "Manage schedules, duty rosters, clock records, and attendance reviews.",
            ["attendance", "organisation", "staff"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit])
    ];

    public static AccessModuleDefinition ResolveModule(string? key, string? name = null, string? group = null)
    {
        var text = $"{key} {group} {name}";

        if (ContainsAny(text, "Payroll", "Payslip", "Allowance", "Deduction", "GoodsLoan", "Loan", "Budget", "Salary", "PayFramework", "Finance"))
            return Modules.First(m => m.Key == "payroll");
        if (ContainsAny(text, "Leave", "Handover", "Holiday"))
            return Modules.First(m => m.Key == "leave");
        if (ContainsAny(text, "Attendance", "Workplan", "WorkPlan", "WorkType", "Schedule", "Duty", "Roster", "Overtime", "OutOfOffice", "LateCheckIn", "Clock", "Shift", "WeeklyHour", "BreakTime", "OfficialTrip"))
            return Modules.First(m => m.Key == "attendance");
        if (ContainsAny(text, "Recruitment", "Vacancy", "Applicant", "Onboarding", "Requisition", "Interview"))
            return Modules.First(m => m.Key == "recruitment");
        if (ContainsAny(text, "Document", "Certificate", "Signature", "Contract"))
            return Modules.First(m => m.Key == "documents");
        if (ContainsAny(text, "Item", "Asset", "Inventory"))
            return Modules.First(m => m.Key == "items");
        if (ContainsAny(text, "Communication", "Chat", "Notification", "Announcement"))
            return Modules.First(m => m.Key == "communications");
        if (ContainsAny(text, "Audit", "Activity", "ErrorLog", "Developer", "Workflow", "Transition", "Edge", "Diagnostic", "Sequence", "RequestType"))
            return Modules.First(m => m.Key == "platform");
        if (ContainsAny(text, "Role", "Permission", "UserGroup", "Access", "Admin", "Super.Admin", "User.", "Users."))
            return Modules.First(m => m.Key == "security");
        if (ContainsAny(text, "Organisation", "Department", "Team", "Group", "Location", "Calendar", "Event", "Leadership", "Structure"))
            return Modules.First(m => m.Key == "organisation");

        return Modules.First(m => m.Key == "staff");
    }

    public static AccessModuleDefinition ResolveModule(PermissionDefinitionDto permission) =>
        ResolveModule(permission.Key, permission.Name, permission.Group);

    public static AccessActionBucket ResolveAction(string? key, string? name = null)
    {
        var text = $"{key} {name}";
        if (ContainsAny(text, "Approve", "Approval", "Review", "Reject", "Sign", "Verify", "Act"))
            return AccessActionBucket.Approve;
        if (ContainsAny(text, "Admin", "Manage", "Configure", "Config", "Setting", "Policy", "Delete", "Remove", "Revoke", "Publish", "Process", "Post", "Export", "Import", "Bulk"))
            return AccessActionBucket.Configure;
        if (ContainsAny(text, "Add", "Create", "Edit", "Update", "Save", "Assign", "Apply", "Submit", "Request", "Upload", "Issue", "Transfer"))
            return AccessActionBucket.CreateEdit;
        return AccessActionBucket.View;
    }

    public static AccessActionBucket ResolveAction(PermissionDefinitionDto permission) =>
        ResolveAction(permission.Key, permission.Name);

    public static List<int> MatchTemplatePermissions(RoleTemplatePreset template, IEnumerable<PermissionDefinitionDto> availablePermissions)
    {
        var moduleSet = template.ModuleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actionSet = template.AllowedActions.ToHashSet();
        return availablePermissions
            .Where(p =>
            {
                if (p.Key.Equals("Super.Admin.Manage", StringComparison.OrdinalIgnoreCase) ||
                    p.Key.Equals("Developer", StringComparison.OrdinalIgnoreCase))
                    return false;
                var mod = ResolveModule(p);
                var act = ResolveAction(p);
                return moduleSet.Contains(mod.Key) && actionSet.Contains(act);
            })
            .Select(p => p.Id)
            .Distinct()
            .ToList();
    }

    public static List<string> SummarizeEnabledModules(IEnumerable<PermissionDefinitionDto> grantedPermissions) =>
        grantedPermissions
            .Select(ResolveModule)
            .DistinctBy(m => m.Key)
            .OrderBy(m => Modules.ToList().FindIndex(x => x.Key == m.Key))
            .Select(m => m.Label)
            .ToList();

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (var term in terms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
