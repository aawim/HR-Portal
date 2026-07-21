using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestCheckListVerification
{
    public int RequestCheckListVerificationId { get; set; }

    public int CheckListItemId { get; set; }

    public bool IsVerificationSuccssful { get; set; }

    public string? Comment { get; set; }

    public int RequestId { get; set; }

    public int OperationLogId { get; set; }

    public virtual CheckListItem CheckListItem { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
