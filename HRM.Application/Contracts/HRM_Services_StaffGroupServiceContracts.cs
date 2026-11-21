namespace HRM.Services.Contracts;

public abstract class StaffGroupServiceContracts
{
    public sealed record StaffGroupOptionDto(int Id, string Name, string? Extra = null);

    public sealed class StaffGroupStaffCandidateDto
    {
        public int IndividualId { get; set; }
        public int JobId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeNumber { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string JobTypeName { get; set; } = string.Empty;
        public string JobTitleName { get; set; } = string.Empty;
        public int? ActiveStaffGroupId { get; set; }
        public string? ActiveStaffGroupName { get; set; }
        public bool HasAdminPortalAccess { get; set; }
    }

    public sealed class StaffGroupMemberDto
    {
        public int StaffGroupMemberId { get; set; }
        public int StaffGroupId { get; set; }
        public int IndividualId { get; set; }
        public int JobId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeNumber { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string JobTypeName { get; set; } = string.Empty;
        public string JobTitleName { get; set; } = string.Empty;
        public DateTime JoinedAtUtc { get; set; }
        public DateTime? LeftAtUtc { get; set; }
        public bool IsActive { get; set; }
        public bool HadDirectAdminPortalAccess { get; set; }
        public bool GrantedAdminPortalAccess { get; set; }
        public bool CurrentAdminPortalAccess { get; set; }
    }

    public sealed class StaffGroupDto
    {
        public int StaffGroupId { get; set; }
        public int OrganisationId { get; set; }
        public string OrganisationName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public int? WorkTemplateId { get; set; }
        public string? WorkTemplateName { get; set; }
        public string ScheduleMode { get; set; } = "Standard";
        public int WorkingDaysMask { get; set; } = 31;
        public int? WorkTypeId { get; set; }
        public string? WorkTypeName { get; set; }
        public bool? OvertimeEnabled { get; set; }
        public decimal? MaximumDailyOvertimeHours { get; set; }
        public decimal? MaximumWeeklyOvertimeHours { get; set; }
        public decimal? OvertimeRateMultiplier { get; set; }
        public int? LeaveSetId { get; set; }
        public string? LeaveSetName { get; set; }
        public bool AdminPortalAccessEnabled { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public bool IsDissolved { get; set; }
        public DateTime? DissolvedAtUtc { get; set; }
        public string? DissolveReason { get; set; }
        public int ActiveMemberCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class StaffGroupDetailDto
    {
        public StaffGroupDto Group { get; set; } = new();
        public List<StaffGroupMemberDto> Members { get; set; } = [];
    }

    public sealed class StaffGroupWorkspaceDto
    {
        public string OrganisationName { get; set; } = string.Empty;
        public List<StaffGroupDto> Groups { get; set; } = [];
        public List<StaffGroupOptionDto> WorkTemplates { get; set; } = [];
        public List<StaffGroupOptionDto> WorkTypes { get; set; } = [];
        public List<StaffGroupOptionDto> LeaveSets { get; set; } = [];
        public List<StaffGroupStaffCandidateDto> StaffCandidates { get; set; } = [];
    }

    public sealed class StaffGroupSaveRequest
    {
        public int StaffGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public int? WorkTemplateId { get; set; }
        public string ScheduleMode { get; set; } = "Standard";
        public int WorkingDaysMask { get; set; } = 31;
        public int? WorkTypeId { get; set; }
        public bool? OvertimeEnabled { get; set; }
        public decimal? MaximumDailyOvertimeHours { get; set; }
        public decimal? MaximumWeeklyOvertimeHours { get; set; }
        public decimal? OvertimeRateMultiplier { get; set; }
        public int? LeaveSetId { get; set; }
        public bool AdminPortalAccessEnabled { get; set; }
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;
        public DateTime? EffectiveTo { get; set; }
    }

    public sealed class StaffGroupMembersRequest
    {
        public int StaffGroupId { get; set; }
        public List<int> IndividualIds { get; set; } = [];
    }

    public interface IOperations
    {
        Task<StaffGroupWorkspaceDto> LoadAsync();
        Task<StaffGroupDetailDto> GetDetailAsync(int staffGroupId);
        Task<StaffGroupDto> SaveGroupAsync(StaffGroupSaveRequest request);
        Task AddMembersAsync(StaffGroupMembersRequest request);
        Task RemoveMemberAsync(int staffGroupId, int individualId);
        Task SetAdminPortalAccessAsync(int staffGroupId, bool enabled);
        Task DissolveGroupAsync(int staffGroupId, string? reason);
    }
}
