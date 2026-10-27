
namespace HRM.Models.LeaveTypes;

public sealed class LeaveHandoverRule
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = "";
    public string Scope { get; set; } = "Organisation";
    public int? OrganisationStructureId { get; set; }
    public int? TeamId { get; set; }
    public int? JobTypeId { get; set; }
    public bool AllLeaveTypes { get; set; } = true;
    public string Mode { get; set; } = "Required";
    public int AfterDays { get; set; } = 2;
    public int Priority { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.AddHours(5).Date;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public int? OperationLogId { get; set; }
    public ICollection<LeaveHandoverRuleType> LeaveTypes { get; set; } = new List<LeaveHandoverRuleType>();
}

public sealed class LeaveHandoverRuleType
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public int LeaveTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public LeaveHandoverRule Rule { get; set; } = null!;
}
