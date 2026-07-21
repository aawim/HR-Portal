using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffSalaryPayrollItem
{
    public int StaffSalaryPayrollItemId { get; set; }

    public int? StaffPayrollItemTypeId { get; set; }

    public decimal Amount { get; set; }

    public string Details { get; set; } = null!;

    public int OperationLogId { get; set; }

    public int StaffSalaryId { get; set; }

    public bool IsValid { get; set; }

    public int? PayrollItemTypeId { get; set; }

    public bool IsSpecialType { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual PayrollItemType? PayrollItemType { get; set; }

    public virtual StaffPayrollItemType? StaffPayrollItemType { get; set; }

    public virtual StaffSalary StaffSalary { get; set; } = null!;
}
