using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollCycleLinkedRequest
{
    public int PayrollCycleLinkedRequestId { get; set; }

    public int PayrollCycleRequestId { get; set; }

    public int RequestId { get; set; }

    public int StaffSalaryId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual PayrollCycle PayrollCycleRequest { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;

    public virtual StaffSalary StaffSalary { get; set; } = null!;
}
