using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class CheckListItem
{
    public int CheckListItemId { get; set; }

    public string CheckListItemName { get; set; } = null!;

    public string? Description { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<RequestCheckListVerification> RequestCheckListVerifications { get; set; } = new List<RequestCheckListVerification>();

    public virtual ICollection<ServiceCheckListVerification> ServiceCheckListVerifications { get; set; } = new List<ServiceCheckListVerification>();

    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();

    public virtual ICollection<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();
}
