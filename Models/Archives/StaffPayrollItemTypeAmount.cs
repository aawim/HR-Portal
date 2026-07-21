using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffPayrollItemTypeAmount
{
    public int StaffPayrollItemTypeAmountId { get; set; }

    public decimal Amount { get; set; }

    public bool Status { get; set; }

    public bool IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public int StaffPayrollItemTypeId { get; set; }

    public int CurrencyTypeId { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual StaffPayrollItemType StaffPayrollItemType { get; set; } = null!;
}
