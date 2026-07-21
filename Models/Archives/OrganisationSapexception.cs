using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationSapexception
{
    public int SapexceptionId { get; set; }

    public int OrganisationBusinessEntityID { get; set; }

    public virtual Organisation OrganisationBusinessEntity { get; set; } = null!;

    public virtual Sapexception Sapexception { get; set; } = null!;
}
