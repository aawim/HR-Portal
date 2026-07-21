using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Month
{
    public int MonthId { get; set; }

    public string EnglishName { get; set; } = null!;

    public string DhivehiName { get; set; } = null!;

    public bool IsValid { get; set; }

    public int MonthOrder { get; set; }

    public int CalendarTypeId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual ICollection<CalendarMonth> CalendarMonths { get; set; } = new List<CalendarMonth>();

    public virtual CalendarType CalendarType { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }
}
