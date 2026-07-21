using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureHeadIncharge
{
    public int OrganisationStructureHeadInchargeId { get; set; }

    public int IndividualId { get; set; }

    public int OrganisationStructureId { get; set; }

    public bool IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Staff Individual { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;
}
