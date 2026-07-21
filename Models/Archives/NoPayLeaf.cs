using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class NoPayLeaf
{
    public int LeaveId { get; set; }

    public int NoPayLeaveTypeId { get; set; }

    public int OperationLogId { get; set; }

    public virtual Leaf Leave { get; set; } = null!;

    public virtual NoPayLeaveType NoPayLeaveType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
