using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttendanceDeviceStaff
{
    public int AttendanceDeviceStaffId { get; set; }

    public int AttendanceDeviceId { get; set; }

    public int IndividualId { get; set; }

    public string EnrollmentNumber { get; set; } = null!;

    public virtual AttendanceDevice AttendanceDevice { get; set; } = null!;

    public virtual Staff Individual { get; set; } = null!;
}
