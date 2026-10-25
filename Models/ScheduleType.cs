using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ScheduleType
{
    public int ScheduleTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
