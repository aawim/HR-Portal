using HRM.Models.Archives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;


public partial class UserOrganisation
{
    public int UserOrganisationID { get; set; }

    public int BusinessEntityID { get; set; }

    public int? OperationLogId { get; set; }

    [ForeignKey(nameof(BusinessEntityID))]
    public virtual Organisation Organisation { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

    public virtual ICollection<UserOrganisationRole> UserOrganisationRoles { get; set; } = new List<UserOrganisationRole>();

    public virtual ICollection<UserOrganisationUserGroup> UserOrganisationUserGroups { get; set; } = new List<UserOrganisationUserGroup>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}



//public partial class UserOrganisation
//{
//    //public int UserOrganisationId { get; set; }
//    //public int BusinessEntityId { get; set; }


//    public int UserOrganisationID { get; set; }
//    public int BusinessEntityID { get; set; }


//    public int? OperationLogId { get; set; }

//    //[ForeignKey("BusinessEntityID")]
//    //public virtual Organisation BusinessEntity { get; set; } = null!;

//    [ForeignKey(nameof(BusinessEntityID))]
//    public virtual Organisation Organisation { get; set; } = null!;

//    public virtual OperationLog? OperationLog { get; set; }

//    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

//    public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

//    public virtual ICollection<UserOrganisationRole> UserOrganisationRoles { get; set; } = new List<UserOrganisationRole>();

//    public virtual ICollection<UserOrganisationUserGroup> UserOrganisationUserGroups { get; set; } = new List<UserOrganisationUserGroup>();

//    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

//    public virtual ICollection<User> Users { get; set; } = new List<User>();
//}
