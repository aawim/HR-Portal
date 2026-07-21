using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollCycle
{
    public int RequestId { get; set; }

    public int OrganisationPayrollPeriodId { get; set; }

    public DateOnly PayrollMonth { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int CycleState { get; set; }

    public virtual OrganisationPayrollPeriod OrganisationPayrollPeriod { get; set; } = null!;

    public virtual ICollection<PayrollCycleLinkedRequest> PayrollCycleLinkedRequests { get; set; } = new List<PayrollCycleLinkedRequest>();

    public virtual Request Request { get; set; } = null!;

    public virtual ICollection<StaffPayrollItemTypeSchedule> StaffPayrollItemTypeSchedules { get; set; } = new List<StaffPayrollItemTypeSchedule>();

    public virtual ICollection<StaffSalary> StaffSalaries { get; set; } = new List<StaffSalary>();
}
