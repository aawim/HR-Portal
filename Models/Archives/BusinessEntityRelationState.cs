using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class BusinessEntityRelationState
{
    public int BusinessEntityRelationStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<BusinessEntityRelation> BusinessEntityRelations { get; set; } = new List<BusinessEntityRelation>();
}
