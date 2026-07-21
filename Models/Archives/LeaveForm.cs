using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveForm
{
    public int LeaveId { get; set; }

    public DateTime ReportedDateTime { get; set; }

    public int OperationLogId { get; set; }

    public virtual Leaf Leave { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
