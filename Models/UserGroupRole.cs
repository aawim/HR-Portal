using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class UserGroupRole
{
    public int UserGroupId { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public virtual UserGroup UserGroup { get; set; } = null!;
}
