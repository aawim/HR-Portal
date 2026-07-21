using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SpecialDay
{
    public int SpecialDayId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int CalenderId { get; set; }

    public int CalenderDayTypeId { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<SpecialDayShift> SpecialDayShifts { get; set; } = new List<SpecialDayShift>();
}
