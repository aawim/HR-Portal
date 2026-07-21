using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Address
{
    public int LocationId { get; set; }

    public int? IslandId { get; set; }

    public virtual AddressInstance? AddressInstance { get; set; }

    public virtual Dhafthar? Dhafthar { get; set; }

    public virtual Island? Island { get; set; }

    public virtual Location Location { get; set; } = null!;
}
