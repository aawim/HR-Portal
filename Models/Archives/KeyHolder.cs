using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class KeyHolder
{
    public int KeyHolderId { get; set; }

    public int IndividualId { get; set; }

    public bool IsCurrent { get; set; }

    public int OperationLogId { get; set; }

    public virtual Staff Individual { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
