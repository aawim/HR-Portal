using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class UserRole
{
    public int RoleId { get; set; }

    public int UserId { get; set; }

    public int UserOrganisationId { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual UserOrganisation UserOrganisation { get; set; } = null!;
}
