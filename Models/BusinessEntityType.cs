using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BusinessEntityType
{
    public int BusinessEntityTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<BusinessEntity> BusinessEntities { get; set; } = new List<BusinessEntity>();

    public virtual ICollection<BusinessEntityRelatedLocationType> BusinessEntityRelatedLocationTypes { get; set; } = new List<BusinessEntityRelatedLocationType>();
}
