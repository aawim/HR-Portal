using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class WorkingDayShift
{
    public int WorkingDayShiftId { get; set; }

    public int WorkShiftId { get; set; }

    public int WorkingDayId { get; set; }

    public bool IsLinked { get; set; }

    public virtual WorkingDay WorkingDay { get; set; } = null!;
}
