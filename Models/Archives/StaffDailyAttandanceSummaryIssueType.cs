using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffDailyAttandanceSummaryIssueType
{
    public int StaffDailyAttandanceSummaryIssueTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<StaffDailyAttandanceSummaryIssue> StaffDailyAttandanceSummaryIssues { get; set; } = new List<StaffDailyAttandanceSummaryIssue>();
}
