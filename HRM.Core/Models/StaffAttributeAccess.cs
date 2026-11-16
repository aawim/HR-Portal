namespace HRM.Models;

/// <summary>Marks an existing position classification as a restricted ABAC resource.</summary>
public sealed class StaffAccessPolicy
{
    public int StaffAccessPolicyId { get; set; }
    public int OrganisationId { get; set; }
    public int PositionClassificationId { get; set; }
    public bool IsRestricted { get; set; } = true;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? OperationLogId { get; set; }
}

/// <summary>Optionally changes a user or role group from normal visibility to explicit-only visibility.</summary>
public sealed class StaffAccessScope
{
    public int StaffAccessScopeId { get; set; }
    public int OrganisationId { get; set; }
    public int? UserId { get; set; }
    public int? UserGroupId { get; set; }
    public bool RequireExplicitGrant { get; set; }
    public bool IsActive { get; set; } = true;
    public int? OperationLogId { get; set; }
}

/// <summary>An ABAC allow/deny rule for a classification or a specific staff member.</summary>
public sealed class StaffAccessGrant
{
    public int StaffAccessGrantId { get; set; }
    public int OrganisationId { get; set; }
    public int? UserId { get; set; }
    public int? UserGroupId { get; set; }
    public int? TargetIndividualId { get; set; }
    public int? PositionClassificationId { get; set; }
    public bool IsAllowed { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAtUtc { get; set; }
    public string? Reason { get; set; }
    public int? OperationLogId { get; set; }
}

/// <summary>Canonical scope types for the Organisational Data Isolation and Access Control Framework.</summary>
public static class AccessScopeTypes
{
    public const string Self = "Self";
    public const string Team = "Team";
    public const string Department = "Department";
    public const string Division = "Division";
    public const string DivisionAndDescendants = "DivisionAndDescendants";
    public const string Organisation = "Organisation";
    public const string Custom = "Custom";

    public static readonly IReadOnlyList<string> All =
    [
        Self,
        Team,
        Department,
        Division,
        DivisionAndDescendants,
        Organisation,
        Custom
    ];

    public static bool IsValid(string? scopeType) =>
        !string.IsNullOrWhiteSpace(scopeType) &&
        All.Any(x => x.Equals(scopeType.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string scopeType) =>
        All.First(x => x.Equals(scopeType.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>Audit action constants for UserAccessScopeAudit.</summary>
public static class AccessScopeAuditActions
{
    public const string Granted = "Granted";
    public const string Updated = "Updated";
    public const string Revoked = "Revoked";
    public const string TemporaryElevation = "TemporaryElevation";
    public const string TransferRevoked = "TransferRevoked";
    public const string Expired = "Expired";
}

/// <summary>
/// Hierarchical organisational access scope assignment (AC_UserAccessScopes).
/// Determines which organisation, division, department, team, or custom unit records a user or role group can access.
/// </summary>
public sealed class UserAccessScope
{
    public int UserAccessScopeId { get; set; }
    public int OrganisationId { get; set; }
    public int? UserId { get; set; }
    public int? UserGroupId { get; set; }
    public int? UnitId { get; set; }
    public int? TeamId { get; set; }
    public string ScopeType { get; set; } = AccessScopeTypes.DivisionAndDescendants;
    public bool IncludeDescendants { get; set; } = true;
    public string? PermissionFilter { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int? GrantedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? RevokedByUserId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public string? Reason { get; set; }
    public int? OperationLogId { get; set; }
}

/// <summary>
/// Immutable audit trail for organisational access scope changes (AC_UserAccessScopeAudits).
/// Records who granted, changed, revoked, or temporarily elevated access, as well as transfer-triggered revocations.
/// </summary>
public sealed class UserAccessScopeAudit
{
    public long UserAccessScopeAuditId { get; set; }
    public int? UserAccessScopeId { get; set; }
    public int OrganisationId { get; set; }
    public int? TargetUserId { get; set; }
    public int? TargetUserGroupId { get; set; }
    public string Action { get; set; } = AccessScopeAuditActions.Granted;
    public string ScopeType { get; set; } = AccessScopeTypes.DivisionAndDescendants;
    public int? UnitId { get; set; }
    public int? TeamId { get; set; }
    public bool IncludeDescendants { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int PerformedByUserId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? Detail { get; set; }
    public int? OperationLogId { get; set; }
}
