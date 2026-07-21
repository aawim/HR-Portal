using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobState
{
    public int JobStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}
