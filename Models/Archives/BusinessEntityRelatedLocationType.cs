using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class BusinessEntityRelatedLocationType
{
    public int LocationTypeId { get; set; }

    public int BusinessEntityTypeId { get; set; }

    public int BusinessEntityRelatedLocationTypeId { get; set; }

    public virtual BusinessEntityType BusinessEntityType { get; set; } = null!;

    public virtual LocationType LocationType { get; set; } = null!;
}
