using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LocationType
{
    public int LocationTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

}
