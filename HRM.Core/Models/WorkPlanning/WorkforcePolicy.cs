namespace HRM.Models.WorkPlanning;

/// <summary>Organisation-scoped, generic duty classification. Never use job titles as scheduling rules.</summary>
public sealed class DutyType
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Standard";
    public string AttendanceStrategy { get; set; } = "CheckInOut";
    public int? WorkTemplateId { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

/// <summary>Immutable-on-use, effective-dated policy version containing validated typed rule JSON.</summary>
public sealed class WorkforcePolicy
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RulesJson { get; set; } = "{}";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

/// <summary>Effective-dated policy attachment. Null targets define the organisation default.</summary>
public sealed class WorkforcePolicyAssignment
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int WorkforcePolicyId { get; set; }
    public int? JobId { get; set; }
    public int? IndividualId { get; set; }
    public int? DutyTypeId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
    public WorkforcePolicy WorkforcePolicy { get; set; } = null!;
}
