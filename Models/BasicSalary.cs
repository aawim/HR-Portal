using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BasicSalary
{
    public int BasicSalaryId { get; set; }

    public double Amount { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;
}
