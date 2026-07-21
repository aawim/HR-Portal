using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Country
{
    public int CountryId { get; set; }

    public string NameEnglish { get; set; } = null!;

    public string? NationalityEnglish { get; set; }

    public string? Isoalpha2Code { get; set; }

    public string? Isoalpha3Code { get; set; }

    public int? IsonumericCode { get; set; }

    public string? NameDhivehi { get; set; }

    public string? NationalityDhivehi { get; set; }

    public int? RegionId { get; set; }

    public virtual ICollection<Individual> Individuals { get; set; } = new List<Individual>();

    public virtual ICollection<LeaveSpendingLocation> LeaveSpendingLocations { get; set; } = new List<LeaveSpendingLocation>();

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual ICollection<OnlineAddress> OnlineAddresses { get; set; } = new List<OnlineAddress>();

    public virtual ICollection<Organisation> Organisations { get; set; } = new List<Organisation>();

    public virtual ICollection<Passport> Passports { get; set; } = new List<Passport>();

    public virtual Region? Region { get; set; }
}
