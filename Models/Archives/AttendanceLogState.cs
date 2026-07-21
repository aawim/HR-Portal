using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AttendanceLogState
{
    public int AttendanceLogStateId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
