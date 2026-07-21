using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ShiftScheduleDay
{
    public int ShiftScheduleDayId { get; set; }

    public int ScheduleId { get; set; }

    public int? DayNumber { get; set; }

    public int? WorkShiftId { get; set; }

    public int SpecialDayTypeId { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public DateTime? Date { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ShiftSchedule Schedule { get; set; } = null!;

    public virtual SpecialDayType SpecialDayType { get; set; } = null!;

    public virtual WorkShift? WorkShift { get; set; }
}
