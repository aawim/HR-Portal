using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class BusinessEntityRelationType
{
    public int BusinessEntityRelationTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public int? BusinessEntityId { get; set; }

    public virtual BusinessEntity? BusinessEntity { get; set; }

    public virtual ICollection<BusinessEntityRelation> BusinessEntityRelations { get; set; } = new List<BusinessEntityRelation>();
}
