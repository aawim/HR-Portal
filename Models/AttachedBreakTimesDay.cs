using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttachedBreakTimesDay
{
    public int AttachedBreakTimesDayId { get; set; }

    public int AttachedBreakTimeId { get; set; }

    public int DayOfWeekId { get; set; }

    public int BreakTimeId { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual AttachedBreakTime AttachedBreakTime { get; set; } = null!;

    public virtual BreakTime BreakTime { get; set; } = null!;

    public virtual DayOfWeek DayOfWeek { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
