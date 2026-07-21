using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ScheduleOwner
{
    public int ScheduleOwnerId { get; set; }

    public int ScheduleId { get; set; }

    public int OwnerTypeId { get; set; }

    public int ItemId { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual OwnerType OwnerType { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}
