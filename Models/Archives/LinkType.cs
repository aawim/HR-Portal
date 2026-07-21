using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LinkType
{
    public int LinkTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<NavigationLink> NavigationLinks { get; set; } = new List<NavigationLink>();
}
