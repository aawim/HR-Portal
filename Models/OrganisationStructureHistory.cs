using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureHistory
{
    public int OrganisationStructureHistoryId { get; set; }

    public int OrganisationStructureRelationId { get; set; }

    public int OrganisationId { get; set; }

    public int RequestId { get; set; }

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual OrganisationStructureRelation OrganisationStructureRelation { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
