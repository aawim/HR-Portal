using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationDeductionType
{
    public int OrganisationDeductionTypeId { get; set; }

    public int DeductionTypeId { get; set; }

    public int OrganisationId { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual DeductionType DeductionType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;
}
