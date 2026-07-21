using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Leaf
{
    public int LeaveId { get; set; }

    public int JobId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int Duration { get; set; }

    public string? ChitNo { get; set; }

    public int LeaveTypeId { get; set; }

    public int OperationLogId { get; set; }

    public int LeaveStateId { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual ICollection<LeaveChangeRequest> LeaveChangeRequests { get; set; } = new List<LeaveChangeRequest>();

    public virtual ICollection<LeaveDocument> LeaveDocuments { get; set; } = new List<LeaveDocument>();

    public virtual LeaveForm? LeaveForm { get; set; }

    public virtual LeaveReason? LeaveReason { get; set; }

    public virtual LeaveRequest? LeaveRequest { get; set; }

    public virtual LeaveSpendingLocation? LeaveSpendingLocation { get; set; }

    public virtual LeaveState LeaveState { get; set; } = null!;

    public virtual LeaveType LeaveType { get; set; } = null!;

    public virtual LeaveWorkHandOverTask? LeaveWorkHandOverTask { get; set; }

    public virtual ICollection<LeaveWorkHandOver> LeaveWorkHandOvers { get; set; } = new List<LeaveWorkHandOver>();

    public virtual NoPayLeaf? NoPayLeaf { get; set; }

    public virtual OfficialTripDetail? OfficialTripDetail { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual SickLeaveForm? SickLeaveForm { get; set; }
}
