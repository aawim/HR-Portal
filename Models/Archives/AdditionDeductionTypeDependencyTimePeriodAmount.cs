using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AdditionDeductionTypeDependencyTimePeriodAmount
{
    public int PayrollItemTypeDependencyTimePeriodAmountId { get; set; }

    public double Amount { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int PayrollItemTypeId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }
}
