using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollCycleProcessingState
{
    public int PayrollCycleProcessingStateId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<StaffDailyAttandanceSummaryIssue> StaffDailyAttandanceSummaryIssues { get; set; } = new List<StaffDailyAttandanceSummaryIssue>();
}
