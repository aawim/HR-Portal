using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class CalendarType
{
    public int CalendarTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public int OperationLogId { get; set; }

    public virtual ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();

    public virtual ICollection<Month> Months { get; set; } = new List<Month>();

    public virtual OperationLog OperationLog { get; set; } = null!;
}
