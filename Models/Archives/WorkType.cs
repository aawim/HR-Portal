using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class WorkType
{
    public int WorkTypeId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsValid { get; set; }

    public virtual ICollection<AssignedWorkType> AssignedWorkTypes { get; set; } = new List<AssignedWorkType>();
}
