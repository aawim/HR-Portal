namespace HRM.AuditLogs.Models;

public sealed class AuditEntityRule
{
    public string EntityName { get; set; } = string.Empty;
    public string Module { get; set; } = "General";
    public bool IsMandatory { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public string? OrganisationIdProperty { get; set; }
    public string? DivisionIdProperty { get; set; }
    public HashSet<string> ExcludedProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RedactedProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AuditLogPolicy
{
    /// <summary>
    /// When true, only entities listed in AuditedEntities are automatically captured by the interceptor.
    /// When false, all entities except ExcludedEntities are captured.
    /// </summary>
    public bool AuditConfiguredEntitiesOnly { get; set; } = true;

    public Dictionary<string, AuditEntityRule> AuditedEntities { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ExcludedEntities { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AuditLog),
        nameof(AuditLogDetail),
        "OperationLog",
        "ErrorLog",
        "AuditEntry"
    };

    public HashSet<string> GlobalExcludedProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "RowVersion",
        "Timestamp",
        "ConcurrencyStamp",
        "UpdatedAtUtc",
        "LastModifiedUtc",
        "ModifiedDate"
    };

    /// <summary>
    /// Properties always redacted at persistence time (never stored in plaintext).
    /// </summary>
    public HashSet<string> SensitivePropertyPatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "SecurityStamp",
        "Secret",
        "ClientSecret",
        "AccessToken",
        "RefreshToken",
        "Token",
        "ApiKey",
        "PrivateKey",
        "Pin",
        "Salt"
    };

    /// <summary>
    /// Properties stored for authorised compliance viewing, but masked at query time
    /// when the requesting actor does not have CanViewConfidentialValues.
    /// </summary>
    public HashSet<string> ConfidentialPropertyPatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "AccountNumber",
        "Iban",
        "NationalId",
        "IdNumber",
        "PassportNumber",
        "BasicSalary",
        "GrossSalary",
        "NetSalary",
        "GrossPay",
        "NetPay",
        "Salary",
        "TaxAmount",
        "PensionAmount"
    };

    public bool ShouldAuditEntity(string entityName, out AuditEntityRule? rule)
    {
        rule = null;
        if (string.IsNullOrWhiteSpace(entityName) || ExcludedEntities.Contains(entityName))
            return false;

        if (AuditedEntities.TryGetValue(entityName, out var configured))
        {
            rule = configured;
            return configured.IsEnabled;
        }

        return !AuditConfiguredEntitiesOnly;
    }

    public static AuditLogPolicy CreateDefault()
    {
        var policy = new AuditLogPolicy();

        void Add(string entity, string module, string? orgProp = null, string? divProp = null, bool mandatory = true)
        {
            policy.AuditedEntities[entity] = new AuditEntityRule
            {
                EntityName = entity,
                Module = module,
                IsMandatory = mandatory,
                OrganisationIdProperty = orgProp,
                DivisionIdProperty = divProp
            };
        }

        // 1. Employee records
        Add("Individual", "Employees");
        Add("Staff", "Employees");
        Add("Job", "Employees", "OrganisationID", "OrganisationStructureId");
        Add("StaffBankAccount", "Employees");
        Add("StaffEmergencyContact", "Employees");

        // 2. Salary and position changes
        Add("EmployeeCompensation", "Compensation", "OrganisationId");
        Add("EmployeePayStepHistory", "Compensation", "OrganisationId");
        Add("Position", "Positions", "OrganisationBusinessEntityId", "OrganisationStructureId");
        Add("JobPosition", "Positions");
        Add("PositionMatrix", "Positions", "OrganisationBusinessEntityId");
        Add("StaffPositionChangeRequest", "Positions", "OrganisationId");

        // 3. Attendance manual corrections & schedules
        Add("AttendanceManualOverride", "Attendance", "OrganisationId");
        Add("AttendanceTimeChangeRequest", "Attendance", "OrganisationId");
        Add("JobWorkTemplateAssignment", "WorkPlanning");
        Add("ScopeScheduleAssignment", "WorkPlanning", "OrganisationId", "OrganisationStructureId");
        Add("StaffGroup", "StaffGroups", "OrganisationId");
        Add("StaffGroupMember", "StaffGroups", "OrganisationId");

        // 4. Payroll & adjustments
        Add("PayrollRun", "Payroll", "OrganisationId");
        Add("PayrollItem", "Payroll");
        Add("PayrollAdjustment", "Payroll", "OrganisationId");
        Add("CalculationRule", "Payroll", "OrganisationId");

        // 5 & 6 & 7. Permissions, roles, organisation access
        Add("User", "AccessManagement", "UserOrganisationId");
        Add("UserRole", "AccessManagement");
        Add("UserPermission", "AccessManagement");
        Add("RolePermission", "AccessManagement");
        Add("UserOrganisationAccess", "AccessManagement", "OrganisationId");
        Add("UserGroupMember", "AccessManagement");
        Add("AdminAccessGrantRequest", "AccessManagement", "OrganisationId");

        // 8. Staff transfers
        Add("StaffTransfer", "Transfers", "OrganisationId");

        // 9. Recruitment
        Add("RecruitmentVacancy", "Recruitment", "OrganisationId");
        Add("RecruitmentApplication", "Recruitment", "OrganisationId");
        Add("RecruitmentOffer", "Recruitment", "OrganisationId");

        // 10. Important configuration
        Add("JobTypeOvertimeRule", "Configuration", "OrganisationId");
        Add("OrganisationCalendarException", "Configuration", "OrganisationId");
        Add("GoodsLoan", "GoodsLoans", "OrganisationBusinessEntityID");
        Add("GoodsLoanDeductionPolicy", "GoodsLoans", "OrganisationBusinessEntityID");

        return policy;
    }
}
