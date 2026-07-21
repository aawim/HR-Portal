using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RecurrenceType
{
    public int RecurrenceTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsValid { get; set; }

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();
}
