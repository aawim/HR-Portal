using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttendanceDevice
{
    public int AttendanceDeviceId { get; set; }

    public string Ipaddress { get; set; } = null!;

    public int Port { get; set; }

    public string? Location { get; set; }

    public bool IsPrimary { get; set; }

    public int AttendanceDeviceStateId { get; set; }

    public int AttendanceClientId { get; set; }

    public int? OperationLogId { get; set; }

    public int AttendanceDeviceInOutTypeId { get; set; }

    public virtual AttendanceClientInstance AttendanceClient { get; set; } = null!;

    public virtual AttendanceDeviceInOutType AttendanceDeviceInOutType { get; set; } = null!;

    public virtual ICollection<AttendanceDeviceStaff> AttendanceDeviceStaffs { get; set; } = new List<AttendanceDeviceStaff>();

    public virtual AttendanceDeviceState AttendanceDeviceState { get; set; } = null!;

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual OperationLog? OperationLog { get; set; }
}
