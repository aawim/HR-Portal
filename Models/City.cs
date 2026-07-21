using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class City
{
    public int CityId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Atoll> Atolls { get; set; } = new List<Atoll>();

    public virtual ICollection<Island> Islands { get; set; } = new List<Island>();
}
