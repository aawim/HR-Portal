using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Dhafthar
{
    public int LocationId { get; set; }

    public string DhaftharNumber { get; set; } = null!;

    public virtual Address Location { get; set; } = null!;
}
