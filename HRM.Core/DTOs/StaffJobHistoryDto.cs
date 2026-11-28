using System;
using System.Collections.Generic;

namespace HRM.DTOs;

public sealed record StaffJobHistorySummaryDto(
    int JobId,
    string OrganisationName,
    string DepartmentName,
    string PositionName,
    string JobTypeName,
    string Status,
    bool IsCurrent,
    DateTime JoinedDate,
    DateTime? TerminatedDate,
    int TotalDays,
    int PresentDays,
    int AbsentDays,
    int ScheduledDays,
    decimal LeaveDaysTaken,
    int LeaveRequestsCount,
    int ProjectsCount,
    int TasksCount,
    int CompletedTasksCount,
    List<StaffJobProjectSummaryDto> Projects,
    List<StaffJobTaskSummaryDto> Tasks
);

public sealed record StaffJobProjectSummaryDto(
    int ProjectId,
    string Reference,
    string Name,
    string Role,
    string Status,
    int ProgressPercentage,
    DateTime? AssignedDate,
    DateTime? EndDate
);

public sealed record StaffJobTaskSummaryDto(
    int TaskId,
    string Reference,
    string Title,
    string ProjectName,
    string Status,
    string Priority,
    int Progress,
    DateTime? DueDate,
    DateTime? CompletedDate
);
