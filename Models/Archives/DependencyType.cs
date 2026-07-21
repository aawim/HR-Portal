using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DependencyType
{
    public int DependencyTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string IsValid { get; set; } = null!;

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();
}
