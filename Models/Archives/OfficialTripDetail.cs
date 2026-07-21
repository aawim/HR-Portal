using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OfficialTripDetail
{
    public int LeaveId { get; set; }

    public int TypeId { get; set; }

    public string Purpose { get; set; } = null!;

    public bool IsLocal { get; set; }

    public string? FocalPointContactNumber { get; set; }

    public string? FocalPointEmail { get; set; }

    public string? FocalPointAddress { get; set; }

    public virtual Leaf Leave { get; set; } = null!;

    public virtual ICollection<OfficialTripLocation> OfficialTripLocations { get; set; } = new List<OfficialTripLocation>();

    public virtual OfficialTripType Type { get; set; } = null!;
}
