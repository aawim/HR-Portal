using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobPositionBasicSalary
{
    public int JobPositionBasicSalaryId { get; set; }

    public int JobPoistionId { get; set; }

    public double Amount { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual JobPosition JobPoistion { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
