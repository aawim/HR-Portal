using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class InOutMode
{
    public int InOutModeId { get; set; }

    public string NameShort { get; set; } = null!;

    public string NameLong { get; set; } = null!;

    public bool IsIn { get; set; }

    public string Icon { get; set; } = null!;

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
