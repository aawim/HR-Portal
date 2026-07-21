using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AttendanceLogChangeRequestType
{
    public int AttendanceLogChangeRequestTypeId { get; set; }

    public string TypeName { get; set; } = null!;
}
