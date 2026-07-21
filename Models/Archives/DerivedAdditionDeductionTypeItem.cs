using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DerivedAdditionDeductionTypeItem
{
    public int DerivedAdditionDeductionTypeItemId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int Type { get; set; }

    public virtual ICollection<DerivedAdditionDeductionType> DerivedAdditionDeductionTypes { get; set; } = new List<DerivedAdditionDeductionType>();
}
