using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttendanceDependentType
{
    public int AttendanceDependentTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsValid { get; set; }

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();
}
