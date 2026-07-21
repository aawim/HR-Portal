using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Location
{
    public int LocationId { get; set; }

    public int LocationTypeId { get; set; }

    public int CountryId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<BusinessEntityLocation> BusinessEntityLocations { get; set; } = new List<BusinessEntityLocation>();

    public virtual Country Country { get; set; } = null!;

    public virtual LocationType LocationType { get; set; } = null!;

    public virtual ICollection<OfficialTripLocation> OfficialTripLocations { get; set; } = new List<OfficialTripLocation>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<OrganisationStructureLocation> OrganisationStructureLocations { get; set; } = new List<OrganisationStructureLocation>();
}
