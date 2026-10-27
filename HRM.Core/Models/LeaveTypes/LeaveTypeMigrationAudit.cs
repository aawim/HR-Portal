
namespace HRM.Models.LeaveTypes;

/// <summary>Immutable source snapshots retained by the one-time type consolidation.</summary>
public sealed class LeaveTypeMigrationAudit
{
    public int Id { get; set; }
    public string SourceEntity { get; set; } = "";
    public int SourceId { get; set; }
    public int? LeaveTypeId { get; set; }
    public string SnapshotJson { get; set; } = "";
    public DateTime MigratedAtUtc { get; set; }
}
