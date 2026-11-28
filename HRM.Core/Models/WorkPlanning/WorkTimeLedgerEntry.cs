namespace HRM.Models.WorkPlanning;
/// <summary>Derived, versioned work-time recognition; never replaces attendance evidence.</summary>
public sealed class WorkTimeLedgerEntry
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public int? WorkPlanSegmentId { get; set; }
    public long? DutyAssignmentId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string TimeCategory { get; set; } = string.Empty;
    public int ActualMinutes { get; set; }
    public int CreditedMinutes { get; set; }
    public int? WorkforcePolicyId { get; set; }
    public int? WorkforcePolicyVersion { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string Status { get; set; } = "Calculated";
    public string? PayrollExportReference { get; set; }
    public DateTime? PayrollExportedAtUtc { get; set; }
    public long? CorrectsLedgerEntryId { get; set; }
    public bool IsPayrollFinalized { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int OperationLogId { get; set; }
}
