using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class UserGroup
{
    public int UserGroupId { get; set; }

    public int UserOrganisationId { get; set; }

    public string GroupName { get; set; } = null!;

    public int? ParentUserGroupId { get; set; }

    public int OperationLogId { get; set; }

    public bool IsGlobal { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<UserAssignedUserGroup> UserAssignedUserGroups { get; set; } = new List<UserAssignedUserGroup>();

    public virtual ICollection<UserGroupRole> UserGroupRoles { get; set; } = new List<UserGroupRole>();

    public virtual UserOrganisation UserOrganisation { get; set; } = null!;

    public virtual ICollection<UserOrganisationUserGroup> UserOrganisationUserGroups { get; set; } = new List<UserOrganisationUserGroup>();
}
