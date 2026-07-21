using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class BusinessEntityRelationAssignedRole
{
    public int BsinessEntityRelationAssignedRoleId { get; set; }

    public int BusinessEntityRelationId { get; set; }

    public int RoleId { get; set; }

    public virtual BusinessEntityRelation BusinessEntityRelation { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
