using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class WorkShift
{
    public int WorkShiftId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool IsPublic { get; set; }

    public bool IsGlobal { get; set; }

    public int OrganisationId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int OperationLogId { get; set; }

    public virtual ICollection<Element> Elements { get; set; } = new List<Element>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual ICollection<ShiftScheduleDay> ShiftScheduleDays { get; set; } = new List<ShiftScheduleDay>();

    public virtual ICollection<WorkShiftsBreakTime> WorkShiftsBreakTimes { get; set; } = new List<WorkShiftsBreakTime>();
}
