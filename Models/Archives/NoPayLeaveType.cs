using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class NoPayLeaveType
{
    public int NoPayLeaveTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<NoPayLeaf> NoPayLeaves { get; set; } = new List<NoPayLeaf>();
}
