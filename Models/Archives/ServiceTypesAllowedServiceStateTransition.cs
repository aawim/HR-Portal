using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ServiceTypesAllowedServiceStateTransition
{
    public int ServiceTypeId { get; set; }

    public int FromServiceStateId { get; set; }

    public int ToServiceStateId { get; set; }

    public virtual ServiceType ServiceType { get; set; } = null!;

    public virtual ServiceState ToServiceState { get; set; } = null!;
}
