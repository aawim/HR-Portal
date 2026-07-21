using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollItemsProcessingWhiteList
{
    public int PayrollItemsProcessingWhiteListId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public int JobTypeId { get; set; }

    public int OwnerOrganisationId { get; set; }

    public bool IsActive { get; set; }

    public bool IsGlobal { get; set; }

    public int? OperationLogId { get; set; }

    public virtual ICollection<AttachedPayrollItemsProcessingWhiteList> AttachedPayrollItemsProcessingWhiteLists { get; set; } = new List<AttachedPayrollItemsProcessingWhiteList>();

    public virtual JobType JobType { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation OwnerOrganisation { get; set; } = null!;

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;
}
