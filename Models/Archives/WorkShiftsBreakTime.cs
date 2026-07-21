using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class WorkShiftsBreakTime
{
    public int WorkShiftsBreakTimesId { get; set; }

    public int WorkShiftId { get; set; }

    public int BreakTimeId { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual BreakTime BreakTime { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual WorkShift WorkShift { get; set; } = null!;
}
