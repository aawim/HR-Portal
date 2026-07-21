using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AttendanceLogMode
{
    public int AttendanceLogModeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
