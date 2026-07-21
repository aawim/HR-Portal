using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttendanceDeviceState
{
    public int AttendanceDeviceStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<AttendanceDevice> AttendanceDevices { get; set; } = new List<AttendanceDevice>();
}
