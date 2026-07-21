using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffPayrollItemTypeSchedule
{
    public int StaffPayrollItemTypeScheduleId { get; set; }

    public int StaffPayrollItemTypeId { get; set; }

    public DateOnly Month { get; set; }

    public double Amount { get; set; }

    public string Status { get; set; } = null!;

    public bool IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public int CurrencyTypeId { get; set; }

    public int? PayrollCycleRequestId { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual PayrollCycle? PayrollCycleRequest { get; set; }

    public virtual StaffPayrollItemType StaffPayrollItemType { get; set; } = null!;
}
