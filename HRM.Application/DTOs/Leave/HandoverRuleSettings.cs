using System.Text.Json;
namespace HRM.DTOs.Leave;
public sealed class HandoverRuleEdit
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Scope { get; set; } = "Organisation";
    public int? OrganisationStructureId { get; set; }
    public int? TeamId { get; set; }
    public int? JobTypeId { get; set; }
    public bool AllLeaveTypes { get; set; } = true;
    public List<int> LeaveTypeIds { get; set; } = [];
    public string Mode { get; set; } = "Required";
    public int AfterDays { get; set; } = 2;
    public int Priority { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.AddHours(5).Date;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed record HandoverOption(int Id,string Name);
public sealed record HandoverRuleSettingsData(int OrganisationId,string Organisation,List<HandoverRuleEdit> Rules,List<HandoverOption> Types,List<HandoverOption> JobTypes,List<HandoverOption> Structures,List<HandoverOption> Teams);
public sealed record EffectiveHandoverRule(int? RuleId,int? Version,string Mode,int? AfterDays,string Source)
{
    public bool Requires(int days) => Mode == "Required" && AfterDays.HasValue && days > AfterDays;
    public string Snapshot(int days) => JsonSerializer.Serialize(new { RuleId,Version,Mode,AfterDays,Source,CountedLeaveDays=days,Required=Requires(days) });
}
