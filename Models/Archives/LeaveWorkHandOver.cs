using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveWorkHandOver
{
    public int LeaveWorkHandOverId { get; set; }

    public int LeaveId { get; set; }

    public int JobId { get; set; }

    public bool IsValid { get; set; }
    public string? TaskDetails { get; set; }
    public string Decision { get; set; } = "Pending";
    public string? Comments { get; set; }
    public DateTime? DecisionAt { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Leaf Leave { get; set; } = null!;
}
