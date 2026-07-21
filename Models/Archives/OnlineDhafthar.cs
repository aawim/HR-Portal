using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineDhafthar
{
    public int OnlineLocationId { get; set; }

    public string DhaftharNumber { get; set; } = null!;

    public virtual OnlineAddress OnlineLocation { get; set; } = null!;
}
