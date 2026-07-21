using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DayOfWeek
{
    public int DayOfWeekId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<AttachedBreakTimesDay> AttachedBreakTimesDays { get; set; } = new List<AttachedBreakTimesDay>();

    public virtual ICollection<Element> Elements { get; set; } = new List<Element>();
}
