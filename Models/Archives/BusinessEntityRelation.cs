using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class BusinessEntityRelation
{
    public int BusinessEntityRelationId { get; set; }

    public int BusinessEntityRelationStateId { get; set; }

    public int BusinessEntityRelationTypeId { get; set; }

    public int DelegatorBusinessEntityId { get; set; }

    public int DelegateeBusinessEntityId { get; set; }

    public int? OperationLogId { get; set; }

    public int? PreviousBusinessEntityRelationSateId { get; set; }

    public virtual ICollection<BusinessEntityRelationAssignedRole> BusinessEntityRelationAssignedRoles { get; set; } = new List<BusinessEntityRelationAssignedRole>();

    public virtual BusinessEntityRelationState BusinessEntityRelationState { get; set; } = null!;

    public virtual BusinessEntityRelationType BusinessEntityRelationType { get; set; } = null!;

    public virtual BusinessEntity DelegateeBusinessEntity { get; set; } = null!;

    public virtual BusinessEntity DelegatorBusinessEntity { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
