using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationPayrollPeriod
{
    public int OrganisationPayrollPeriodId { get; set; }

    public int OrganisationId { get; set; }

    public int StartDay { get; set; }

    public int EndDay { get; set; }

    public bool IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual ICollection<OrganisationPayrollPeriodJobType> OrganisationPayrollPeriodJobTypes { get; set; } = new List<OrganisationPayrollPeriodJobType>();

    public virtual ICollection<PayrollCycle> PayrollCycles { get; set; } = new List<PayrollCycle>();
}
