using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffDailyAttandanceSummaryIssue
{
    public int StaffDailyAttandanceSummaryIssueId { get; set; }

    public int StaffDailyAttendanceSummeryId { get; set; }

    public int StaffDailyAttandanceSummaryIssueTypeId { get; set; }

    public int ReferancePrimaryKey { get; set; }

    public string? Comments { get; set; }

    public DateTime? CheckedDate { get; set; }

    public int? PayrollCycleProcessingStateId { get; set; }

    public virtual PayrollCycleProcessingState? PayrollCycleProcessingState { get; set; }

    public virtual StaffDailyAttandanceSummaryIssueType StaffDailyAttandanceSummaryIssueType { get; set; } = null!;

    public virtual StaffDailyAttendanceSummery StaffDailyAttendanceSummery { get; set; } = null!;
}
