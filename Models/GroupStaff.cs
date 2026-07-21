using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class GroupStaff
{
    public int GroupStaffId { get; set; }

    public int GroupId { get; set; }

    public int StaffIndividualId { get; set; }

    public DateTime EffectiveStartDate { get; set; }

    public DateTime? EffectiveEndDate { get; set; }

    public int OperationLogId { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Staff StaffIndividual { get; set; } = null!;
}
