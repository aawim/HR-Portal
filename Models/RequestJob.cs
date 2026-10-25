using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestJob
{
    public int RequestId { get; set; }

    public int JobId { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
