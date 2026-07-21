using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class CalendarSchedule
{
    public int CalendarScheduleId { get; set; }

    public int ScheduleId { get; set; }

    public int CalendarId { get; set; }

    public virtual Calendar Calendar { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}
