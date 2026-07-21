using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttendanceDeviceInOutType
{
    public int AttendanceDeviceInOutTypeId { get; set; }

    public string AttendanceDeviceInOutTypeName { get; set; } = null!;

    public virtual ICollection<AttendanceDevice> AttendanceDevices { get; set; } = new List<AttendanceDevice>();
}
