using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollItemTypeDependencyAmount
{
    public int PayrollItemTypeDependencyAmountId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public int DependencyId { get; set; }

    public decimal Amount { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;
}
