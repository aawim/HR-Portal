using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffSalaryDeduction
{
    public int StaffSalaryDeductionId { get; set; }

    public int StaffAssignedDeductionId { get; set; }

    public double Amount { get; set; }

    public string Details { get; set; } = null!;

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public int StaffSalaryId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual StaffAssignedDeduction StaffAssignedDeduction { get; set; } = null!;

    public virtual StaffSalary StaffSalary { get; set; } = null!;
}
