using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class NavigationLink
{
    public int NavigationLinkId { get; set; }

    public string LinkName { get; set; } = null!;

    public int? ParentNavigationLinkId { get; set; }

    public string Url { get; set; } = null!;

    public bool? IsValid { get; set; }

    public int? RoleId { get; set; }

    public int DisplayOrder { get; set; }

    public int LinkTypeId { get; set; }

    public int ContextId { get; set; }

    public int ModuleId { get; set; }

    public string? LinkNameDhivehiName { get; set; }

    public virtual Context Context { get; set; } = null!;

    public virtual ICollection<NavigationLink> InverseParentNavigationLink { get; set; } = new List<NavigationLink>();

    public virtual LinkType LinkType { get; set; } = null!;

    public virtual Module Module { get; set; } = null!;

    public virtual ICollection<NavigationLinkFacilityType> NavigationLinkFacilityTypes { get; set; } = new List<NavigationLinkFacilityType>();

    public virtual NavigationLink? ParentNavigationLink { get; set; }

    public virtual Role? Role { get; set; }
}
