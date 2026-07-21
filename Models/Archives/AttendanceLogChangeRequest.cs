using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AttendanceLogChangeRequest
{
    public int AttendanceLogId { get; set; }

    public int RequestId { get; set; }

    public string Comments { get; set; } = null!;

    public virtual AttendanceLog AttendanceLog { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
