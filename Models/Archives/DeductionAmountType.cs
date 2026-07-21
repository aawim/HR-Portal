using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DeductionAmountType
{
    public int DeductionAmountTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsValid { get; set; }

    public virtual ICollection<DeductionType> DeductionTypes { get; set; } = new List<DeductionType>();
}
