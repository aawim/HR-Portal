using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ServiceCheckListVerification
{
    public int ServiceCheckListVerificationId { get; set; }

    public int CheckListItemId { get; set; }

    public bool IsVerificationSuccessful { get; set; }

    public string? Comment { get; set; }

    public int ServiceId { get; set; }

    public int OprationLogId { get; set; }

    public virtual CheckListItem CheckListItem { get; set; } = null!;

    public virtual OperationLog OprationLog { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
