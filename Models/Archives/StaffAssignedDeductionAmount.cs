using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffAssignedDeductionAmount
{
    public int StaffAssignedDeductionAmountId { get; set; }

    public double Amount { get; set; }

    public int Status { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public int StaffAssignedDeductionId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual StaffAssignedDeduction StaffAssignedDeduction { get; set; } = null!;
}
