using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureSupervisor
{
    public int OrganisationStructureSupervisorId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int SupervisorStaffId { get; set; }

    public bool IsValid { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual Staff SupervisorStaff { get; set; } = null!;
}
