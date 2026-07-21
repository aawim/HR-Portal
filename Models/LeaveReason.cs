using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LeaveReason
{
    public int LeaveId { get; set; }

    public string Reason { get; set; } = null!;

    public int LeaveTypeReasonTypeId { get; set; }

    public virtual Leaf Leave { get; set; } = null!;

    public virtual LeaveTypeReasonType LeaveTypeReasonType { get; set; } = null!;
}
