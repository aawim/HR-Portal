using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AdditionOrDeductionType
{
    public int AdditionOrDeductionTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsValid { get; set; }

    public virtual ICollection<PayrollItemTypeProcessingCode> PayrollItemTypeProcessingCodes { get; set; } = new List<PayrollItemTypeProcessingCode>();

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();
}
