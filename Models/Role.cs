using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Role
{
    //public int RoleId { get; set; }
    public int RoleID { get; set; }
    public string RoleKey { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? ParentRoleId { get; set; }

    public bool? IsSystemRole { get; set; }

    public int ContextId { get; set; }

    public int? OperationLogId { get; set; }

    public int? ModuleId { get; set; }

    public virtual ICollection<BusinessEntityRelationAssignedRole> BusinessEntityRelationAssignedRoles { get; set; } = new List<BusinessEntityRelationAssignedRole>();

    public virtual Context Context { get; set; } = null!;

    public virtual Module? Module { get; set; }

    public virtual ICollection<NavigationLink> NavigationLinks { get; set; } = new List<NavigationLink>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<RequestTypesStatesRequiredRole> RequestTypesStatesRequiredRoles { get; set; } = new List<RequestTypesStatesRequiredRole>();

    public virtual ICollection<RequestTypesStatesRequiredRolesForProcessing> RequestTypesStatesRequiredRolesForProcessings { get; set; } = new List<RequestTypesStatesRequiredRolesForProcessing>();

    public virtual ICollection<UserGroupRole> UserGroupRoles { get; set; } = new List<UserGroupRole>();

    public virtual ICollection<UserOrganisationRole> UserOrganisationRoles { get; set; } = new List<UserOrganisationRole>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserServiceRole> UserServiceRoles { get; set; } = new List<UserServiceRole>();


}
