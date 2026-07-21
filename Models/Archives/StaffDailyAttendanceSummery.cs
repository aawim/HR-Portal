using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffDailyAttendanceSummery
{
    public int StaffDailyAttendanceSummeryId { get; set; }

    public int? StaffSalaryId { get; set; }

    public DateOnly? Date { get; set; }

    public bool SalaryToPay { get; set; }

    public TimeOnly? ReportingTime { get; set; }

    public TimeOnly? SignedInTime { get; set; }

    public TimeOnly? LateDuration { get; set; }

    public int? OperationLogId { get; set; }

    public string? SalaryToPayReason { get; set; }

    public bool? IsAbsent { get; set; }

    public decimal? AbsentFine { get; set; }

    public decimal? LateFine { get; set; }

    public decimal? SalaryAmount { get; set; }

    public int? JobPositionId { get; set; }

    public TimeOnly? Otduration { get; set; }

    public decimal? Otamount { get; set; }

    public TimeOnly? DurationToWork { get; set; }

    public bool? IsProcessed { get; set; }

    public string? Comments { get; set; }

    public DateTime? LastUpdatedDate { get; set; }

    public bool? IsWorkingDay { get; set; }

    public TimeOnly? BreakOut { get; set; }

    public TimeOnly? BreakIn { get; set; }

    public TimeOnly? ScheduledOut { get; set; }

    public TimeOnly? ClockOut { get; set; }

    public virtual JobPosition? JobPosition { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<StaffDailyAttandanceSummaryIssue> StaffDailyAttandanceSummaryIssues { get; set; } = new List<StaffDailyAttandanceSummaryIssue>();

    public virtual StaffSalary? StaffSalary { get; set; }
}
