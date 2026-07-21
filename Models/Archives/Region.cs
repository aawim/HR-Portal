using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Region
{
    public int RegionId { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public virtual ICollection<Country> Countries { get; set; } = new List<Country>();
}
