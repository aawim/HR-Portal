using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class GroupLeaveSet
{
    public int GroupLeaveSetId { get; set; }

    public int LeaveSetId { get; set; }

    public int GroupId { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual LeaveSet LeaveSet { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
