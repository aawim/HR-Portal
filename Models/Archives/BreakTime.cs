using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BreakTime
{
    public int BreakTimeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool IsPublic { get; set; }

    public bool IsGlobal { get; set; }

    public int? Duration { get; set; }

    public int OrganisationId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int OperationLogId { get; set; }

    public virtual ICollection<AttachedBreakTimesDay> AttachedBreakTimesDays { get; set; } = new List<AttachedBreakTimesDay>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual ICollection<WorkShiftsBreakTime> WorkShiftsBreakTimes { get; set; } = new List<WorkShiftsBreakTime>();
}
