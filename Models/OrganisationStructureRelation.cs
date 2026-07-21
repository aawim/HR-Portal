using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureRelation
{
    public int OrganisationStructureRelationId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int ParentOrganisationStructureId { get; set; }

    public DateTime ActiveFromDate { get; set; }

    public DateTime? ActiveToDate { get; set; }

    public bool IsCurrent { get; set; }

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual ICollection<OrganisationStructureHistory> OrganisationStructureHistories { get; set; } = new List<OrganisationStructureHistory>();

    public virtual OrganisationStructure ParentOrganisationStructure { get; set; } = null!;
}
