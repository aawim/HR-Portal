using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class TeamStaff
{
    public int TeamStaffsId { get; set; }

    public int TeamId { get; set; }

    public int StaffId { get; set; }

    public bool IsSuperVisor { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual Staff Staff { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
