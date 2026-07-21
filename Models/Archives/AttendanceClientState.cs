using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttendanceClientState
{
    public int AttendanceClientStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<AttendanceClientInstance> AttendanceClientInstances { get; set; } = new List<AttendanceClientInstance>();
}
