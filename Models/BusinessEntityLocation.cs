using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BusinessEntityLocation
{
    public int BusinessEntityLocationId { get; set; }

    public int BusinessEntityId { get; set; }

    public int LocationId { get; set; }

    public int BusinessEntityLocationTypeId { get; set; }

    public bool IsValid { get; set; }

    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    public virtual BusinessEntityLocationType BusinessEntityLocationType { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;
}
