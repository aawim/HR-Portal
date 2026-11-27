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
        new("security", "Access & Security", "bi-shield-lock", "Roles, permissions, portal access, user management, and staff visibility scopes."),
        new("account", "Account", "bi-person-check", "User account viewing, approvals, username, and email updates."),
        new("address", "Address", "bi-geo-alt", "Individual and entity address records."),
        new("attendance", "Attendance & Shifts", "bi-clock", "Clock events, work plans, schedules, shift rosters, break times, and out-of-office."),
        new("platform", "Audit, Logs & Developer", "bi-terminal", "Audit trails, activity logs, reports, settings, edge agents, and diagnostics."),
        new("business-entity", "Business Entity", "bi-buildings", "Business entity contacts, relations, facilities, and relationship types."),
        new("communications", "Communications", "bi-broadcast", "Announcements, intranet notices, provider integrations, and staff messaging."),
        new("staff", "Core HR & Staff", "bi-people", "General staff administration, bulk uploads, keyholders, and staff search."),
        new("data-correction", "Data Correction", "bi-pencil-square", "Data correction requests and approvals for job and position records."),
        new("documents", "Documents & Certificates", "bi-folder2-open", "Official HR documents, contracts, certificates, and signatures."),
        new("employee", "Employee", "bi-person-badge", "Employee profiles, sensitive HR records, medical, disciplinary, and directory visibility."),
        new("individual", "Individual", "bi-person-vcard", "Individual identity records, ID cards, contacts, relations, and personal documents."),
        new("items", "Items & Assets", "bi-box-seam", "Item requisitions, stock catalogue, and staff asset tracking."),
        new("job", "Job", "bi-briefcase", "Employment jobs, job requests, job leave types, and job assignments."),
        new("leave", "Leave", "bi-calendar2-check", "Leave applications, sick/family leave, entitlements, policies, and handovers."),
        new("organisation", "Organisation & Structure", "bi-building", "Departments, teams, operational groups, events, calendars, and leadership."),
        new("overtime", "Overtime", "bi-lightning-charge", "Overtime pre-approvals, overtime claims, reviews, approvals, and payments."),
        new("payroll", "Payroll & Finance", "bi-cash-stack", "Pay frameworks, payslips, allowances, deductions, loans, budgets, and currency rates."),
        new("position", "Position", "bi-diagram-2", "Positions, classifications, ranks, position configuration, and basic salary links."),
        new("project", "Project", "bi-folder-check", "Team and organisational project management."),
        new("recruitment", "Recruitment & Onboarding", "bi-person-plus", "Vacancies, applicants, evaluations, and appointment onboarding."),
        new("requests", "Requests", "bi-inbox", "Central request processing, verification, approvals, and request type rules."),
        new("staff-transfer", "Staff Transfer", "bi-arrow-left-right", "Internal staff transfers, confirmations, approvals, and activation."),
        new("task-management", "Task Management", "bi-kanban", "Work items, tasks, checklists, stacks, routine templates, priorities, and KPIs."),
        new("termination", "Termination", "bi-person-x", "Staff separations, exit clearances, final approvals, and terminations.")
    ];

    public static readonly IReadOnlyList<RoleTemplatePreset> RoleTemplates =
    [
        new("hr-manager", "HR Manager", "Full HR operations & approvals across Employee, Job, Leave, Overtime, Attendance, Transfers, Termination, Requests, and Documents.",
            ["employee", "individual", "job", "position", "staff-transfer", "termination", "data-correction", "leave", "overtime", "attendance", "requests", "staff", "organisation", "recruitment", "documents", "items", "communications"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit, AccessActionBucket.Approve, AccessActionBucket.Configure]),
        new("hr-officer", "HR Officer", "Day-to-day HR administration without system configuration or platform security.",
            ["employee", "individual", "address", "job", "position", "staff-transfer", "data-correction", "leave", "overtime", "attendance", "requests", "staff", "organisation", "recruitment", "documents", "items"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit]),
        new("payroll-officer", "Payroll Officer", "Payroll processing, allowances, deductions, loans, overtime, and attendance verification.",
            ["payroll", "overtime", "attendance", "leave", "employee", "position"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit, AccessActionBucket.Approve]),
        new("supervisor", "Department Supervisor", "View team records and approve leave, attendance, overtime, tasks, and item requests.",
            ["employee", "leave", "overtime", "attendance", "requests", "task-management", "project", "items"],
            [AccessActionBucket.View, AccessActionBucket.Approve]),
        new("attendance-officer", "Attendance Officer", "Manage schedules, duty rosters, clock records, overtime, and attendance reviews.",
            ["attendance", "overtime", "organisation", "employee"],
            [AccessActionBucket.View, AccessActionBucket.CreateEdit])
    ];

    public static AccessModuleDefinition ResolveModule(string? key, string? name = null, string? group = null)
    {
        var k = (key ?? string.Empty).Trim();
        var text = $"{key} {group} {name}";

        // Exact prefix / domain-first checks for dedicated modules
        if (StartsWithAny(k, "TaskManagement.") || ContainsAny(text, "TaskManagement"))
            return Module("task-management");
        if (StartsWithAny(k, "DataCorrections.", "DataCorrection.") || ContainsAny(text, "DataCorrection"))
            return Module("data-correction");
        if (StartsWithAny(k, "StaffTransfer.") || ContainsAny(text, "StaffTransfer"))
            return Module("staff-transfer");
        if (StartsWithAny(k, "StaffSeparation.", "TerminateAcrossOrganisation", "Terminate Across Organisation") ||
            ContainsAny(text, "StaffSeparation", "Termination", "Terminate Across"))
            return Module("termination");
        if (StartsWithAny(k, "Staff.AccessPolicy.", "Staff.AccessScope.") ||
            StartsWithAny(k, "Role.", "Roles.", "User.", "Users.", "UserOrganisation.", "UserOrganisations.", "Super.Admin.") ||
            k.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            return Module("security");
        if (StartsWithAny(k, "Position.", "Positions.", "Classifications.", "Ranks.", "JobPosition."))
            return Module("position");
        if (StartsWithAny(k, "Job.", "JobLeaveType."))
            return Module("job");
        if (StartsWithAny(k, "StaffFinance.") ||
            StartsWithAny(k, "Payroll", "Budget.", "GoodsLoan.", "SalaryRate.", "Deduction", "CurrencyRates.") ||
            ContainsAny(text, "Payroll", "Payslip", "Allowance", "Deduction", "GoodsLoan", "Loan", "Budget", "Salary", "PayFramework", "StaffFinance"))
            return Module("payroll");
        if (StartsWithAny(k, "Account.") || ContainsAny(k, "Account."))
            return Module("account");
        if (StartsWithAny(k, "Address."))
            return Module("address");
        if (StartsWithAny(k, "BusinessEntity.", "Relationship.") || ContainsAny(text, "BusinessEntity"))
            return Module("business-entity");
        if (StartsWithAny(k, "Employee.", "Directory.") || ContainsAny(k, "Employee."))
            return Module("employee");
        if (StartsWithAny(k, "Individual.", "Individuals."))
            return Module("individual");
        if (StartsWithAny(k, "Projects.", "Project."))
            return Module("project");
        if (StartsWithAny(k, "Overtime.", "OT.") || ContainsAny(text, "Overtime", "Reports.Organisation.OT"))
            return Module("overtime");
        if (StartsWithAny(k, "Requests.", "RequestTypes."))
            return Module("requests");
        if (StartsWithAny(k, "Leave.", "SickLeave.", "FamilyLeave.", "LeaveConfigurable.") ||
            ContainsAny(text, "Leave", "Handover", "Holiday"))
            return Module("leave");
        if (StartsWithAny(k, "Attendance.", "Schedule.", "ShiftSchedule.", "BreakTime.", "Attached.BreakTime.", "OutOfOffice.", "OfficialTrip.", "WorkTypes.") ||
            ContainsAny(text, "Attendance", "Workplan", "WorkPlan", "WorkType", "Schedule", "Duty", "Roster", "OutOfOffice", "LateCheckIn", "Clock", "Shift", "WeeklyHour", "BreakTime", "OfficialTrip"))
            return Module("attendance");
        if (StartsWithAny(k, "Recruitment.") || ContainsAny(text, "Recruitment", "Vacancy", "Applicant", "Onboarding", "Requisition", "Interview"))
            return Module("recruitment");
        if (StartsWithAny(k, "Document.", "Documents.", "Certificate.") || ContainsAny(text, "Document", "Certificate", "Signature", "Contract"))
            return Module("documents");
        if (StartsWithAny(k, "Asset.", "Is.Global.Items.") || ContainsAny(text, "Item", "Asset", "Inventory"))
            return Module("items");
        if (StartsWithAny(k, "Intranet.", "Communication.") || ContainsAny(text, "Communication", "Chat", "Notification", "Announcement"))
            return Module("communications");
        if (StartsWithAny(k, "Edge.", "Settings.", "Reports.", "Developer") ||
            ContainsAny(text, "Audit", "Activity", "ErrorLog", "Developer", "Workflow", "Transition", "Edge", "Diagnostic", "Sequence"))
            return Module("platform");
        if (ContainsAny(text, "Role", "Permission", "UserGroup", "Access", "Super.Admin"))
            return Module("security");
        if (StartsWithAny(k, "Organisation.", "OrganisationEvent.", "Leadership.") ||
            ContainsAny(text, "Organisation", "Department", "Team", "Group", "Location", "Calendar", "Event", "Leadership", "Structure"))
            return Module("organisation");

        return Module("staff");
    }

    private static AccessModuleDefinition Module(string key) => Modules.First(m => m.Key == key);

    private static bool StartsWithAny(string value, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
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
