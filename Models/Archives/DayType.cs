using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DayType
{
    public int DayTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<OtpreApprovalTime> OtpreApprovalTimes { get; set; } = new List<OtpreApprovalTime>();

    public virtual ICollection<SpecialDayType> SpecialDayTypes { get; set; } = new List<SpecialDayType>();
}
