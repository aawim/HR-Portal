using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Island
{
    public int IslandId { get; set; }

    public string NameEnglish { get; set; } = null!;

    public string? NameDhivehi { get; set; }

    public int AtollId { get; set; }

    public string? PostCode { get; set; }

    public bool IsInhibited { get; set; }

    public int? CityId { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual Atoll Atoll { get; set; } = null!;

    public virtual City? City { get; set; }

    public virtual ICollection<LeaveSpendingLocation> LeaveSpendingLocations { get; set; } = new List<LeaveSpendingLocation>();

    public virtual ICollection<OnlineAddress> OnlineAddresses { get; set; } = new List<OnlineAddress>();

    public virtual ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
