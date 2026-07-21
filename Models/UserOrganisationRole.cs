using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

public partial class UserOrganisationRole
{
    public int UserOrganisationRoleID { get; set; }

    //public int UserOrganisationId { get; set; }
    public int UserOrganisationID { get; set; }

    //public int RoleId { get; set; }
    public int RoleID { get; set; }
    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    [ForeignKey("UserOrganisationID")]
    [InverseProperty("UserOrganisationRoles")]
    public virtual UserOrganisation UserOrganisation { get; set; } = null!;
}
