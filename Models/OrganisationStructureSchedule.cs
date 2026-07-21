using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureSchedule
{
    public int OrganisationStructureScheduleId { get; set; }

    public int ScheduleId { get; set; }

    public int OrganisationStructureId { get; set; }

    public bool IsLinked { get; set; }

    public int OperationLogId { get; set; }

    public bool IsActive { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;
}
