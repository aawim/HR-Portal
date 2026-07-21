using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureState
{
    public int OrganisationStructureStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<OrganisationStructureDraft> OrganisationStructureDrafts { get; set; } = new List<OrganisationStructureDraft>();

    public virtual ICollection<OrganisationStructure> OrganisationStructures { get; set; } = new List<OrganisationStructure>();
}
