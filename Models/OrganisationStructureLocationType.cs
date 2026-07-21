using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureLocationType
{
    public int OrganisationStructureLocationTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<OrganisationStructureLocation> OrganisationStructureLocations { get; set; } = new List<OrganisationStructureLocation>();
}
