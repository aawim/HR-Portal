using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class UserOrganisationUserGroup
{
    public int UserOrganisationId { get; set; }

    public int UserGroupId { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual UserGroup UserGroup { get; set; } = null!;

    public virtual UserOrganisation UserOrganisation { get; set; } = null!;
}
