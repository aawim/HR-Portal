using HRM.DTOs.Roles;
using HRM.Models;
using HRM.Models.Archives;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class PermissionSettingsService
{
    private sealed record CanonicalPermissionSpec(
        string Key,
        string Name,
        string Description,
        string ContextName,
        string ModuleName);

    private sealed record SubFunctionSpec(
        string SubFunctionKey,
        string Title,
        string Route,
        string Description,
        string[] PermissionKeys);

    private sealed record MenuAreaSpec(
        string AreaKey,
        string Title,
        string Route,
        string Icon,
        string Description,
        SubFunctionSpec[] SubFunctions);

    private sealed record ContextAreaSpec(
        string ContextKey,
        string Title,
        string Subtitle,
        string BaseRoute,
        string Icon,
        MenuAreaSpec[] Areas);

    private static readonly CanonicalPermissionSpec[] CanonicalPermissions =
    [
        new("Communication.Manage", "Manage Communication Hub", "Manage communication integrations, destinations, automation and delivery within the current tenant. Approved Admin Portal access is also required.", "Admin", "Settings"),
        // Individual & Identity
        new("Individual.View", "View Individual Registry", "View individual personal records and registry profiles.", "Admin", "HR"),
        new("Individual.Create", "Create Individual", "Register new individuals in the central registry.", "Admin", "HR"),
        new("Individual.Edit", "Edit Individual", "Update individual identity, photo, and personal details.", "Admin", "HR"),
        new("Individual.Delete", "Delete Individual", "Delete unattached individual registry records.", "Admin", "HR"),

        // Employee & Staff
        new("Employee.View", "View Employees", "View staff records, employment details, and directory profiles within scope.", "Admin", "HR"),
        new("Employee.Create", "Create / Onboard Employees", "Create staff employment records and initiate onboarding.", "Admin", "HR"),
        new("Employee.Edit", "Edit Employees", "Edit staff job records, assignments, and profile details.", "Admin", "HR"),
        new("Employee.Delete", "Delete Employees", "Delete or archive staff records.", "Admin", "HR"),
        new("Staff.Manage", "Manage Staff Roster", "Manage staff management hub, department/group assignments, and exports.", "Admin", "HR"),
        new("Staff.Add.Admin", "Admin Staff Onboarding", "Process administrative staff appointment and onboarding workflows.", "Admin", "HR"),
        new("Staff.Admin.Edit", "Admin Edit Staff Record", "Perform administrative updates on employee records.", "Admin", "HR"),
        new("Staff.EnrolmentNumbers.Manage", "Manage Staff Enrollment Numbers", "Assign and update biometric/device enrollment numbers for staff.", "Admin", "HR"),

        // Recruitment
        new("Recruitment.View", "View Recruitment", "View recruitment requisitions, vacancies, candidate progress, and recruitment reports within organisational scope.", "Admin", "HR"),
        new("Recruitment.Manage", "Manage Recruitment", "Create and manage CSC-controlled recruitment requisitions, vacancies, screening, assessment, results, offers, and onboarding handoffs.", "Admin", "HR"),

        // Data Classification & Field-Level Permissions
        new("Employee.PrivateRecord.View", "View Full Employee Private Record", "Access private personnel dossier fields and confidential employee records.", "Admin", "HR"),
        new("Employee.Salary.View", "View Employee Salary & Bank Details", "View basic salary, allowances, and employee bank account numbers.", "Admin", "Payroll"),
        new("Employee.Identification.View", "View National ID, Passport & Emergency Contacts", "View National ID card numbers, passport numbers, date of birth, and emergency contacts.", "Admin", "HR"),
        new("Employee.Disciplinary.View", "View Employee Disciplinary Records", "View confidential disciplinary actions and inquiries.", "Admin", "HR"),
        new("Employee.Medical.View", "View Employee Medical Records", "View confidential medical certificates and health records.", "Admin", "HR"),
        new("Directory.PublicProfile.View", "View Directory Public Profile", "View public staff directory information, name, designation, and unit.", "Online", "HR"),
        new("Intranet.Announcement.View", "View Intranet Announcements", "View DHR Connect intranet posts, events, and announcements.", "Online", "Intranet"),

        // Attendance & Working Hours
        new("Attendance.View", "View Attendance", "View attendance logs, clock events, and daily attendance status.", "Online", "Attendance"),
        new("Attendance.Create", "Record Attendance & Requests", "Clock in/out and submit attendance, out-of-office, or official trip requests.", "Online", "Attendance"),
        new("Attendance.Adjust", "Adjust Attendance Logs", "Manually add, adjust, or invalidate staff attendance records.", "Admin", "Attendance"),
        new("Attendance.Approve", "Approve Attendance Requests", "Approve attendance corrections, uneven logs, out-of-office, and official trip requests.", "Admin", "Attendance"),
        new("Attendance.Reject", "Reject Attendance Requests", "Reject submitted attendance, out-of-office, and official trip requests.", "Admin", "Attendance"),
        new("Attendance.Cancel", "Cancel Attendance Requests", "Cancel or withdraw pending attendance requests.", "Online", "Attendance"),
        new("Attendance.WorkingHours.Configure", "Configure Working Hours", "Configure weekly target hours, make-up rules, and working hour policies.", "Admin", "Work Planning"),
        new("Attendance.WorkingHours.Review", "Review Weekly Worked Hours", "Review staff weekly worked hours, deficits, and exceptions.", "Admin", "Attendance"),
        new("Attendance.Schedule.Amend", "Amend Official Schedule / Time Change", "Create temporary start-time or rain/weather schedule amendments.", "Admin", "Work Planning"),
        new("Schedule.Attach", "Assign Schedules & Duty Rosters", "Assign work templates, duty rosters, and manual duty shifts to units, teams, or staff.", "Admin", "Work Planning"),

        // Overtime
        new("Overtime.View", "View Overtime", "View overtime pre-approvals, claims, and overtime reports.", "Online", "Attendance"),
        new("Overtime.Create", "Submit Overtime Requests", "Submit overtime pre-approval requests and completed overtime claims.", "Online", "Attendance"),
        new("Overtime.Approve", "Approve Overtime Requests", "Review and approve staff overtime pre-approvals and claims.", "Admin", "Attendance"),
        new("Overtime.Reject", "Reject Overtime Requests", "Reject overtime pre-approvals and claims.", "Admin", "Attendance"),

        // Leave & Handovers
        new("Leave.View", "View Leave Applications & Balances", "View leave entitlements, applications, calendars, and leave reports.", "Online", "Leave"),
        new("Leave.Create", "Apply for Leave", "Submit new annual, sick, family, maternity, no-pay, or official leave applications.", "Online", "Leave"),
        new("Leave.Edit", "Edit Leave & Policies", "Modify leave applications, handover rules, leave sets, leave types, and leave policies.", "Admin", "Leave"),
        new("Leave.Approve", "Approve Leave & Handovers", "Approve employee leave applications, leave change requests, and work handovers.", "Admin", "Leave"),
        new("Leave.Reject", "Reject Leave Requests", "Reject leave applications and leave change requests.", "Admin", "Leave"),
        new("Leave.Cancel", "Cancel Leave Applications", "Cancel or request early return on leave applications.", "Online", "Leave"),
        new("Leave.CarryForward.View", "View Leave Carry-Forward", "View unused leave balances eligible for service-year carry-forward.", "Admin", "Leave"),
        new("Leave.CarryForward.Process", "Process Leave Carry-Forward", "Approve and post annual leave carry-forward balances.", "Admin", "Leave"),

        // Staff Transfers
        new("StaffTransfer.View", "View Staff Transfers", "View internal and inter-department staff transfer requests and history.", "Online", "HR"),
        new("StaffTransfer.Create", "Initiate Staff Transfer", "Create and submit new internal staff transfer requests.", "Admin", "HR"),
        new("StaffTransfer.Edit", "Edit Staff Transfer", "Update draft or pending staff transfer details and schedules.", "Admin", "HR"),
        new("StaffTransfer.Comment", "Comment on Staff Transfer", "Add review notes and comments on transfer workflows.", "Online", "HR"),
        new("StaffTransfer.Confirm", "Confirm Staff Transfer (Employee)", "Complete mandatory employee confirmation or rejection on a proposed transfer.", "Online", "HR"),
        new("StaffTransfer.Approve", "Approve Staff Transfer (Receiving Dept / HR)", "Approve incoming or outgoing department staff transfers.", "Admin", "HR"),
        new("StaffTransfer.Activate", "Activate Staff Transfer", "Execute effective-date transfer activation and job structure transition.", "Admin", "HR"),
        new("StaffTransfer.ProvisionAccess", "Provision Transfer Door & System Access", "Review and update physical door access and system scopes during transfer.", "Admin", "HR"),
        new("StaffTransfer.ExportRestrictedPdf", "Export Restricted Transfer PDF", "Generate and download official printable transfer order PDFs.", "Admin", "HR"),

        // Staff Separation & Offboarding
        new("StaffSeparation.View", "View Staff Separations", "View employee exit requests, notice periods, handovers, and clearance status.", "Online", "HR"),
        new("StaffSeparation.Initiate", "Initiate Staff Separation / Resignation", "Submit resignation or initiate an employee offboarding case.", "Online", "HR"),
        new("StaffSeparation.ClearanceReview", "Review Departmental Exit Clearances", "Verify and sign off asset, finance, IT, or department exit clearances.", "Admin", "HR"),
        new("StaffSeparation.FinalApprove", "Final HR Approval for Separation", "Perform final HR review and approval once clearances are complete.", "Admin", "HR"),
        new("StaffSeparation.Execute", "Execute Legal Termination", "Execute final employment termination and close active job assignments.", "Admin", "HR"),

        // Items & Asset Custody
        new("Asset.View", "View Items & Assets", "View item catalogue, personal custody assets, and item requisitions.", "Online", "Assets & Items"),
        new("Asset.Inventory.Manage", "Manage Item Catalogue & Inventory", "Create and manage item categories, asset units, serial numbers, and consumable stock.", "Admin", "Assets & Items"),
        new("Asset.Issuance.Initiate", "Request / Initiate Item Issuance", "Submit item requisitions or initiate asset issuance to staff.", "Online", "Assets & Items"),
        new("Asset.Acknowledge", "Acknowledge Asset Receipt", "Confirm receipt and custody of issued company assets.", "Online", "Assets & Items"),
        new("Asset.HR.Approve", "Approve Staff Item Requests", "Approve or reject job-related equipment and personal consumable requests.", "Admin", "Assets & Items"),
        new("Asset.Return.Inspect", "Inspect Returned Assets", "Inspect condition of returned assets and complete return sign-off.", "Admin", "Assets & Items"),
        new("Asset.Audit.View", "View Asset Custody Audit Trail", "Inspect full custody history and audit logs for assets.", "Admin", "Assets & Items"),
        new("Asset.ExportRestrictedPdf", "Export Printable Asset & Clearance Forms", "Print official asset handover, service, and clearance certificates.", "Admin", "Assets & Items"),
        new("Asset.Service.Request", "Request Asset Maintenance / Service", "Submit repair or maintenance requests for assigned assets.", "Online", "Assets & Items"),
        new("Asset.Service.Manage", "Manage Asset Maintenance", "Schedule and manage asset servicing and repair workflows.", "Admin", "Assets & Items"),
        new("Asset.Technician.Execute", "Execute Technician Asset Service", "Record technical diagnostics and completion on asset service tickets.", "Admin", "Assets & Items"),
        new("Asset.Upgrade.Authorize", "Authorize Asset Hardware Upgrade", "Approve hardware upgrade or replacement requests for active assets.", "Admin", "Assets & Items"),
        new("Asset.Clearance.Authorize", "Authorize Asset Exit Clearance", "Certify full asset return clearance for separating or transferring staff.", "Admin", "Assets & Items"),

        // Certificates
        new("Certificate.Request", "Request Employment Certificate", "Submit requests for official employment, experience, or salary certificates.", "Online", "HR"),
        new("Certificate.View", "View Employment Certificates", "View certificate requests and download issued certificates.", "Online", "HR"),
        new("Certificate.Review", "Review Certificate Requests", "Verify employment history and review pending certificate requests.", "Admin", "HR"),
        new("Certificate.Approve", "Approve & Issue Certificates", "Approve certificate issuance and generate official signed certificates.", "Admin", "HR"),
        new("Certificate.Revoke", "Revoke / Reissue Certificates", "Revoke or supersede previously issued employment certificates.", "Admin", "HR"),
        new("Certificate.Generate", "Generate Certificate PDF", "Render official QR-verified certificate PDFs.", "Admin", "HR"),

        // Document Centre (DHR-DMS)
        new("Document.ViewMetadata", "View Document Metadata", "Browse document registries, metadata, and version lists.", "Online", "Documents"),
        new("Document.Preview", "Preview Documents", "Preview PDF and image documents inline in the browser.", "Online", "Documents"),
        new("Document.Download", "Download Documents", "Download SHA-256 verified document files.", "Online", "Documents"),
        new("Document.Upload", "Upload / Register Documents", "Upload new documents into staff dossiers or organisation libraries.", "Online", "Documents"),
        new("Document.ModifyMetadata", "Modify Document Metadata & Links", "Update document classifications, tags, and record links.", "Admin", "Documents"),
        new("Document.AddVersion", "Upload New Document Version", "Upload new versions and restore prior versions of registered documents.", "Admin", "Documents"),
        new("Document.SubmitApproval", "Submit Document for Approval", "Submit draft documents into the verification and publishing workflow.", "Admin", "Documents"),
        new("Document.Review", "Review Pending Documents", "Review documents and employee document requests awaiting verification.", "Admin", "Documents"),
        new("Document.Approve", "Approve & Publish Documents", "Approve documents as official records and issue requested letters.", "Admin", "Documents"),
        new("Document.Sign", "Sign Official Documents", "Apply official signatory approval on generated template documents.", "Admin", "Documents"),
        new("Document.Share", "Manage Document Access (ACL)", "Grant or revoke document-level access control entries.", "Admin", "Documents"),
        new("Document.Archive", "Archive Documents & Legal Holds", "Archive records and place or release Legal Holds.", "Admin", "Documents"),
        new("Document.Dispose", "Dispose Expired Documents", "Execute retention-based disposition of expired non-held documents.", "Admin", "Documents"),
        new("Document.Configure", "Configure DMS Types & Templates", "Manage document categories, document types, retention policies, and merge templates.", "Admin", "Documents"),

        // Payroll & Staff Finance
        new("Payroll.PayslipView", "View Personal Payslips", "View and download personal monthly payslips.", "Online", "Payroll"),
        new("Payroll.View", "View Enterprise Payroll", "View payroll periods, runs, and summary totals.", "Admin", "Payroll"),
        new("Payroll.Configure", "Configure Payroll Periods & Rules", "Create payroll periods, cut-off dates, and attendance deduction rules.", "Admin", "Payroll"),
        new("Payroll.Prepare", "Prepare Payroll Run", "Create monthly payroll runs and stage employee inputs.", "Admin", "Payroll"),
        new("Payroll.Calculate", "Execute Payroll Calculation Engine", "Trigger background payroll calculation and recalculation jobs.", "Admin", "Payroll"),
        new("Payroll.Review", "Review Employee Payroll Results", "Inspect calculated employee payslips, variances, and deductions.", "Admin", "Payroll"),
        new("Payroll.Approve", "Approve Payroll Run", "Approve verified payroll runs for finalization.", "Admin", "Payroll"),
        new("Payroll.Finalise", "Finalise & Lock Payroll Run", "Finalise payroll runs and lock period calculations.", "Admin", "Payroll"),
        new("Payroll.Export", "Export Payroll Reports & Bank Files", "Export payroll summaries, bank transfer files, and statutory reports.", "Admin", "Payroll"),
        new("Payroll.PaymentPrepare", "Prepare Payment Batches", "Generate bank payment batches for finalised payroll runs.", "Admin", "Payroll"),
        new("Payroll.PaymentAuthorise", "Authorise Bank Payment Batches", "Authorise and release payment batches for disbursement.", "Admin", "Payroll"),
        new("Payroll.AuditView", "View Payroll Audit Trail", "Inspect immutable calculation and approval audit logs.", "Admin", "Payroll"),
        new("StaffFinance.View", "View Allowances & Deductions", "View personal allowances, deductions, and finance requests.", "Online", "Payroll"),
        new("StaffFinance.Configure", "Configure Position Allowances & Finance Rules", "Configure allowance types, deduction types, and financial rules.", "Admin", "Payroll"),
        new("StaffFinance.Manage", "Manage Staff Allowances & Deductions", "Create and manage recurring or one-off employee allowances and deductions.", "Admin", "Payroll"),
        new("StaffFinance.Assess", "Assess Staff Finance Requests", "Review and approve employee allowance/deduction requests.", "Admin", "Payroll"),
        new("StaffFinance.Payroll", "Post Allowances & Deductions to Payroll", "Post approved allowances, deductions, and loan installments to payroll.", "Admin", "Payroll"),

        // Budget
        new("Budget.View", "View State Budget & Reports", "View fiscal years, MoF economic code allocations, and budget execution reports.", "Admin", "Budget"),
        new("Budget.Manage", "Manage Budget Allocations, Virements & Ledger", "Create fiscal years, allocations, MoF economic codes, virements, and ledger transactions.", "Admin", "Budget"),
        new("Budget.Approve", "Approve Budget Virements & Solvency", "Approve budget virements and pre-payroll budget solvency checks.", "Admin", "Budget"),
        new("Budget.Export", "Export MoF Budget Execution Statements", "Export CSV and PDF budget execution statements for the Ministry of Finance.", "Admin", "Budget"),

        // Goods Loans
        new("GoodsLoan.View", "View Goods Loans", "View staff goods loan eligibility, active loans, and repayment schedules.", "Online", "Goods Loans"),
        new("GoodsLoan.Create", "Apply for Goods Loan", "Submit staff goods loan applications for partner merchant stores.", "Online", "Goods Loans"),
        new("GoodsLoan.Manage", "Manage Goods Loans, Stores & Settlements", "Manage merchant stores, agreements, deduction policies, loan approvals, and settlements.", "Admin", "Goods Loans"),

        // Positions, Work Types & Projects
        new("Position.Details", "View Position Overview & Matrix", "View position overview, pay frameworks, ranks, and classifications.", "Admin", "Settings"),
        new("Positions.Configure", "Configure Pay Frameworks, Ranks & Positions", "Create and edit pay frameworks, Job Matrices, ranks, classifications, and positions.", "Admin", "Settings"),
        new("Staff.Positions.Manage", "Manage Staff Position Assignments", "Assign positions to staff and approve position change requests.", "Admin", "HR"),
        new("WorkTypes.Manage", "Manage Work Types Catalogue", "Create and manage the organisation Work Types catalogue.", "Admin", "Settings"),
        new("Job.WorkType.Assign", "Assign Work Types to Staff", "Assign work modalities/types (e.g., On-Site, Work From Home) to employee jobs.", "Admin", "HR"),
        new("Job.WorkType.ViewAssigned", "View Assigned Staff Work Types", "View work types assigned to employee jobs.", "Admin", "HR"),
        new("Projects.Manage", "Manage Projects & Tasks", "Create and manage projects, milestones, deliverables, and task assignments.", "Admin", "Projects & Tasks"),

        // Organisation, Users, Roles & Access Control
        new("Organisation.Edit", "Manage Organisation Settings & Structure", "Edit organisation details, structures, calendars, locations, teams, and supervisors.", "Admin", "Settings"),
        new("UserOrganisations.View", "View User Organisations", "View organisation workspaces and tenant details.", "Admin", "Settings"),
        new("UserOrganisations.Admin.View", "Manage Admin Portal Access Grants", "View and manage multi-stage Admin Portal access requests and grants.", "Admin", "Settings"),
        new("UserOrganisations.Roles.Edit", "Manage Organisation Permission Ceiling", "Configure the permissions enabled for an organisation.", "Admin", "Settings"),
        new("User.Manage", "Manage User Accounts", "Create, edit, enable, and disable user accounts.", "Admin", "Settings"),
        new("Users.Edit", "Edit User Account Details", "Update user account bindings and status.", "Admin", "Settings"),
        new("Users.Roles.View", "View User Account Permissions", "View roles and direct permissions assigned to user accounts.", "Admin", "Settings"),
        new("Users.Roles.Edit", "Edit User Account Permissions", "Assign or revoke roles and direct permissions on user accounts.", "Admin", "Settings"),
        new("Roles.View", "View Permission Definitions", "View system permission definitions and area groupings.", "Admin", "Settings"),
        new("Roles.Add", "Create Permission Definitions", "Create new permission definitions.", "Admin", "Settings"),
        new("Roles.Edit", "Edit Permission Definitions", "Update permission names, descriptions, contexts, and modules.", "Admin", "Settings"),
        new("Role.Manage", "Manage Roles & Permission Assignments", "Create and edit roles (user groups) and their bundled permissions.", "Admin", "Settings"),
        new("UserOrganisation.Groups.View", "View Organisation Roles (User Groups)", "View roles and assigned staff in the organisation.", "Admin", "Settings"),
        new("UserOrganisation.Groups.Create", "Create Organisation Roles (User Groups)", "Create new role groups in the organisation.", "Admin", "Settings"),
        new("UserOrganisation.Groups.Edit", "Edit Organisation Roles (User Groups)", "Update role group names and assigned permissions.", "Admin", "Settings"),
        new("Staff.AccessPolicy.Manage", "Manage Classification Access Policies", "Configure ABAC classification policies and explicit-only staff access grants.", "Admin", "Settings"),
        new("Staff.AccessScope.Manage", "Manage Organisational Access Scopes", "Assign, edit, and revoke hierarchical organisational access scopes (Division, Department, Team, Self).", "Admin", "Settings"),
        new("Staff.AccessScope.Simulate", "Simulate Effective Access Preview", "Run the Effective Access Preview simulator for any user in the organisation.", "Admin", "Settings"),

        // Leadership, Intranet & Platform Governance
        new("Leadership.Appointment.Manage", "Manage Leadership Appointments & Authority", "Manage MDS leadership appointments, organisation responsibilities, authority grants, and delegations.", "Admin", "Leadership"),
        new("OrganisationEvent.Manage", "Manage Organisation Events & Intranet", "Create and edit official organisation events and intranet content.", "Admin", "Intranet"),
        new("OrganisationEvent.Publish", "Publish Organisation Events & Announcements", "Approve and publish organisation events and intranet announcements.", "Admin", "Intranet"),
        new("Edge.Agent.Manage", "Manage Attendance Edge Agents & PKI", "Enrol attendance edge agents, manage trusted PKI roots, and bind biometric devices.", "Developer", "Developer"),
        new("Developer", "Developer Portal Access", "Access the Developer Workspace, error logs, state transitions, workflows, and diagnostics.", "Developer", "Developer"),
        new("Admin", "Organisation Administrator", "Broad administrative access across organisation settings and HR modules.", "Admin", "Settings"),
        new("Super.Admin.Manage", "Platform Super Administrator", "Cross-tenant platform administration (direct audited grant only).", "Developer", "Developer")
    ];

    private static readonly ContextAreaSpec[] SiteMapHierarchy =
    [
        // =====================================================================
        // CONTEXT 1: ONLINE (EMPLOYEE SELF-SERVICE PORTAL)
        // =====================================================================
        new(
            "Online",
            "Online (Employee Self-Service Portal)",
            "Personal workforce portal for time & attendance, leave applications, duty rosters, assets, certificates, documents, payslips, and intranet.",
            "/online",
            "bi-person-workspace",
            [
                new(
                    "online-home",
                    "Home (User Dashboard & Profile)",
                    "/online",
                    "bi-house-door",
                    "Employee landing dashboard, time clock, attendance summary cards, work plan, and personal profile.",
                    [
                        new("online-dashboard", "User Dashboard & Overview Cards", "/online", "Personal attendance heatmap, recent leaves, upcoming events, and identity expiry alerts.",
                            ["Employee.View", "Attendance.View", "Leave.View", "Directory.PublicProfile.View", "Intranet.Announcement.View"]),
                        new("online-timeclock", "Time Clock & Test Punch Card", "/online", "Live check-in/check-out punch buttons, today's attendance log card, and uneven log alerts.",
                            ["Attendance.Create", "Attendance.View"]),
                        new("online-workplan-hours", "Today's Work Plan & Weekly Worked Hours", "/online", "Daily scheduled segments, weekly target vs worked hours, and rain/weather amendments.",
                            ["Attendance.View", "Attendance.WorkingHours.Review"]),
                        new("online-profile", "My Profile & Identity Card", "/profile", "View personal profile, update profile photo, active job summary, employment history, and leave history.",
                            ["Individual.View", "Individual.Edit", "Employee.View"])
                    ]),
                new(
                    "online-attendance",
                    "Attendance",
                    "/online/attendance",
                    "bi-clock-history",
                    "Personal attendance logs, clock events, correction requests, organisation daily board, out-of-office, and official trips.",
                    [
                        new("online-my-attendance", "My Attendance", "/online/attendance/my-attendance", "View personal attendance logs, lateness records, monthly/yearly summaries, and PDF/CSV exports.",
                            ["Attendance.View", "Attendance.Create"]),
                        new("online-clock-events", "My Clock Events", "/online/attendance/my-clock-events", "Inspect daily check-in/check-out records alongside scheduled shift segments and physical punches.",
                            ["Attendance.View", "Attendance.Create"]),
                        new("online-attendance-request", "Attendance Request", "/online/attendance/attendance-request", "Submit and track manual attendance correction requests and review received team requests.",
                            ["Attendance.Create", "Attendance.View", "Attendance.Approve", "Attendance.Reject", "Attendance.Cancel"]),
                        new("online-organisation-daily", "Organisation Daily", "/online/attendance/organisation-daily", "Review daily attendance status, on-leave staff, and workforce presence across authorised units.",
                            ["Attendance.View", "Directory.PublicProfile.View"]),
                        new("online-out-of-office", "Out Of Office Request", "/online/attendance/out-of-office-request", "Request approval for off-site work, client visits, meetings, or temporary short absences.",
                            ["Attendance.Create", "Attendance.View", "Attendance.Approve", "Attendance.Reject", "Attendance.Cancel"]),
                        new("online-official-trip", "Official Request (Official Trip)", "/online/attendance/official-request", "Submit and manage official duty, business travel, and field work trip requests.",
                            ["Attendance.Create", "Attendance.View", "Attendance.Approve", "Attendance.Reject", "Attendance.Cancel"])
                    ]),
                new(
                    "online-duty-roster",
                    "Duty Roster",
                    "/online/duty-roster",
                    "bi-calendar3",
                    "View assigned duty shifts, accept duties, and request or approve shift swaps and transfers.",
                    [
                        new("online-duty-roster-view", "My Duty Roster, Shift Swaps & Transfers", "/online/duty-roster", "Inspect scheduled duties, accept assignments, request duty swaps/transfers, and approve peer swaps.",
                            ["Attendance.View", "Schedule.Attach", "Attendance.Approve", "Attendance.Reject"])
                    ]),
                new(
                    "online-break-time",
                    "Break Time Request",
                    "/online/break-time-request",
                    "bi-cup-hot",
                    "Employee break time requests and break schedule tracking.",
                    [
                        new("online-break-requests", "Break Time Requests", "/online/break-time-request", "Submit and view break time requests and break logs.",
                            ["Attendance.Create", "Attendance.View", "Attendance.Approve"])
                    ]),
                new(
                    "online-overtime",
                    "Overtime",
                    "/online/ot",
                    "bi-lightning-charge",
                    "Overtime pre-approval tasks, completed overtime claims with clock evidence, and approvals.",
                    [
                        new("online-ot-requests", "Overtime Pre-Approval & Claims", "/online/ot", "Request overtime pre-approval, submit completed overtime claims with clock evidence, and review team OT.",
                            ["Overtime.View", "Overtime.Create", "Overtime.Approve", "Overtime.Reject", "Attendance.View"])
                    ]),
                new(
                    "online-items-assets",
                    "Item Requests & My Assets",
                    "/online/item-requests",
                    "bi-box-seam",
                    "Requisition job equipment or personal supplies, and manage company assets held in personal custody.",
                    [
                        new("online-item-requests", "Item Requests (Equipment & Personal Supplies)", "/online/item-requests", "Request job-related equipment, hardware upgrades, replacements, or personal office supplies.",
                            ["Asset.View", "Asset.Issuance.Initiate", "Asset.Service.Request", "Asset.Upgrade.Authorize"]),
                        new("online-my-assets", "My Assets (Custody, Handover & Service)", "/online/my-assets", "View assigned assets in custody, acknowledge handover receipt, request maintenance, or initiate return.",
                            ["Asset.View", "Asset.Acknowledge", "Asset.Service.Request"])
                    ]),
                new(
                    "online-goods-loans",
                    "Goods Loans",
                    "/online/goods-loans",
                    "bi-cash-coin",
                    "Employee goods loan applications, credit limit eligibility, and salary deduction installment schedules.",
                    [
                        new("online-goods-loans-portal", "Staff Goods Loans Portal", "/online/goods-loans", "Apply for goods loans at partner merchant stores and track monthly repayment installments.",
                            ["GoodsLoan.View", "GoodsLoan.Create", "GoodsLoan.Manage"])
                    ]),
                new(
                    "online-transfers-separations",
                    "Staff Transfers & Separation",
                    "/online/staff-transfers",
                    "bi-arrow-left-right",
                    "Employee self-service for internal transfer confirmations and resignation/offboarding handovers.",
                    [
                        new("online-staff-transfers", "My Staff Transfers & Mandatory Confirmation", "/online/staff-transfers", "Review proposed internal transfers, add comments, and submit mandatory staff confirmation.",
                            ["StaffTransfer.View", "StaffTransfer.Confirm", "StaffTransfer.Comment"]),
                        new("online-staff-separation", "Staff Separation & Exit Handover Portal", "/online/staff-separation", "Submit resignation, complete exit handover tasks, and track departmental clearance progress.",
                            ["StaffSeparation.View", "StaffSeparation.Initiate"])
                    ]),
                new(
                    "online-certificates-documents",
                    "Certificates & Documents",
                    "/online/certificates",
                    "bi-file-earmark-check",
                    "Request official employment certificates and access personal dossier and organisation documents.",
                    [
                        new("online-certificates", "Employment & Experience Certificates", "/online/certificates", "Request official certificates, respond to clarifications, and download signed QR-verified PDFs.",
                            ["Certificate.Request", "Certificate.View"]),
                        new("online-documents", "My Documents & Official Letter Requests", "/online/documents", "View personal dossier files, organisation circulars, and request official HR letters.",
                            ["Document.ViewMetadata", "Document.Preview", "Document.Download", "Document.Upload"])
                    ]),
                new(
                    "online-intranet-chat",
                    "Intranet & Staff Chat",
                    "/online/dhr-connect",
                    "bi-chat-dots",
                    "DHR Connect organisation social feed, announcements, events, and real-time direct/group chat.",
                    [
                        new("online-dhr-connect", "Intranet (DHR Connect & Organisation Hub)", "/online/dhr-connect", "View organisation announcements, colleague updates, new joiners, and upcoming events.",
                            ["Intranet.Announcement.View", "Directory.PublicProfile.View"]),
                        new("online-staff-chat", "Staff Chat & Request Cards", "/online/chat", "Direct 1-on-1 messaging, group chats, and interactive workflow request cards.",
                            ["Directory.PublicProfile.View", "Employee.View"])
                    ]),
                new(
                    "online-leaves-handovers",
                    "Leaves & Work Handovers",
                    "/online/leaves",
                    "bi-calendar2-week",
                    "Leave applications, printable leave chits, new leave requests, leave changes, and delegated work handovers.",
                    [
                        new("online-leave-applications", "Leave Applications & Printable Receipt", "/online/leaves/search", "Browse personal leave history, check approval stages, and print official leave receipts.",
                            ["Leave.View", "Leave.Cancel"]),
                        new("online-new-leave", "New Leave Application (Annual, Sick, Family, Maternity, No-Pay, Official Trip)", "/online/leaves/new-leave-application", "Lodge new leave applications with handover tasks and medical/supporting evidence.",
                            ["Leave.Create", "Leave.View"]),
                        new("online-leave-change", "Leave Change Request (Early Return / Extension)", "/online/leaves/leave-change-request", "Request modifications, early returns, or cancellations for approved leave.",
                            ["Leave.Edit", "Leave.Cancel", "Leave.Approve", "Leave.Reject"]),
                        new("online-work-handovers", "Work Handovers", "/online/leave-handovers", "Accept or reject delegated tasks during colleague leave and review handover completion.",
                            ["Leave.View", "Leave.Edit", "Leave.Approve"])
                    ]),
                new(
                    "online-payslips-finance-leadership",
                    "Payslips, Allowances & Leadership",
                    "/online/my-payslips",
                    "bi-wallet2",
                    "Personal monthly payslips, allowances & deductions requests, position/supervisor requests, and leadership workspace.",
                    [
                        new("online-my-payslips", "My Payslips", "/online/my-payslips", "View and download personal monthly payslips and earnings/deductions breakdowns.",
                            ["Payroll.PayslipView"]),
                        new("online-allowances-deductions", "Allowances & Deductions", "/online/allowances-deductions", "View personal allowances and deductions and submit staff finance requests.",
                            ["StaffFinance.View", "StaffFinance.Manage"]),
                        new("online-leadership-workspace", "Leadership Workspace", "/online/leadership", "Cross-organisation oversight workspace for appointed ministers, board chairs, and executives.",
                            ["Leadership.Appointment.Manage", "Employee.View"]),
                        new("online-position-supervisor-requests", "Staff Position & Supervisor Change Requests", "/online/staff-position-requests", "View and act on staged staff position and supervisor change workflow requests.",
                            ["Staff.Positions.Manage", "Position.Details", "Organisation.Edit"])
                    ])
            ]),

        // =====================================================================
        // CONTEXT 2: ADMIN (ADMIN & HR MANAGEMENT PORTAL)
        // =====================================================================
        new(
            "Admin",
            "Admin (HR & Management Portal)",
            "Enterprise administration for supervised teams, HR operations, attendance, leave, payroll, state budget, document governance, and system settings.",
            "/admin",
            "bi-shield-check",
            [
                new(
                    "admin-workspace",
                    "Home — Admin Workspace",
                    "/admin/my/workspace",
                    "bi-briefcase",
                    "Unified management workspace with 13 folder tabs for supervised teams, projects, calendars, structures, requests, onboarding, and 16-tab Staff Workspace.",
                    [
                        new("admin-ws-home", "Workspace Home, Team KPIs & Directory Search", "/admin/my/workspace", "Live workforce metrics, supervised team roster, recent searches, and personal to-do tasks.",
                            ["Employee.View", "Attendance.View", "Leave.View"]),
                        new("admin-ws-teams", "Teams & Sub-Teams Center (Members, Member 360°, Evaluations, Schedule, Leaves)", "/admin/my/workspace?tab=teams", "Manage supervised teams, sub-teams, Member 360° work summaries, and performance evaluations.",
                            ["Employee.View", "Employee.Edit", "Organisation.Edit", "Schedule.Attach", "Leave.Approve"]),
                        new("admin-ws-projects", "Projects & Tasks Tab", "/admin/my/workspace?tab=projects", "Create and manage team projects, milestones, deliverables, and task assignments.",
                            ["Projects.Manage", "Employee.View"]),
                        new("admin-ws-calendars", "Calendars (Organisation & Supervisor Calendars)", "/admin/my/workspace?tab=calendar", "View organisation working/holiday calendars and supervised team schedules.",
                            ["Organisation.Edit", "Attendance.View", "Leave.View"]),
                        new("admin-ws-structures", "Structures (Approved Hierarchy & Unit Details)", "/admin/my/workspace?tab=structures", "Inspect organisation structure tree, current/former unit staff, schedules, and leave sets.",
                            ["Employee.View", "Organisation.Edit"]),
                        new("admin-ws-requests", "Requests Command Center (Leave, Attendance, Uneven Log, Duty, OT, Late, Position, Items, Certificates)", "/admin/my/workspace?tab=request", "Unified approval command center across all 10 workforce request categories.",
                            ["Leave.Approve", "Leave.Reject", "Attendance.Approve", "Attendance.Reject", "Overtime.Approve", "Staff.Positions.Manage", "Asset.HR.Approve", "Certificate.Review", "Certificate.Approve"]),
                        new("admin-ws-onboarding", "Staff Onboarding (Search Individuals, Former Staff, Add Individual, Appointment Requests)", "/admin/my/workspace/onboarding", "Search unattached registry individuals, register new people, and process multi-stage appointment requests.",
                            ["Individual.View", "Individual.Create", "Individual.Edit", "Employee.Create", "Staff.Add.Admin", "Staff.Manage"]),
                        new("admin-ws-attendance-timechange", "Leave Log, Attendance, Time Change & Org Daily Tabs", "/admin/my/workspace?tab=attendance", "Review weekly worked hours, exceptions, start-time/rain amendments, and daily attendance.",
                            ["Attendance.View", "Attendance.WorkingHours.Review", "Attendance.Schedule.Amend", "Leave.View"]),
                        new("admin-ws-staff-workspace", "Staff Workspace (16 Employee Tabs: Overview, Performance, Leave, Attendance, Positions, Finance, OT, Items, Documents, Schedule, Work Types, Transfer, Certificates, Goods Loans, Bank, Emergency Contacts)", "/admin/my/workspace/staff", "Full 360° employee record management and field-level data classification controls.",
                            ["Employee.View", "Employee.Edit", "Employee.PrivateRecord.View", "Employee.Salary.View", "Employee.Identification.View", "Employee.Disciplinary.View", "Employee.Medical.View", "Staff.Positions.Manage", "Schedule.Attach", "Job.WorkType.Assign"]),
                        new("admin-ws-org-workspace", "Organisation Workspace (Overview, Staff, Teams, Roles, Permissions, Leave Types, Documents)", "/admin/my/workspace/organisation", "Dedicated organisation inspector for structure, workforce, roles, and permissions.",
                            ["UserOrganisations.View", "Organisation.Edit", "UserOrganisations.Roles.Edit"])
                    ]),
                new(
                    "admin-dashboard",
                    "Dashboard — Executive Organisation Overview",
                    "/admin",
                    "bi-speedometer2",
                    "Executive KPIs, department status cards, request activity, headcount distribution, gender & work modality analytics.",
                    [
                        new("admin-exec-overview", "Organisation Overview & People Analytics", "/admin", "Filterable executive dashboard for workforce headcount, attendance, modality, and processing requests.",
                            ["Employee.View", "Attendance.View", "Leave.View"])
                    ]),
                new(
                    "admin-hr-attendance",
                    "HR -> Attendance",
                    "/admin/attendance",
                    "bi-clock",
                    "Organisation attendance management, leave administration, overtime management, and attendance/fine reports.",
                    [
                        new("admin-att-individual-dept", "Individual, Department & Organisation Daily Attendance", "/admin/attendance/manage-attendance/individual-attendance", "Check detailed attendance records for individuals, departments, former staff, and daily organisation boards.",
                            ["Attendance.View", "Employee.View"]),
                        new("admin-att-bulk-invalidate", "Add Bulk Attendance & Attendance Invalidate Requests", "/admin/attendance/manage-attendance/add-bulk-attendance", "Upload/enter bulk attendance and process requests to void or invalidate incorrect logs.",
                            ["Attendance.Adjust", "Attendance.Create", "Attendance.Approve"]),
                        new("admin-att-requests-trips", "Attendance Requests, Uneven Logs, Out Of Office & Official Trips", "/admin/attendance/manage-attendance/attendance-requests", "Review and approve manual attendance entries, uneven logs, out-of-office, and official trips.",
                            ["Attendance.View", "Attendance.Approve", "Attendance.Reject", "Attendance.Cancel"]),
                        new("admin-att-manage-leaves", "Manage Leaves (Search, Leave Change Request, Staff & Org Leave Reports, Bulk Data Entry)", "/admin/attendance/manage-leaves", "Search and manage organisation leave applications, changes, and leave balance reports.",
                            ["Leave.View", "Leave.Create", "Leave.Edit", "Leave.Approve", "Leave.Reject", "Leave.Cancel"]),
                        new("admin-att-manage-overtimes", "Manage Overtimes (OT Pre-Approval, OT Request, Staff & Org OT Reports)", "/admin/attendance/manage-overtimes", "Manage overtime pre-approvals, claims, clock evidence verification, and overtime cost reports.",
                            ["Overtime.View", "Overtime.Create", "Overtime.Approve", "Overtime.Reject"]),
                        new("admin-att-manage-reports", "Manage Reports (Daily, Staff, Team, OT, Leave, Break Fine & Late Fine Reports)", "/admin/attendance/manage-reports", "Generate attendance, overtime, leave, break fine, and late fine reports across the organisation.",
                            ["Attendance.View", "Leave.View", "Payroll.View"])
                    ]),
                new(
                    "admin-hr-performance",
                    "HR -> Performance",
                    "/admin/performance",
                    "bi-graph-up-arrow",
                    "Employee performance management, checklists, goals, and appraisals.",
                    [
                        new("admin-performance-hub", "Performance Management Hub & Evaluations", "/admin/performance", "Manage employee performance reviews, evaluations, and work priorities.",
                            ["Employee.View", "Employee.Edit"])
                    ]),
                new(
                    "admin-hr-payroll",
                    "HR -> Payroll",
                    "/admin/payroll",
                    "bi-cash-stack",
                    "Enterprise multi-organisation payroll processing, calculation engine, bank payment batches, attendance deductions, and allowances.",
                    [
                        new("admin-payroll-runs", "Runs & Calculation Engine", "/admin/payroll/payroll", "Create monthly payroll runs, trigger deterministic calculation engine, and manage approval/finalization stages.",
                            ["Payroll.View", "Payroll.Prepare", "Payroll.Calculate", "Payroll.Approve", "Payroll.Finalise", "Payroll.AuditView"]),
                        new("admin-payroll-employees", "Employee Payroll Results", "/admin/payroll/payroll", "Review per-employee gross earnings, allowances, pension, tax, deductions, and net pay.",
                            ["Payroll.View", "Payroll.Review", "Payroll.PayslipView", "Employee.Salary.View"]),
                        new("admin-payroll-payments", "Payment Batches & Bank Files", "/admin/payroll/payroll", "Prepare and authorise idempotent bank transfer batches and export payment files.",
                            ["Payroll.PaymentPrepare", "Payroll.PaymentAuthorise", "Payroll.Export", "Employee.Salary.View"]),
                        new("admin-payroll-periods-deductions", "Periods, Configurations & Attendance Deductions", "/admin/payroll/payroll", "Configure payroll periods, wage codes, and automated late/absence/break attendance deductions.",
                            ["Payroll.Configure", "Payroll.View", "Attendance.View"]),
                        new("admin-payroll-finance", "Allowances and Deductions", "/admin/payroll/finance", "Manage position allowances, recurring/one-off staff allowances & deductions, and payroll postings.",
                            ["StaffFinance.Configure", "StaffFinance.Manage", "StaffFinance.Assess", "StaffFinance.Payroll", "Employee.Salary.View"])
                    ]),
                new(
                    "admin-hr-budget",
                    "HR -> Budget",
                    "/admin/budget",
                    "bi-bar-chart-line",
                    "State Budget & Appropriations: Fiscal Years, MoF Economic Codes, Allocations, Virements, Ledger, and Payroll Budget Bridge.",
                    [
                        new("admin-budget-overview", "Overview & Execution Reports (MoF CSV/PDF)", "/admin/budget/overview", "Executive summary of budget allocations, Personal Emoluments utilization, and MoF exports.",
                            ["Budget.View", "Budget.Export"]),
                        new("admin-budget-operations", "Budget Operations & Fiscal Years (Allocations, MoF Economic Codes, Virements, Ledger)", "/admin/budget/manage", "Configure Fiscal Years, State Budget Allocations, MoF Economic Codes, Virements, and Ledger Transactions.",
                            ["Budget.View", "Budget.Manage", "Budget.Approve"]),
                        new("admin-budget-bridge", "Payroll Budget Bridge", "/admin/budget/payroll-bridge", "Automated pre-payroll budget solvency checks and payroll component to MoF COA mappings.",
                            ["Budget.View", "Budget.Manage", "Payroll.View"])
                    ]),
                new(
                    "admin-hr-staff",
                    "HR -> Staff, Transfers, Separation & Handovers",
                    "/admin/hr/staff/manage",
                    "bi-people",
                    "Workforce roster management, internal transfers, offboarding/separations, duty rosters, and work handovers.",
                    [
                        new("admin-hr-staff-manage", "Manage Staff, Search, Former Staff, Enrollment & Bulk Upload", "/admin/hr/staff/manage", "Comprehensive staff roster (All, By Department, By Groups), CSV/PDF exports, enrollment numbers, and bulk upload.",
                            ["Employee.View", "Employee.Create", "Employee.Edit", "Staff.Manage", "Staff.Admin.Edit", "Staff.EnrolmentNumbers.Manage", "Staff.Add.Admin", "Employee.PrivateRecord.View", "Employee.Salary.View", "Employee.Identification.View"]),
                        new("admin-hr-transfers", "Staff Transfers (Drafts, Confirmations, Approvals, Scheduled, Activation & Door Access)", "/admin/hr/staff-transfers", "Initiate, confirm, approve, schedule, and activate internal staff transfers with printable transfer orders.",
                            ["StaffTransfer.View", "StaffTransfer.Create", "StaffTransfer.Edit", "StaffTransfer.Comment", "StaffTransfer.Confirm", "StaffTransfer.Approve", "StaffTransfer.Activate", "StaffTransfer.ProvisionAccess", "StaffTransfer.ExportRestrictedPdf"]),
                        new("admin-hr-separations", "Staff Separation & Offboarding (Handovers, Clearances, HR Review & Termination)", "/admin/hr/separations", "Manage resignations, notice periods, supervisor handovers, multi-department clearances, and termination.",
                            ["StaffSeparation.View", "StaffSeparation.Initiate", "StaffSeparation.ClearanceReview", "StaffSeparation.FinalApprove", "StaffSeparation.Execute"]),
                        new("admin-hr-duty-handovers", "Duty Roster Schedule Manager & Work Handovers", "/admin/hr/work-planning/duty-roster", "Manage duty rosters, draft planner, schedule assignments, manual duties, policies, and work handovers.",
                            ["Schedule.Attach", "Attendance.View", "Attendance.Adjust", "Leave.View", "Leave.Approve"])
                    ]),
                new(
                    "admin-hr-documents-certificates",
                    "HR -> Document Centre & Certificates",
                    "/admin/hr/documents",
                    "bi-folder2-open",
                    "Enterprise Document & Records Management (DHR-DMS) and Official Employment Certificate Authority.",
                    [
                        new("admin-dms-centre", "Document Centre (Dashboard, Central Registry, Staff Dossiers, Org Libraries, Approvals, Requests, Templates, Archives & Holds, Config)", "/admin/hr/documents", "SHA-256 verified document vault, versioning, templates, legal holds, ACLs, and official letter issuance.",
                            ["Document.ViewMetadata", "Document.Preview", "Document.Download", "Document.Upload", "Document.ModifyMetadata", "Document.AddVersion", "Document.SubmitApproval", "Document.Review", "Document.Approve", "Document.Sign", "Document.Share", "Document.Archive", "Document.Dispose", "Document.Configure"]),
                        new("admin-certificates-centre", "Employment & Experience Certificates Management", "/admin/hr/certificates", "Review certificate requests, verify employment records, approve & issue, reissue, or revoke certificates.",
                            ["Certificate.View", "Certificate.Review", "Certificate.Approve", "Certificate.Revoke", "Certificate.Generate"])
                    ]),
                new(
                    "admin-hr-departments-groups-items",
                    "HR -> Departments, Requests, Supervisors, Groups & Items",
                    "/admin/hr/departments/management",
                    "bi-building",
                    "Department management & matrix, HR requests inbox, supervisor assignments, operational groups, and item/asset custody.",
                    [
                        new("admin-hr-departments", "Departments Management & Department Matrix", "/admin/hr/departments/management", "View approved departments, assigned supervisors, workforce capacity, and leave overview.",
                            ["Employee.View", "Organisation.Edit", "Attendance.View", "Leave.View"]),
                        new("admin-hr-requests-inbox", "Received Requests Inbox", "/admin/hr/requests/received", "Unified HR request inbox grouped by request type with filtering and approval links.",
                            ["Employee.View", "Leave.Approve", "Attendance.Approve"]),
                        new("admin-hr-supervisors-groups", "Supervisors Management & Operational Groups", "/admin/hr/supervisors/management", "Assign department/team supervisors and manage operational groups and group types.",
                            ["Organisation.Edit", "Employee.Edit", "Employee.View"]),
                        new("admin-hr-items-custody", "Item Catalogue, Staff Items & Asset Custody", "/admin/hr/items/item-catalogue", "Manage item categories, inventory stock, staff item requisitions, asset custody, maintenance, and clearance forms.",
                            ["Asset.View", "Asset.Inventory.Manage", "Asset.Issuance.Initiate", "Asset.HR.Approve", "Asset.Acknowledge", "Asset.Return.Inspect", "Asset.Service.Manage", "Asset.Technician.Execute", "Asset.Upgrade.Authorize", "Asset.Clearance.Authorize", "Asset.Audit.View", "Asset.ExportRestrictedPdf"]),
                        new("admin-hr-events-viuga", "Organisation Events, CSC Viuga & Others", "/admin/hr/events", "Manage organisation events, Civil Service Commission Viuga sync, and miscellaneous HR lookups.",
                            ["OrganisationEvent.Manage", "OrganisationEvent.Publish", "Admin"])
                    ]),
                new(
                    "admin-settings-user-org",
                    "Settings -> User & Organisation",
                    "/admin/settings/user",
                    "bi-gear",
                    "User accounts, Admin Portal access workflows, account permissions, and 9-tab Organisation Workspace settings.",
                    [
                        new("admin-settings-user-accounts", "User -> Manage Account, Admin Portal Access & Manage Permissions", "/admin/settings/user/manage-account", "Edit user accounts, approve/revoke Admin Portal access grants, and assign user roles/direct permissions.",
                            ["User.Manage", "Users.Edit", "UserOrganisations.Admin.View", "Users.Roles.View", "Users.Roles.Edit", "Admin"]),
                        new("admin-settings-communications", "Communication Hub", "/admin/settings/communications", "Manage tenant integrations, destinations, automation and delivery. Approved Admin Portal access is mandatory.",
                            ["Communication.Manage", "Admin"]),
                        new("admin-settings-org-workspace", "Organisation -> Manage, Document Centre, Structure Types, Calendar, Locations, Types & Organisation Permissions", "/admin/settings/org/manage", "Configure organisation profile, structure hierarchy, calendars, locations, and tenant permission ceiling.",
                            ["Organisation.Edit", "UserOrganisations.View", "UserOrganisations.Roles.Edit", "Document.Configure", "Super.Admin.Manage"])
                    ]),
                new(
                    "admin-settings-leaves-config",
                    "Settings -> Leaves, Request Types & Configurations",
                    "/admin/settings/leaves",
                    "bi-sliders",
                    "Work handover rules, leave sets, leave accrual, carry-forward, leave types/policies, work types, approval flows, and job types.",
                    [
                        new("admin-settings-leaves", "Leaves -> Handover Rules, Leave Sets, Accrual by Job Type, Carry Forward, Configurable, Leave Types & Leave Policy", "/admin/settings/leaves/work-handover-rules", "Configure tenant handover rules, leave sets, monthly accrual rates, carry-forward processing, and leave policies.",
                            ["Leave.Edit", "Leave.CarryForward.View", "Leave.CarryForward.Process", "Organisation.Edit", "Admin"]),
                        new("admin-settings-config", "Configurations -> Request Types, Work Types, Request Approval Flows & Job Type Configuration", "/admin/settings/config/work-types", "Manage request types, work types, multi-stage approval flows, job types, and overtime policies.",
                            ["WorkTypes.Manage", "Job.WorkType.Assign", "Job.WorkType.ViewAssigned", "Organisation.Edit", "Admin", "Developer"])
                    ]),
                new(
                    "admin-settings-roles-positions-workplan",
                    "Settings -> Roles & Permissions, Positions, Work Planning & Projects",
                    "/admin/settings/roles/roles",
                    "bi-shield-lock",
                    "Roles & Area Map, Permission definitions, Staff Attribute Access & Scope Simulation, Pay Frameworks/Positions, Work Planning, and Projects.",
                    [
                        new("admin-settings-roles-permissions", "Roles & Permissions -> Permissions, Roles & Area Map", "/admin/settings/roles/roles", "Manage permission definitions, user group roles, and interactive Site Map Area permission assignments.",
                            ["Roles.View", "Roles.Add", "Roles.Edit", "Role.Manage", "UserOrganisation.Groups.View", "UserOrganisation.Groups.Create", "UserOrganisation.Groups.Edit"]),
                        new("admin-settings-staff-access", "Roles & Permissions -> Staff Attribute Access & Scope Simulation", "/admin/settings/roles/staff-access", "Manage hierarchical Organisational Access Scopes, Effective Access Preview simulation, ABAC classification policies, and scope audit history.",
                            ["Staff.AccessPolicy.Manage", "Staff.AccessScope.Manage", "Staff.AccessScope.Simulate", "Employee.PrivateRecord.View", "Employee.Salary.View", "Employee.Identification.View", "Employee.Disciplinary.View", "Employee.Medical.View"]),
                        new("admin-settings-positions", "Positions -> Overview, Pay Frameworks, Ranks, Classifications, Positions & Position Allowances", "/admin/settings/positions/overview", "Configure government Job Matrices, pay steps, ranks, classifications, positions, and position allowances.",
                            ["Position.Details", "Positions.Configure", "Staff.Positions.Manage"]),
                        new("admin-settings-workplanning", "Work Planning -> Working Hours, Duty Roster, Templates, Plans & Segments", "/admin/settings/workplaning/working-hours", "Configure weekly hours, work templates, shift/break segments, and daily employee work plans.",
                            ["Attendance.WorkingHours.Configure", "Attendance.Schedule.Amend", "Schedule.Attach"]),
                        new("admin-settings-projects", "Projects & Tasks Management Hub", "/admin/settings/projects", "Enterprise management for department projects, milestones, deliverables, and task allocations.",
                            ["Projects.Manage", "Organisation.Edit", "Employee.View"])
                    ]),
                new(
                    "admin-leadership-loans-intranet",
                    "Leadership, Goods Loans, Intranet & Recruitment",
                    "/admin/settings/leadership",
                    "bi-award",
                    "MDS leadership appointments & authority audit, staff goods loan administration, intranet administration, and recruitment.",
                    [
                        new("admin-leadership-admin", "Leadership Administration & Leadership Records / Audit", "/admin/settings/leadership", "Manage appointment types, appointments, organisation responsibilities, authority grants, delegations, attendance policies, and audit logs.",
                            ["Leadership.Appointment.Manage", "Super.Admin.Manage"]),
                        new("admin-goods-loans-admin", "Goods Loans Administration (Approvals, Register, Stores, Agreements, Policy, Settlements & Reports)", "/admin/goods-loans", "Manage partner merchant stores, agreements, deduction policies, loan approvals, and monthly store settlements.",
                            ["GoodsLoan.Manage", "StaffFinance.Payroll"]),
                        new("admin-intranet-recruitment", "Intranet Administration & Recruitment", "/admin/intranet", "Manage intranet posts, announcements, new joiners, categories, and recruitment intake.",
                            ["OrganisationEvent.Manage", "OrganisationEvent.Publish", "Intranet.Announcement.View", "Employee.Create", "Staff.Add.Admin"])
                    ])
            ]),

        // =====================================================================
        // CONTEXT 3: DEVELOPER (DEVELOPER & PLATFORM GOVERNANCE PORTAL)
        // =====================================================================
        new(
            "Developer",
            "Developer (Platform & Diagnostics Portal)",
            "Platform engineering workspace for request types, state transition rules, approval workflows, error logs, application health, and biometric edge agents.",
            "/developer",
            "bi-code-square",
            [
                new(
                    "dev-workspace",
                    "Developer Workspace",
                    "/developer",
                    "bi-terminal",
                    "Centralized developer tabs for telemetry, error logs, request types, state transitions, notifications, workflows, health, edge agents, and diagnostics.",
                    [
                        new("dev-overview", "Overview & Test Attendance Time Card Toggle", "/developer/overview", "Developer telemetry summary and toggle for the simulated test attendance clock on `/online`.",
                            ["Developer", "Super.Admin.Manage"]),
                        new("dev-error-logs", "Error Logs", "/developer/error-logs", "Inspect captured application exceptions, stack traces, request routes, and timestamps.",
                            ["Developer", "Super.Admin.Manage"]),
                        new("dev-request-types", "Request Types Configuration", "/developer/request-types", "Create and manage system request types, sequence numbering, Dhivehi names, and active flags.",
                            ["Developer", "Super.Admin.Manage", "Admin"]),
                        new("dev-transitions", "Transition States (From State -> To State)", "/developer/transitions", "Configure allowable state transitions per request type and prevent invalid state changes.",
                            ["Developer", "Super.Admin.Manage"]),
                        new("dev-notifications", "Notification Resolution", "/developer/notification-resolution", "Verify workflow notification routing, recipient resolution, and configuration alerts.",
                            ["Developer", "Super.Admin.Manage"]),
                        new("dev-workflows", "Approval Workflows", "/developer/workflows", "Configure multi-stage approval levels and approver user groups by Job Type and Request Type.",
                            ["Developer", "Organisation.Edit", "Admin"]),
                        new("dev-health", "Application Health", "/developer/health", "Live telemetry for Backend API, Payroll.Worker service, page directory, and most visited routes.",
                            ["Developer", "Super.Admin.Manage"]),
                        new("dev-edge-agents", "Client Agents (Attendance Edge Agents & PKI)", "/developer/edge-agents", "Enrol attendance edge agents, manage trusted PKI root CAs, bind biometric devices, and inspect ingestion failures.",
                            ["Edge.Agent.Manage", "Developer", "Super.Admin.Manage"]),
                        new("dev-diagnostics", "System & Diagnostics", "/developer/diagnostics", "Inspect runtime environment configuration, database providers, and system diagnostics.",
                            ["Developer", "Super.Admin.Manage"])
                    ])
            ])
    ];

    private async Task EnsureCanonicalSiteMapCatalogAsync(HrmTeContext db)
    {
        // Only auto-seed into non-empty databases that already have at least one Context or Organisation configured,
        // preserving lightweight unit test fixtures that assert exact counts on minimal seeded tables.
        if (!await db.Contexts.AnyAsync() && !await db.Organisations.AnyAsync())
            return;

        var existingContexts = await db.Contexts.ToListAsync();
        var requiredContexts = new (string Name, string Key, string Description, bool IsDefault)[]
        {
            ("Online", "ONLINE", "Employee Self-Service Portal (/online)", true),
            ("Admin", "ADMIN", "Admin & HR Management Portal (/admin)", false),
            ("Developer", "DEVELOPER", "Developer & Platform Governance Portal (/developer)", false)
        };

        var anyContextAdded = false;
        foreach (var (name, key, desc, isDefault) in requiredContexts)
        {
            if (!existingContexts.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                                           (c.ContextKey != null && c.ContextKey.Equals(key, StringComparison.OrdinalIgnoreCase))))
            {
                var ctx = new Context
                {
                    Name = name,
                    ContextKey = key,
                    Description = desc,
                    IsDefault = isDefault && existingContexts.Count == 0
                };
                db.Contexts.Add(ctx);
                existingContexts.Add(ctx);
                anyContextAdded = true;
            }
        }
        if (anyContextAdded)
            await db.SaveChangesAsync();

        var existingModules = await db.Modules.ToListAsync();
        var requiredModuleNames = CanonicalPermissions.Select(p => p.ModuleName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var anyModuleAdded = false;
        foreach (var modName in requiredModuleNames)
        {
            if (!existingModules.Any(m => m.Name.Equals(modName, StringComparison.OrdinalIgnoreCase)))
            {
                var mod = new Module { Name = modName, IsDhivehiSupported = true };
                db.Modules.Add(mod);
                existingModules.Add(mod);
                anyModuleAdded = true;
            }
        }
        if (anyModuleAdded)
            await db.SaveChangesAsync();

        var existingRoles = await db.Roles.ToListAsync();
        // Only auto-seed missing canonical permissions when the database has standard system roles (> 5 roles)
        // so tiny unit-test fixtures with 3-4 hand-crafted roles are not altered.
        if (existingRoles.Count >= 5)
        {
            var defaultContextId = existingContexts.FirstOrDefault()?.ContextId ?? 1;
            var anyRoleAdded = false;
            foreach (var spec in CanonicalPermissions)
            {
                var existing = existingRoles.FirstOrDefault(r => r.RoleKey.Equals(spec.Key, StringComparison.OrdinalIgnoreCase));
                var contextId = existingContexts
                    .FirstOrDefault(c => c.Name.Equals(spec.ContextName, StringComparison.OrdinalIgnoreCase))?.ContextId
                    ?? defaultContextId;
                var moduleId = existingModules
                    .FirstOrDefault(m => m.Name.Equals(spec.ModuleName, StringComparison.OrdinalIgnoreCase))?.ModuleId;

                if (existing == null)
                {
                    var newRole = new Role
                    {
                        RoleKey = spec.Key,
                        Name = spec.Name,
                        Description = spec.Description,
                        ContextId = contextId,
                        ModuleId = moduleId,
                        IsSystemRole = true
                    };
                    db.Roles.Add(newRole);
                    existingRoles.Add(newRole);
                    anyRoleAdded = true;
                }
                else if (!existing.ModuleId.HasValue && moduleId.HasValue)
                {
                    existing.ModuleId = moduleId;
                    anyRoleAdded = true;
                }
            }
            if (anyRoleAdded)
                await db.SaveChangesAsync();
        }
    }

    private static List<SiteMapContextDto> BuildSiteMapAreaCatalog(
        IReadOnlyList<PermissionDefinitionDto> dbPermissions,
        IReadOnlyList<PermissionSettingOption> dbContexts)
    {
        var permByKey = dbPermissions
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var canonicalByKey = CanonicalPermissions
            .GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var mappedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<SiteMapContextDto>();

        foreach (var ctxSpec in SiteMapHierarchy)
        {
            var ctxDto = new SiteMapContextDto
            {
                ContextKey = ctxSpec.ContextKey,
                Title = ctxSpec.Title,
                Subtitle = ctxSpec.Subtitle,
                BaseRoute = ctxSpec.BaseRoute,
                Icon = ctxSpec.Icon
            };

            foreach (var areaSpec in ctxSpec.Areas)
            {
                var areaDto = new SiteMapMenuAreaDto
                {
                    AreaKey = areaSpec.AreaKey,
                    Title = areaSpec.Title,
                    Route = areaSpec.Route,
                    Icon = areaSpec.Icon,
                    Description = areaSpec.Description
                };

                foreach (var sfSpec in areaSpec.SubFunctions)
                {
                    var sfDto = new SiteMapSubFunctionDto
                    {
                        SubFunctionKey = sfSpec.SubFunctionKey,
                        Title = sfSpec.Title,
                        Route = sfSpec.Route,
                        Description = sfSpec.Description
                    };

                    foreach (var permKey in sfSpec.PermissionKeys.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        mappedKeys.Add(permKey);
                        if (permByKey.TryGetValue(permKey, out var dbPerm))
                        {
                            sfDto.Permissions.Add(new SiteMapPermissionItemDto
                            {
                                Id = dbPerm.Id,
                                Key = dbPerm.Key,
                                Name = dbPerm.Name,
                                Description = dbPerm.Description,
                                ContextName = !string.IsNullOrWhiteSpace(dbPerm.ContextName) ? dbPerm.ContextName : ctxSpec.ContextKey,
                                ModuleName = dbPerm.Group,
                                IsSystem = dbPerm.IsSystem
                            });
                        }
                        else if (canonicalByKey.TryGetValue(permKey, out var canon))
                        {
                            sfDto.Permissions.Add(new SiteMapPermissionItemDto
                            {
                                Id = 0,
                                Key = canon.Key,
                                Name = canon.Name,
                                Description = canon.Description,
                                ContextName = canon.ContextName,
                                ModuleName = canon.ModuleName,
                                IsSystem = true
                            });
                        }
                        else
                        {
                            sfDto.Permissions.Add(new SiteMapPermissionItemDto
                            {
                                Id = 0,
                                Key = permKey,
                                Name = permKey,
                                Description = "",
                                ContextName = ctxSpec.ContextKey,
                                ModuleName = PermissionDefinitionDto.PermissionArea(permKey),
                                IsSystem = true
                            });
                        }
                    }

                    areaDto.SubFunctions.Add(sfDto);
                }

                ctxDto.Areas.Add(areaDto);
            }

            result.Add(ctxDto);
        }

        // Guarantee zero permissions are left unmapped: attach any custom/additional DB permissions
        // to their corresponding Context (Online, Admin, Developer) and Area/Group.
        var unmappedDbPerms = dbPermissions.Where(p => !mappedKeys.Contains(p.Key)).ToList();
        if (unmappedDbPerms.Count > 0)
        {
            foreach (var group in unmappedDbPerms.GroupBy(p => ResolveFallbackContextKey(p, dbContexts), StringComparer.OrdinalIgnoreCase))
            {
                var targetContext = result.FirstOrDefault(c => c.ContextKey.Equals(group.Key, StringComparison.OrdinalIgnoreCase))
                                    ?? result.First(c => c.ContextKey == "Admin");

                var customArea = new SiteMapMenuAreaDto
                {
                    AreaKey = $"{targetContext.ContextKey.ToLowerInvariant()}-custom-permissions",
                    Title = "Additional & Custom Permissions",
                    Route = targetContext.BaseRoute,
                    Icon = "bi-key",
                    Description = "Custom and additional database permissions configured for this context."
                };

                foreach (var moduleGroup in group.GroupBy(p => p.Group, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key))
                {
                    customArea.SubFunctions.Add(new SiteMapSubFunctionDto
                    {
                        SubFunctionKey = $"custom-{targetContext.ContextKey.ToLowerInvariant()}-{moduleGroup.Key.ToLowerInvariant()}",
                        Title = $"{moduleGroup.Key} Permissions",
                        Route = targetContext.BaseRoute,
                        Description = $"Configured permissions under {moduleGroup.Key}.",
                        Permissions = moduleGroup.OrderBy(p => p.Name).Select(p => new SiteMapPermissionItemDto
                        {
                            Id = p.Id,
                            Key = p.Key,
                            Name = p.Name,
                            Description = p.Description,
                            ContextName = !string.IsNullOrWhiteSpace(p.ContextName) ? p.ContextName : targetContext.ContextKey,
                            ModuleName = p.Group,
                            IsSystem = p.IsSystem
                        }).ToList()
                    });
                }

                targetContext.Areas.Add(customArea);
            }
        }

        return result;
    }

    private static string ResolveFallbackContextKey(PermissionDefinitionDto perm, IReadOnlyList<PermissionSettingOption> contexts)
    {
        var ctxName = perm.ContextName;
        if (string.IsNullOrWhiteSpace(ctxName))
            ctxName = contexts.FirstOrDefault(c => c.Id == perm.ContextId)?.Name ?? "";

        if (ctxName.Contains("Online", StringComparison.OrdinalIgnoreCase) ||
            ctxName.Contains("Employee", StringComparison.OrdinalIgnoreCase) ||
            ctxName.Contains("Portal", StringComparison.OrdinalIgnoreCase))
            return "Online";

        if (ctxName.Contains("Dev", StringComparison.OrdinalIgnoreCase) ||
            perm.Key.StartsWith("Developer", StringComparison.OrdinalIgnoreCase) ||
            perm.Key.StartsWith("Edge.", StringComparison.OrdinalIgnoreCase) ||
            perm.Key.StartsWith("Super.Admin", StringComparison.OrdinalIgnoreCase))
            return "Developer";

        return "Admin";
    }
}
