using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OtpreApprovalTime
{
    public int OtpreApprovalTimeId { get; set; }

    public int RequestId { get; set; }

    public int DayTypeId { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int DurationLimit { get; set; }

    public int OperationLogId { get; set; }

    public virtual DayType DayType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual OtpreApproval Request { get; set; } = null!;
}
