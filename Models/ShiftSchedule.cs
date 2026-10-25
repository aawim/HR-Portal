using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ShiftSchedule
{
    public int ScheduleId { get; set; }

    public virtual Schedule Schedule { get; set; } = null!;

    public virtual ICollection<ShiftScheduleDay> ShiftScheduleDays { get; set; } = new List<ShiftScheduleDay>();
}
