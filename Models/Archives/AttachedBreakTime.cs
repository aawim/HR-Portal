using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttachedBreakTime
{
    public int AttachedBreakTimeId { get; set; }

    public int OwnerTypeId { get; set; }

    public int ItemId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int OperationLogId { get; set; }

    public bool IsValid { get; set; }

    public virtual ICollection<AttachedBreakTimesDay> AttachedBreakTimesDays { get; set; } = new List<AttachedBreakTimesDay>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual OwnerType OwnerType { get; set; } = null!;
}
