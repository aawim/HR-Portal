using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineLocation
{
    public int OnlineLocationId { get; set; }

    public int LocationTypeId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual LocationType LocationType { get; set; } = null!;

    public virtual OnlineAddress? OnlineAddress { get; set; }

    public virtual ICollection<OnlineBusinessEntityLocation> OnlineBusinessEntityLocations { get; set; } = new List<OnlineBusinessEntityLocation>();

    public virtual OperationLog? OperationLog { get; set; }
}
