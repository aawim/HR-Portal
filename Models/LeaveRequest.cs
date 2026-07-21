using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LeaveRequest
{
    public int RequestId { get; set; }

    public int LeaveId { get; set; }

    public virtual Leaf Leave { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
