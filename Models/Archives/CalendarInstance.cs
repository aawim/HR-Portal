using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class CalendarInstance
{
    public int CalendarInstanceId { get; set; }

    public int Year { get; set; }

    public int CalendarId { get; set; }

    public int OperationLogId { get; set; }

    public virtual Calendar Calendar { get; set; } = null!;

    public virtual ICollection<CalendarMonth> CalendarMonths { get; set; } = new List<CalendarMonth>();

    public virtual OperationLog OperationLog { get; set; } = null!;
}
