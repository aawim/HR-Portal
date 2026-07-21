using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ScheduleElement
{
    public int ScheduleElementsId { get; set; }

    public int ScheduleId { get; set; }

    public int ElementId { get; set; }

    public int OperationLogId { get; set; }

    public virtual Element Element { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}
