using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationalStructureSapexception
{
    public int SapexceptionId { get; set; }

    public int OrganisationStructureId { get; set; }

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual Sapexception Sapexception { get; set; } = null!;
}
