using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineArea
{
    public int OnlineLocationId { get; set; }

    public string AreaName { get; set; } = null!;

    public int? WardId { get; set; }

    public virtual OnlineAddress OnlineLocation { get; set; } = null!;

    public virtual Ward? Ward { get; set; }
}
