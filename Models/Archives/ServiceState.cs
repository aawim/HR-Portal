using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ServiceState
{
    public int ServiceStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<ServiceTypesAllowedServiceStateTransition> ServiceTypesAllowedServiceStateTransitions { get; set; } = new List<ServiceTypesAllowedServiceStateTransition>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
