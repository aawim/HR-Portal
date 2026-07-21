using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LeaveState
{
    public int LeaveStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<Leaf> Leaves { get; set; } = new List<Leaf>();
}
