using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class UserAssignedUserGroup
{
    public int UserGroupId { get; set; }

    public int UserId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual UserGroup UserGroup { get; set; } = null!;
}
