using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class BusinessEntitySchedule
{
    public int BusinessEntityScheduleId { get; set; }

    public int ScheduleId { get; set; }

    public int BusinessEntityId { get; set; }

    public bool IsLinked { get; set; }

    public int OperationLogId { get; set; }

    public bool IsActive { get; set; }

    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}
