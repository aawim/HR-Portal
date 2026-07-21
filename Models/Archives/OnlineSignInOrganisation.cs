using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineSignInOrganisation
{
    public int OrganisationId { get; set; }

    public bool IsEnaled { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;
}
