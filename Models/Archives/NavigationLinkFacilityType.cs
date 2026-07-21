using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class NavigationLinkFacilityType
{
    public int NavigationLinkFacilityTypeId { get; set; }

    public int NavigationLinkId { get; set; }

    public int? FacilityTypeId { get; set; }

    public virtual FacilityRegistrationType? FacilityType { get; set; }

    public virtual NavigationLink NavigationLink { get; set; } = null!;
}
