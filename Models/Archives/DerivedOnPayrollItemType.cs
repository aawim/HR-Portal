using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DerivedOnPayrollItemType
{
    public int DerivedOnPayrollItemTypeId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public int OtherPayrollItemTypeId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual PayrollItemType OtherPayrollItemType { get; set; } = null!;

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;
}
