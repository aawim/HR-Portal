using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobPositionsRequest
{
    public int JobPositionsRequestId { get; set; }

    public int JobPositionId { get; set; }

    public int RequestId { get; set; }

    public DateTime? JobPositionRemovalDate { get; set; }

    public int OperationLogId { get; set; }

    public virtual JobPosition JobPosition { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
