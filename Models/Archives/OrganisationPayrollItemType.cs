using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationPayrollItemType
{
    public int OrganisationPayrollItemTypeId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public int OrganisationId { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;
}
