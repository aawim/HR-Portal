using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobLeaveType
{
    public int JobLeaveTypeId { get; set; }

    public int JobId { get; set; }

    public int LeaveTypeId { get; set; }

    public int? RemainingDays { get; set; }

    public DateTime? LastLeaveTakenDate { get; set; }

    public bool IsValid { get; set; }

    public DateTime? RenewedDate { get; set; }

    public bool IsLeaveInfoUpdated { get; set; }

    public int OperationLogId { get; set; }

    public DateTime? EffectiveFromDate { get; set; }

    public DateTime? EffectiveToDate { get; set; }

    public DateTime? AutoCorrectDate { get; set; }

    public string? AutoCorrectRemark { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual LeaveType LeaveType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
