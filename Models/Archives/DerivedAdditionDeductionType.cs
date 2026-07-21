using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DerivedAdditionDeductionType
{
    public int DerivedAdditionDeductionTypeId { get; set; }

    public int DeductionTypeId { get; set; }

    public int DerivedAdditionDeductionTypeItemId { get; set; }

    public virtual DeductionType DeductionType { get; set; } = null!;

    public virtual DerivedAdditionDeductionTypeItem DerivedAdditionDeductionTypeItem { get; set; } = null!;
}
