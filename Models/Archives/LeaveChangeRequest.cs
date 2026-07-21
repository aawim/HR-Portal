using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveChangeRequest
{
    public int LeaveChangeRequestId { get; set; }

    public int LeaveId { get; set; }

    public int LeaveChangeRequestTypeId { get; set; }

    public int RequestId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public string Reason { get; set; } = null!;

    public int OperationLogId { get; set; }

    public virtual Leaf Leave { get; set; } = null!;

    public virtual LeaveChangeRequestType LeaveChangeRequestType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
