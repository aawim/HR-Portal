using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LeaveType
{
    public int LeaveTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? NameDhivehi { get; set; }

    public int? Duration { get; set; }

    public bool IncludeHolidays { get; set; }

    public bool IncludePay { get; set; }

    public bool IsPublic { get; set; }

    public bool IsGlobal { get; set; }

    public bool IsLocationRequired { get; set; }

    public int ServiceDurationMonths { get; set; }

    public int OrganisationId { get; set; }

    public int RequestTypeId { get; set; }

    public bool IsSystemType { get; set; }

    public bool IsRenewed { get; set; }

    public int OperationLogId { get; set; }

    public bool IsStaffWideAvailable { get; set; }

    public double? PayPercentage { get; set; }

    public int StartInMonth { get; set; }

    public int RepeatedEveryInMonth { get; set; }

    public virtual ICollection<JobLeaveType> JobLeaveTypes { get; set; } = new List<JobLeaveType>();

    public virtual ICollection<LeaveSetLeaveType> LeaveSetLeaveTypes { get; set; } = new List<LeaveSetLeaveType>();

    public virtual ICollection<Leaf> Leaves { get; set; } = new List<Leaf>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual RequestType RequestType { get; set; } = null!;
}
