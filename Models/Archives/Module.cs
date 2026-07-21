using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Module
{
    public int ModuleId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDhivehiSupported { get; set; }

    public virtual ICollection<NavigationLink> NavigationLinks { get; set; } = new List<NavigationLink>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
