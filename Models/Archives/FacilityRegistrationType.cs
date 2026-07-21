using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class FacilityRegistrationType
{
    public int FacilityRegistrationTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsValid { get; set; }

    public string? RegistrationNumberFormat { get; set; }

    public string? RegistrationNumberRegex { get; set; }

    public virtual ICollection<NavigationLinkFacilityType> NavigationLinkFacilityTypes { get; set; } = new List<NavigationLinkFacilityType>();
}
