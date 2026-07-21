using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class GroupSchedule
{
    public int GroupScheduleId { get; set; }

    public int ScheduleId { get; set; }

    public int GroupId { get; set; }

    public int OperationLogId { get; set; }

    public bool IsActive { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}
