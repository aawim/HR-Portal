using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class CalendarMonth
{
    public int CalendarMonthId { get; set; }

    public int MonthId { get; set; }

    public DateTime StartDate { get; set; }

    public int NumberOfDays { get; set; }

    public int OperationLogId { get; set; }

    public int CalendarInstanceId { get; set; }

    public virtual CalendarInstance CalendarInstance { get; set; } = null!;

    public virtual Month Month { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
