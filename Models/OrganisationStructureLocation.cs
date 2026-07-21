using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureLocation
{
    public int OrganisationStructureLocationId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int LocationId { get; set; }

    public bool IsValid { get; set; }

    public int OrganisationStructureLocationTypeId { get; set; }

    public virtual Location Location { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual OrganisationStructureLocationType OrganisationStructureLocationType { get; set; } = null!;
}
