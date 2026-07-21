using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineAddress
{
    public int OnlineLocationId { get; set; }

    public int CountryId { get; set; }

    public int? IslandId { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual Island? Island { get; set; }

    public virtual OnlineAddressInstance? OnlineAddressInstance { get; set; }

    public virtual OnlineArea? OnlineArea { get; set; }

    public virtual OnlineDhafthar? OnlineDhafthar { get; set; }

    public virtual OnlineLocation OnlineLocation { get; set; } = null!;
}
