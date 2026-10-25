using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveSet
{
    public int LeaveSetId { get; set; }

    public string Name { get; set; } = null!;

    public int OrganisationId { get; set; }

    public int OperationLogId { get; set; }

    public bool IsPublic { get; set; }

    public bool IsGlobal { get; set; }

    public virtual ICollection<GroupLeaveSet> GroupLeaveSets { get; set; } = new List<GroupLeaveSet>();

    public virtual ICollection<LeaveSetLeaveType> LeaveSetLeaveTypes { get; set; } = new List<LeaveSetLeaveType>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;
}
