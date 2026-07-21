using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttachedPayrollItemsProcessingWhiteList
{
    public int AttachedPayrollItemsProcessingWhiteListId { get; set; }

    public int PayrollItemsProcessingWhiteListId { get; set; }

    public int OrganisationId { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual PayrollItemsProcessingWhiteList PayrollItemsProcessingWhiteList { get; set; } = null!;
}
