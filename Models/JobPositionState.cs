using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobPositionState
{
    public int JobPositionStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();
}
