using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OwnerType
{
    public int OwnerTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<AttachedBreakTime> AttachedBreakTimes { get; set; } = new List<AttachedBreakTime>();

    public virtual ICollection<ScheduleOwner> ScheduleOwners { get; set; } = new List<ScheduleOwner>();
}
