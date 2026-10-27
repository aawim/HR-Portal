using System;
using System.Collections.Generic;
namespace HRM.Models.Archives;

public partial class WorkingDay
{
    public int WorkingDayId { get; set; }

    public int DayOfWeekId { get; set; }

    public int CalenderId { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

}
