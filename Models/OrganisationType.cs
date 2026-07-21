using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationType
{
    public int OrganisationTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Organisation> Organisations { get; set; } = new List<Organisation>();
}
