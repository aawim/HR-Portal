using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SpecialDayShift
{
    public int SpecialDayShiftId { get; set; }

    public int WorkShiftId { get; set; }

    public int SpecialDayId { get; set; }

    public bool IsLinked { get; set; }

    public virtual SpecialDay SpecialDay { get; set; } = null!;
}
