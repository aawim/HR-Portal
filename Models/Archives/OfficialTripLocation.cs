using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OfficialTripLocation
{
    public int OfficialTripLocationsId { get; set; }

    public int LeaveId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int LocationId { get; set; }

    public string? ContactNumber { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public virtual OfficialTripDetail Leave { get; set; } = null!;

    public virtual Location Location { get; set; } = null!;
}
