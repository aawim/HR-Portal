using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Element
{
    public int ElementId { get; set; }

    public string? Description { get; set; }

    public int? DayOfWeekId { get; set; }

    public int SpecialDayTypeId { get; set; }

    public int? WorkShiftId { get; set; }

    public DateTime? Date { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual DayOfWeek? DayOfWeek { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<ScheduleElement> ScheduleElements { get; set; } = new List<ScheduleElement>();

    public virtual SpecialDayType SpecialDayType { get; set; } = null!;

    public virtual WorkShift? WorkShift { get; set; }

    public virtual ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();
}
