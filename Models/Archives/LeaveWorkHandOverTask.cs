using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveWorkHandOverTask
{
    public int LeaveId { get; set; }

    public string TaskDetails { get; set; } = null!;

    public virtual Leaf Leave { get; set; } = null!;
}
