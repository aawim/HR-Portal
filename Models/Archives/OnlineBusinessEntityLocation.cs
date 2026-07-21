using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineBusinessEntityLocation
{
    public int OnlineBusinessEntityLocationId { get; set; }

    public int BusinessEntityId { get; set; }

    public int OnlineLocationId { get; set; }

    public int BusinessEntityLocationTypeId { get; set; }

    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    public virtual BusinessEntityLocationType BusinessEntityLocationType { get; set; } = null!;

    public virtual OnlineLocation OnlineLocation { get; set; } = null!;
}
