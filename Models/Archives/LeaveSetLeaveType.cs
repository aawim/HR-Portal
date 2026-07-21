using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveSetLeaveType
{
    public int LeaveSetLeaveTypeId { get; set; }

    public int LeaveSetId { get; set; }

    public int LeaveTypeId { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual LeaveSet LeaveSet { get; set; } = null!;

    public virtual LeaveType LeaveType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
