using HRM.Models.Archives;
using HRM.Models.WorkPlanning;

namespace HRM.Models;

public sealed class StaffGroup
{
    public int StaffGroupId { get; set; }
    public int OrganisationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }

    // Schedule override
    public int? WorkTemplateId { get; set; }
    public string ScheduleMode { get; set; } = "Standard";
    public int WorkingDaysMask { get; set; } = 31;

    // Work Type / clocking override
    public int? WorkTypeId { get; set; }

    // Overtime override
    public bool? OvertimeEnabled { get; set; }
    public decimal? MaximumDailyOvertimeHours { get; set; }
    public decimal? MaximumWeeklyOvertimeHours { get; set; }
    public decimal? OvertimeRateMultiplier { get; set; }

    // Leave policy override
    public int? LeaveSetId { get; set; }

    // Admin portal access grant
    public bool AdminPortalAccessEnabled { get; set; }

    // Lifecycle
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDissolved { get; set; }
    public DateTime? DissolvedAtUtc { get; set; }
    public int? DissolvedByUserId { get; set; }
    public string? DissolveReason { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int OperationLogId { get; set; }

    public WorkTemplate? WorkTemplate { get; set; }
    public WorkType? WorkType { get; set; }
    public LeaveSet? LeaveSet { get; set; }
    public ICollection<StaffGroupMember> Members { get; set; } = new List<StaffGroupMember>();
}

public sealed class StaffGroupMember
{
    public int StaffGroupMemberId { get; set; }
    public int StaffGroupId { get; set; }
    public int OrganisationId { get; set; }
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public DateTime? LeftAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HadDirectAdminPortalAccess { get; set; }
    public bool GrantedAdminPortalAccess { get; set; }
    public int AddedByUserId { get; set; }
    public int? RemovedByUserId { get; set; }
    public int OperationLogId { get; set; }

    public StaffGroup StaffGroup { get; set; } = null!;
}
