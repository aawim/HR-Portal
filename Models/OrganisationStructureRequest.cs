using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureRequest
{
    public int OrganisationStructureRequestId { get; set; }

    public int RequestId { get; set; }

    public int OrganisationBusinessEntityID { get; set; }

    public virtual Organisation OrganisationBusinessEntity { get; set; } = null!;

    public virtual ICollection<OrganisationStructureDraft> OrganisationStructureDrafts { get; set; } = new List<OrganisationStructureDraft>();

    public virtual Request Request { get; set; } = null!;
}
