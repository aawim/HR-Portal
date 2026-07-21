using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DerivedOnType
{
    public int DerivedOnTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool IsValid { get; set; }

    public virtual ICollection<DeductionType> DeductionTypes { get; set; } = new List<DeductionType>();

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();
}
