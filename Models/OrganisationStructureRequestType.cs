using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureRequestType
{
    public int OrganisationStructureRequestTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<OrganisationStructureDraft> OrganisationStructureDrafts { get; set; } = new List<OrganisationStructureDraft>();
}
