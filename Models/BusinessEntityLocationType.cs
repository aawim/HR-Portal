using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BusinessEntityLocationType
{
    public int BusinessEntityLocationTypeId { get; set; }

    public string? TypeName { get; set; }

    public virtual ICollection<BusinessEntityLocation> BusinessEntityLocations { get; set; } = new List<BusinessEntityLocation>();

    public virtual ICollection<OnlineBusinessEntityLocation> OnlineBusinessEntityLocations { get; set; } = new List<OnlineBusinessEntityLocation>();
}
