using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PercentageVariableDeductionAmount
{
    public int PercentageVariableDeductionAmountId { get; set; }

    public int DeductionTypeId { get; set; }

    public double StartAmount { get; set; }

    public double EndAmount { get; set; }

    public decimal Percentage { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual DeductionType DeductionType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
