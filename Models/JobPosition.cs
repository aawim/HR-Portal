using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobPosition
{
    public int JobPositionId { get; set; }

    public int PositionId { get; set; }

    public int JobId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int JobPositionStateId { get; set; }

    public int OperationLogId { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual ICollection<JobPositionBasicSalary> JobPositionBasicSalaries { get; set; } = new List<JobPositionBasicSalary>();

    public virtual JobPositionState JobPositionState { get; set; } = null!;

    public virtual ICollection<JobPositionsRequest> JobPositionsRequests { get; set; } = new List<JobPositionsRequest>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Position Position { get; set; } = null!;

    public virtual ICollection<StaffDailyAttendanceSummery> StaffDailyAttendanceSummeries { get; set; } = new List<StaffDailyAttendanceSummery>();
}
