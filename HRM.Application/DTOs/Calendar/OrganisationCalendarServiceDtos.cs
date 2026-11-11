using HRM.DTOs.Calendar;
using HRM.DTOs.Leave;

namespace HRM.DTOs.Calendar;

public sealed class EffectiveWorkdayResolutionDto
{
    public DateTime WorkDate { get; set; }
    public DayOfWeek NormalWeekday { get; set; }
    public bool IsWorkingDay { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsSpecialWorkingDay { get; set; }
    public bool AttendanceRequired { get; set; }
    public bool LeaveChargeable { get; set; }
    public TimeSpan? NormalStartTime { get; set; }
    public TimeSpan? NormalEndTime { get; set; }
    public TimeSpan? EffectiveStartTime { get; set; }
    public TimeSpan? EffectiveEndTime { get; set; }
    public string StatusDescription { get; set; } = "Normal Working Day";
    public string OverrideSource { get; set; } = "None";
    public int? OrganisationEventId { get; set; }
    public string? EventTitle { get; set; }
    public List<OrganisationCalendarExceptionSegmentDto> Segments { get; set; } = [];
}

public sealed class OrganisationCalendarExceptionSegmentDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public sealed class OrganisationCalendarDaySummaryDto
{
    public DateTime Date { get; set; }
    public EffectiveWorkdayResolutionDto WorkdayResolution { get; set; } = new();
    public List<OrganisationEventDto> Events { get; set; } = [];
    public int ApprovedLeavesCount { get; set; }
    public int LeavePlansCount { get; set; }
    public int PendingLeavesCount { get; set; }
    public List<LeaveCalendarEntry> Leaves { get; set; } = [];
}

public sealed class SupervisorTeamOptionDto
{
    public int TeamId { get; set; }
    public string Name { get; set; } = "";
    public int MemberCount { get; set; }
}

public sealed class SupervisorCalendarScheduleItemDto
{
    public int IndividualId { get; set; }
    public string StaffName { get; set; } = "";
    public DateTime Date { get; set; }
    public string ScheduleType { get; set; } = "Official"; // Official, Shift, Duty, Leave, Plan, Task, Event
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string Details { get; set; } = "";
    public bool HasConflict { get; set; }
    public string? ConflictDescription { get; set; }
}

public sealed class SupervisorCalendarTaskDto
{
    public int SupervisorCalendarTaskId { get; set; }
    public int? TeamId { get; set; }
    public string? TeamName { get; set; }
    public int OwnerIndividualId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string TaskCategory { get; set; } = "General"; // General, Payroll, Evaluation, Attendance, LeaveApproval
    public DateTime? StartDateTime { get; set; }
    public DateTime DueDateTime { get; set; }
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    public string Status { get; set; } = "NotStarted"; // NotStarted, InProgress, Completed, Cancelled
    public int? ReminderMinutes { get; set; }
    public string? SourceModule { get; set; }
    public int? SourceRecordId { get; set; }
    public List<int> AssigneeIndividualIds { get; set; } = [];
}

public sealed class LeaveImpactAssessmentDto
{
    public int CalendarImpactAssessmentId { get; set; }
    public int OrganisationCalendarExceptionId { get; set; }
    public string SourceRecordType { get; set; } = "Leave";
    public int SourceRecordId { get; set; }
    public int IndividualId { get; set; }
    public string StaffName { get; set; } = "";
    public string AssessmentType { get; set; } = "LeaveDuration";
    public string? PreviousValue { get; set; }
    public string? ProposedValue { get; set; }
    public string? ImpactDetails { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsLockedPeriod { get; set; }
    public DateTime AssessedDate { get; set; }
}
