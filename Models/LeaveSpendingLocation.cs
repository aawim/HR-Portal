using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LeaveSpendingLocation
{
    public int LeaveId { get; set; }

    public int CountryId { get; set; }

    public int? IslandId { get; set; }

    public string Address { get; set; } = null!;

    public string? ContactNumber { get; set; }

    public int OperationLogId { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual Island? Island { get; set; }

    public virtual Leaf Leave { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
