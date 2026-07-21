using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Ot
{
    public int Otid { get; set; }

    public int OtrequestId { get; set; }

    public string WorkDone { get; set; } = null!;

    public int? StartAttendanceLogId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public int? EndAttendanceLogId { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int? Duration { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual AttendanceLog? EndAttendanceLog { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Otrequest Otrequest { get; set; } = null!;

    public virtual AttendanceLog? StartAttendanceLog { get; set; }
}
