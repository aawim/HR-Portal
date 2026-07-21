using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LeaveTypeReasonType
{
    public int LeaveTypeReasonTypeId { get; set; }

    public int LeaveTypeId { get; set; }

    public string ReasonTypeName { get; set; } = null!;

    public virtual ICollection<LeaveReason> LeaveReasons { get; set; } = new List<LeaveReason>();
}
