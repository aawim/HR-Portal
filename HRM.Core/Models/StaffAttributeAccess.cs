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
